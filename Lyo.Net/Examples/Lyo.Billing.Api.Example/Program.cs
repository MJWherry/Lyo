using System.Security.Cryptography;
using System.Text;
using Lyo.Api;
using Lyo.Api.Middleware;
using Lyo.Billing;
using Lyo.Billing.Api;
using Lyo.Billing.Postgres;
using Lyo.Cache;
using Lyo.Json;
using Lyo.Encryption.Extensions;
using Lyo.KeyStore;
using Lyo.Ledger.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddLyoApiCompression();
builder.Services.ConfigureHttpJsonOptions(o => LyoJsonSerializerOptions.ApplyTo(o.SerializerOptions));
builder.Services.AddLocalCache();
builder.Services.AddLyoQueryServices();
builder.Services.AddLedgerApiFromConfiguration(builder.Configuration);
builder.Services.AddBillingApiFromConfiguration(builder.Configuration);
builder.Services.AddKeyedLocalKeyStore(
    BillingEncryptionKeys.Fields, ks => {
        var seed = SHA256.HashData(Encoding.UTF8.GetBytes("lyo-billing-fields-dev-key/v1"));
        ks.AddKey(BillingEncryptionKeys.Fields, "v1", seed);
        ks.SetCurrentVersion(BillingEncryptionKeys.Fields, "v1");
    });
builder.Services.AddEncryptionServiceKeyed(BillingEncryptionKeys.Fields, BillingEncryptionKeys.Fields);
builder.Services.AddBillingFieldEncryption(BillingEncryptionKeys.Fields);
var application = builder.Build();
application.UseMiddleware<LoggingMiddleware>();
application.UseLyoApiCompression();
if (application.Environment.IsDevelopment()) {
    application.MapOpenApi();
    application.MapScalarApiReference();
}

application.BuildLedgerGroup();
application.BuildBillingGroup();
application.Run();
