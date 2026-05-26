# Lyo.Email.Web.Components

Blazor (MudBlazor) workbench component for sending email through an injected `IEmailService`.

## Components

- `EmailWorkbench` — full compose-and-send experience for the email service:
    - To / Cc / Bcc chip inputs with email-format validation.
    - From address and From name overrides.
    - Subject, rich-text HTML body via `LyoRichTextEditor`, and a plain-text/raw-HTML fallback editor.
    - Attachment uploads via `LyoFileUpload` (configured for up to five files, with progress and event
      callbacks for `Started`, `Progress`, `Completed`, `Cancelled`, and `Failed`).
    - "Send" action that delegates to `IEmailService.SendEmailAsync(EmailRequestBuilder)` and reports
      success/failure through `ISnackbar`.

The component requires `IEmailService` (registered via `Lyo.Email`) and an `ISnackbar` provider
(registered as part of MudBlazor) in the DI container of the host application.

## Target framework

`net10.0`

## Related projects

- [`Lyo.Email`](../Lyo.Email/README.md)
- [`Lyo.Email.Models`](../Lyo.Email.Models/README.md)
- [`Lyo.Web.Components`](../../../Integration/Web/Lyo.Web.Components/README.md)
