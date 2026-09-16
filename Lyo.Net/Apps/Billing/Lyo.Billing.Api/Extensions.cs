using Lyo.Api;
using Lyo.Api.ApiEndpoint;
using Lyo.Api.Export;
using Lyo.Api.Mapping;
using Lyo.Billing;
using Lyo.Billing.Postgres;
using Lyo.Billing.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.EntityReference.Postgres.Database;
using Lyo.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lyo.Billing.Api;

/// <summary>HTTP mapping for the Lyo Billing API. Register store DI with <c>AddBillingApiFromConfiguration</c> first. Does not render PDFs.</summary>
public static class Extensions
{
    private static readonly string[] InvoiceDeniedSelectFields = ["EncryptedTaxId", "TaxId"];
    private static readonly string[] ClientDeniedSelectFields = ["EncryptedTaxId", "TaxId"];
    private static readonly string[] BillerDeniedSelectFields = ["EncryptedRemittance", "Remittance", "EncryptedBankAccountNumber", "BankAccountNumber"];

    extension(IServiceCollection services)
    {
        /// <summary>Registers the Billing Postgres store, CRUD services, and an identity mapper. Hosts must also register Ledger.</summary>
        public IServiceCollection AddBillingApi(Action<PostgresBillingOptions> configure)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(configure);
            var options = new PostgresBillingOptions();
            configure(options);
            return services.AddBillingApi(options);
        }

        /// <summary>Registers Billing from configuration section <see cref="PostgresBillingOptions.SectionName" />.</summary>
        public IServiceCollection AddBillingApiFromConfiguration(IConfiguration configuration, string configSectionName = PostgresBillingOptions.SectionName)
        {
            ArgumentHelpers.ThrowIfNull(configuration);
            services.AddPostgresBillingStoreFromConfiguration(configuration, configSectionName);
            return services.AddBillingApiCore();
        }

        /// <summary>Registers the Billing Postgres store, CRUD services, and an identity mapper.</summary>
        public IServiceCollection AddBillingApi(PostgresBillingOptions options)
        {
            ArgumentHelpers.ThrowIfNull(services);
            ArgumentHelpers.ThrowIfNull(options);
            services.AddPostgresBillingStore(options);
            return services.AddBillingApiCore();
        }

