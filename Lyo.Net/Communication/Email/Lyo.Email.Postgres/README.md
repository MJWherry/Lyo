# Lyo.Email.Postgres

PostgreSQL EF Core store for email mailbox logs (`EmailLogEntity` / `EmailAttachmentLogEntity`). The package never sends or fetches mail. It wires `EmailDbContext` so workers or gateways can persist send and receive outcomes after [`Lyo.Email`](../Lyo.Email/README.md) (outbound) or a host IMAP/POP client (inbound). There is no FileStorage id on the schema; hosts that save HTML write their own file and record the path.

## Features

- **Schema only.** `email.email_logs` and `email.email_attachment_logs`. This package does not send SMTP, fetch IMAP/POP, or subscribe to `EmailService` events.
- **Direction.** `EmailDirection.Outbound` (default) or `Inbound`, matching SMS mailbox logs.
- **Bodies.** Plain text on the row (`TextBody`). HTML is a host-owned file whose name and path are stored (`HtmlFileName` / `HtmlFilePath`), not a FileStorage id.
- **Attachments.** Metadata only: file name, content type, byte length, optional JSON bag, sort order. Bytes are not stored.
- **DI.** `AddEmailDbContext` / `AddEmailDbContextFactory` / `AddEmailDbContextFactoryFromConfiguration`.

## Examples

### Injection helpers

```csharp
services.AddEmailDbContextFactoryFromConfiguration(builder.Configuration);
// or
services.AddEmailDbContextFactory(opts =>
{
    opts.ConnectionString = "Host=...;Database=...;Username=...;Password=...";
    opts.EnableAutoMigrations = true;
});
```

### Insert after send

```csharp
emailService.EmailSent += async (_, args) =>
{
    var result = args.EmailResult;
    var request = result.Data;
    string? htmlFileName = null;
    string? htmlFilePath = null;
    if (!string.IsNullOrWhiteSpace(request?.HtmlBody))
    {
        htmlFileName = $"{Guid.NewGuid():N}.html";
        htmlFilePath = Path.Combine(htmlArchiveRoot, htmlFileName);
        await File.WriteAllTextAsync(htmlFilePath, request.HtmlBody);
    }

    await using var context = await factory.CreateDbContextAsync();
    context.EmailLogs.Add(new EmailLogEntity
    {
        Direction = EmailDirection.Outbound,
        FromAddress = request?.FromAddress,
        Subject = request?.Subject,
        TextBody = request?.TextBody,
        HtmlFileName = htmlFileName,
        HtmlFilePath = htmlFilePath,
        IsSuccess = result.IsSuccess,
        Message = result.SmtpResponse,
        MessageId = result.MessageId,
        SentTimestamp = result.SentDate,
        ErrorMessage = result.Errors?.FirstOrDefault()?.Message
    });
    await context.SaveChangesAsync();
};
```

## Types

- `EmailDbContext`. EF Core context exposing `DbSet<EmailLogEntity>` (`email_logs`) and `DbSet<EmailAttachmentLogEntity>` (`email_attachment_logs`). Default schema is `email`. `SaveChanges` and `SaveChangesAsync` stamp `CreatedTimestamp` on insert and `UpdatedTimestamp` on update for both entities.
- `EmailDbContextFactory`. Design-time factory for EF Core tools.
- `EmailDirection`. `Outbound` (default) or `Inbound`.
- `EmailLogEntity`. Direction, sender, recipients (jsonb), subject, `TextBody`, host-owned `HtmlFileName` / `HtmlFilePath`, success flag, SMTP or fetch note, error message, MIME `MessageId`, `SentTimestamp` / `ReceivedTimestamp`, and audit timestamps. Attachment bytes and HTML content are not stored on this row.
- `EmailAttachmentLogEntity`. Attachment metadata only (file name, `ContentType`, `SizeBytes`, `MetadataJson`, `SortOrder`) linked back to an `EmailLogEntity`. No FileStorage id.
- `PostgresEmailOptions`. An `IPostgresMigrationConfig` implementation. Section name is `PostgresEmail`. Default schema is `email`. `EnableAutoMigrations` defaults to `false`.

## Injection helpers

Every extension hangs off `IServiceCollection`. Hosts map and insert; this package does not subscribe to send events.

| Extension | Description |
| -------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddEmailDbContext(string connectionString)` | Wires an `IDbContextFactory<EmailDbContext>` plus a scoped `EmailDbContext` taken from the factory. |
| `AddEmailDbContextFactory(PostgresEmailOptions options)` | Registers `IDbContextFactory<EmailDbContext>` against `options.ConnectionString` with the `email` migrations history schema, plus support for the `Lyo.Postgres` migration runner. |
| `AddEmailDbContextFactory(Action<PostgresEmailOptions> configure)` | Same as above, using an inline configuration callback. |
| `AddEmailDbContextFactoryFromConfiguration(IConfiguration configuration, string configSectionName = PostgresEmailOptions.SectionName)` | Reads `PostgresEmailOptions` from configuration (section defaults to `PostgresEmail`) and wires the factory. |

## Supported frameworks

`net10.0`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)