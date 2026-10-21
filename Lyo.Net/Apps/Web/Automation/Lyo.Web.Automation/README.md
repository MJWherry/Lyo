# Lyo.Web.Automation

Locators, JSON automation plans, session abstraction, and plan runners live here as shared models. `AutomationBrowserMetadata` holds each browser's name, environment variables, and
install paths. `AutomationBrowserDetector` resolves an executable from that catalog. This package does not take a dependency on Playwright or Selenium types.

## Features

- `Constants.Rest` spells the automation run routes (`Automation/Run`, cancel, log, snapshots) so the C# client and the TypeScript client share one spelling.
  `IAutomationBrowserSessionFactory.CreateSession` is the host seam that opens a session for a browser kind and a headless flag. `CaptureNetworkAutomationStep` listens for request
  URLs for a duration and stores the ones that match a regular expression. `AutomationRunStoragePaths` is the file-storage layout: `Runs/{runId}` for the run and
  `Runs/{runId}/steps/{stepId}/{stepId}.png` for a step screenshot. Extractions use the same run or step prefix.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Formatter` (direct, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (direct, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `System.Text.Json` `10.0.5` (direct, microsoft, netstandard2.0)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)