        private IServiceCollection AddBillingApiCore()
        {
            services.AddLyoCrudServices<BillingDbContext>();
            services.TryAddSingleton<ILyoMapper, IdentityLyoMapper>();
            return services;
        }
    }

    /// <summary>Maps Billing CRUD and issue/void/pay endpoints. Invoke after <c>AddBillingApi</c>.</summary>
    public static WebApplication BuildBillingGroup(this WebApplication app)
    {
        app.CreateBuilder<BillingDbContext, BillingClientEntity, BillingClientEntity, BillingClientEntity, Guid>("Billing/Clients", "Billing")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    DeniedSelectFields = ClientDeniedSelectFields,
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id;
                        if (ctx.Entity.CreatedAt == default)
                            ctx.Entity.CreatedAt = DateTime.UtcNow;
                        if (ctx.Entity.TenantId is null || ctx.Entity.TenantId == Guid.Empty)
                            ctx.Entity.TenantId = ResolveRowTenant(ctx.Services, ctx.Entity.TenantId);
                        if (ctx.Entity.Status == 0)
                            ctx.Entity.Status = (int)BillingClientStatus.Active;
                        EncryptClientTax(ctx.Services, ctx.Entity);
                    },
                    BeforeUpdate = ctx => {
                        RestoreClientCiphertext(ctx.DbContext, ctx.Entity);
                        EncryptClientTax(ctx.Services, ctx.Entity);
                    }
                })
            .Build();
        app.CreateBuilder<BillingDbContext, ChargeEntity, ChargeEntity, ChargeEntity, Guid>("Billing/Charges", "Billing")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    BeforeCreate = ctx => {
                        ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id;
                        if (ctx.Entity.CreatedAt == default)
                            ctx.Entity.CreatedAt = DateTime.UtcNow;
                        if (ctx.Entity.TenantId is null || ctx.Entity.TenantId == Guid.Empty)
                            ctx.Entity.TenantId = ResolveRowTenant(ctx.Services, ctx.Entity.TenantId);
                    },
                    BeforeUpdate = ctx => {
                        var billed = ctx.DbContext.Entry(ctx.Entity).OriginalValues.GetValue<Guid?>(nameof(ChargeEntity.InvoiceId));
                        if (billed != null)
                            throw new BillingInvoiceStateException("A billed charge cannot be edited.");
                    }
                })
            .Build();
        app.CreateBuilder<BillingDbContext, InvoiceEntity, InvoiceEntity, InvoiceEntity, Guid>("Billing/Invoices", "Billing")
            .WithCrud(
                ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new() {
                    DeniedSelectFields = InvoiceDeniedSelectFields
                })
            .Build();
        app.CreateBuilder<BillingDbContext, InvoiceLineEntity, InvoiceLineEntity, InvoiceLineEntity, Guid>("Billing/InvoiceLines", "Billing")
            .WithCrud(ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new())
            .Build();
        app.CreateBuilder<BillingDbContext, PaymentEntity, PaymentEntity, PaymentEntity, Guid>("Billing/Payments", "Billing")
            .WithCrud(ApiFeatureSet.ReadOnly + ExportApiFeature.Instance, new())
            .Build();
        app.CreateBuilder<BillingDbContext, BillerProfileEntity, BillerProfileEntity, BillerProfileEntity, Guid>("Billing/BillerProfiles", "Billing")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    DeniedSelectFields = BillerDeniedSelectFields,
                    BeforeCreate = ctx => ctx.Entity.Id = ctx.Entity.Id == default ? LyoGuid.CreateCombPostgres() : ctx.Entity.Id
                })
            .Build();

        app.MapPost(
            "/Billing/Invoices/from-charges",
            async (CreateInvoiceFromChargesRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                var invoice = await store.CreateInvoiceFromChargesAsync(req.ClientId, req.ChargeIds ?? [], req.TaxRate, req.TenantId, ct).ConfigureAwait(false);
                return Results.Ok(invoice);
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Invoices/draft",
            async (CreateDraftInvoiceRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                var invoice = await store.CreateDraftInvoiceAsync(req.ClientId, req.TaxRate, req.TenantId, ct).ConfigureAwait(false);
                return Results.Ok(invoice);
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Invoices/{id:guid}/lines",
            async (Guid id, InvoiceLineRecord line, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(line);
                return Results.Ok(await store.AddInvoiceLineAsync(id, line, tenantId, ct).ConfigureAwait(false));
            }).WithTags("Billing");

        app.MapDelete(
            "/Billing/Invoices/{id:guid}/lines/{lineId:guid}",
            async (Guid id, Guid lineId, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.RemoveInvoiceLineAsync(id, lineId, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        app.MapDelete(
            "/Billing/Invoices/{id:guid}/draft",
            async (Guid id, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.DeleteDraftInvoiceAsync(id, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        app.MapGet(
            "/Billing/Clients/{clientId:guid}/unbilled-charges",
            async (Guid clientId, IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.ListUnbilledChargesAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");

        app.MapGet(
            "/Billing/Summary",
            async (IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.GetBillingSummaryAsync(tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");

        app.MapGet(
            "/Billing/Invoices/{id:guid}/record",
            async (Guid id, Guid? tenantId, IBillingStore store, CancellationToken ct) => {
                var invoice = await store.GetInvoiceByIdAsync(id, tenantId, ct).ConfigureAwait(false);
                return invoice is null ? Results.NotFound() : Results.Ok(invoice);
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Invoices/{id:guid}/issue",
            async (Guid id, Guid? tenantId, IBillingStore store, CancellationToken ct)
                => Results.Ok(await store.IssueInvoiceAsync(id, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");

        app.MapPost(
            "/Billing/Invoices/{id:guid}/void",
            async (Guid id, Guid? tenantId, IBillingStore store, CancellationToken ct)
                => Results.Ok(await store.VoidInvoiceAsync(id, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");

        app.MapPost(
            "/Billing/Invoices/{id:guid}/pay",
            async (Guid id, RecordInvoicePaymentRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                var payment = await store.RecordPaymentAsync(id, req.Amount, req.Currency, req.Method, req.Reference, req.TenantId, ct).ConfigureAwait(false);
                return Results.Ok(payment);
            }).WithTags("Billing");

        MapPayloadCrud<BillingPersonEntity>(app, "Billing/People");
        MapPayloadCrud<BillingPhoneNumberEntity>(app, "Billing/PhoneNumbers");
        MapPayloadCrud<BillingEmailAddressEntity>(app, "Billing/EmailAddresses");
        MapPayloadCrud<BillingAddressPayloadEntity>(app, "Billing/AddressPayloads");
        MapLinkCrud<BillingContactEntity>(app, "Billing/Contacts");
        MapLinkCrud<BillingPhoneEntity>(app, "Billing/Phones");
        MapLinkCrud<BillingEmailEntity>(app, "Billing/Emails");
        MapLinkCrud<BillingAddressEntity>(app, "Billing/Addresses");

        app.MapPost(
            "/Billing/Clients/{clientId:guid}/contacts",
            async (Guid clientId, AttachBillingContactRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                return Results.Ok(
                    await store.AttachContactAsync(clientId, req.Contact, req.Role, req.Title, req.IsPrimary, req.IsBillingContact, req.TenantId, ct)
                        .ConfigureAwait(false));
            }).WithTags("Billing");
        app.MapGet(
            "/Billing/Clients/{clientId:guid}/contacts",
            async (Guid clientId, IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.ListContactsAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");
        app.MapDelete(
            "/Billing/Contacts/{linkId:guid}",
            async (Guid linkId, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.DetachContactAsync(linkId, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Clients/{clientId:guid}/phones",
            async (Guid clientId, AttachBillingPhoneRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                return Results.Ok(await store.AttachPhoneAsync(clientId, req.Phone, req.Usage, req.IsPrimary, req.Contact, req.TenantId, ct).ConfigureAwait(false));
            }).WithTags("Billing");
        app.MapGet(
            "/Billing/Clients/{clientId:guid}/phones",
            async (Guid clientId, IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.ListPhonesAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");
        app.MapDelete(
            "/Billing/Phones/{linkId:guid}",
            async (Guid linkId, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.DetachPhoneAsync(linkId, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Clients/{clientId:guid}/emails",
            async (Guid clientId, AttachBillingEmailRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                return Results.Ok(await store.AttachEmailAsync(clientId, req.Email, req.Usage, req.IsPrimary, req.Contact, req.TenantId, ct).ConfigureAwait(false));
            }).WithTags("Billing");
        app.MapGet(
            "/Billing/Clients/{clientId:guid}/emails",
            async (Guid clientId, IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.ListEmailsAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");
        app.MapDelete(
            "/Billing/Emails/{linkId:guid}",
            async (Guid linkId, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.DetachEmailAsync(linkId, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        app.MapPost(
            "/Billing/Clients/{clientId:guid}/addresses",
            async (Guid clientId, AttachBillingAddressRequest req, IBillingStore store, CancellationToken ct) => {
                ArgumentHelpers.ThrowIfNull(req);
                return Results.Ok(await store.AttachAddressAsync(clientId, req.Address, req.Usage, req.IsPrimary, req.Contact, req.TenantId, ct).ConfigureAwait(false));
            }).WithTags("Billing");
        app.MapGet(
            "/Billing/Clients/{clientId:guid}/addresses",
            async (Guid clientId, IBillingStore store, Guid? tenantId, CancellationToken ct)
                => Results.Ok(await store.ListAddressesAsync(clientId, tenantId, ct).ConfigureAwait(false)))
            .WithTags("Billing");
        app.MapDelete(
            "/Billing/Addresses/{linkId:guid}",
            async (Guid linkId, IBillingStore store, Guid? tenantId, CancellationToken ct) => {
                await store.DetachAddressAsync(linkId, tenantId, ct).ConfigureAwait(false);
                return Results.NoContent();
            }).WithTags("Billing");

        return app;
    }

    private static void MapPayloadCrud<TEntity>(WebApplication app, string route)
        where TEntity : class
    {
        app.CreateBuilder<BillingDbContext, TEntity, TEntity, TEntity, Guid>(route, "Billing")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    BeforeCreate = ctx => StampPayloadId(ctx.Entity, ctx.Services)
                })
            .Build();
    }

    private static void MapLinkCrud<TEntity>(WebApplication app, string route)
        where TEntity : EntityRelationEntityBase
    {
        app.CreateBuilder<BillingDbContext, TEntity, TEntity, TEntity, Guid>(route, "Billing")
            .WithCrud(
                ApiFeatureSet.DefaultCrud + ExportApiFeature.Instance, new() {
                    BeforeCreate = ctx => StampRelation(ctx.Entity, ctx.Services)
                })
            .Build();
    }

    private static void StampPayloadId(object entity, IServiceProvider services)
    {
        var idProp = entity.GetType().GetProperty("Id");
        if (idProp?.GetValue(entity) is Guid id && id == default)
            idProp.SetValue(entity, LyoGuid.CreateCombPostgres());
        var created = entity.GetType().GetProperty("CreatedTimestamp");
        if (created != null && created.GetValue(entity) is DateTime dt && dt == default)
            created.SetValue(entity, DateTime.UtcNow);
        var tenant = entity.GetType().GetProperty("TenantId");
        if (tenant != null) {
            var tenantVal = tenant.GetValue(entity);
            if (tenantVal is null || tenantVal is Guid empty && empty == Guid.Empty)
                tenant.SetValue(entity, ResolveRowTenant(services, null));
        }
    }

    private static void StampRelation(EntityRelationEntityBase entity, IServiceProvider services)
    {
        if (entity.Id == default)
            entity.Id = LyoGuid.CreateCombPostgres();
        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;
        if (entity.TenantId == Guid.Empty)
            entity.TenantId = ResolveRowTenant(services, null) ?? Guid.Empty;
        if (string.IsNullOrWhiteSpace(entity.ActorEntityType)) {
            entity.ActorEntityType = EntityRefWellKnown.SystemActorType;
            entity.ActorEntityId = EntityRefPersistedGuid.PersistedEntityId(EntityRef.ForGuid(EntityRefWellKnown.SystemActorType, EntityRefWellKnown.SystemActorId));
        }

        if (string.IsNullOrWhiteSpace(entity.Visibility))
            entity.Visibility = EntityRefVisibility.Private;
    }

    private static Guid? ResolveRowTenant(IServiceProvider services, Guid? requested)
        => TenancyResolver.Resolve(
            requested is { } id && id != Guid.Empty ? requested : null,
            new TenancyOptions(),
            services.GetRequiredService<EntityRefOptions>());

    private static void RestoreClientCiphertext(BillingDbContext context, BillingClientEntity entity)
    {
        var original = context.Entry(entity).OriginalValues;
        if (string.IsNullOrWhiteSpace(entity.TaxId)) {
            entity.EncryptedTaxId = original.GetValue<byte[]?>(nameof(BillingClientEntity.EncryptedTaxId));
            entity.TaxIdLast4 = original.GetValue<string?>(nameof(BillingClientEntity.TaxIdLast4));
        }
    }

    private static void EncryptClientTax(IServiceProvider services, BillingClientEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.TaxId))
            return;

        var encryption = services.GetService<IBillingFieldEncryptionService>();
        if (encryption is not { IsEncryptionEnabled: true })
            throw new BillingEncryptionRequiredException("Saving a tax id requires IBillingFieldEncryptionService with a registered IEncryptionService.");

        var tax = entity.TaxId;
        byte[]? encrypted = null;
        entity.TaxIdLast4 = BillingTaxId.Last4(tax);
        encryption.Encrypt(ref tax, ref encrypted);
        entity.EncryptedTaxId = encrypted;
        entity.TaxId = null;
    }
}

/// <summary>Passthrough mapper so query endpoints can use entity-as-both DTOs.</summary>
internal sealed class IdentityLyoMapper : ILyoMapper
{
    public TResult Map<TResult>(object source)
    {
        if (source is TResult typed)
            return typed;

        throw new InvalidOperationException($"No mapping from {source.GetType().Name} to {typeof(TResult).Name}.");
    }

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        if (source is TDest)
            return;

        throw new InvalidOperationException($"No mapping from {typeof(TSource).Name} to {typeof(TDest).Name}.");
    }
}
