# Lyo.Web.Automation.Studio

Opens a visible Playwright window. The drawer is the only place for recording, network tools, and in-browser playback.

**Record** turns clicks, typing, and selects into an `AutomationPlan`. Its step list is where delay, store literal, page URL, page title, download URLs, capture network, store captured URLs, extract images, links, text, and attribute, and drag-to-text all get added.

**Play** loads plan JSON and runs it in the same window. `--plan` puts a file into that box when the window opens. Recording still starts empty.

**Tools** is the network tracker: start and stop, filter, the request popup, replay in a new tab, and extract a selected JSON value into a plan step.

The plan library, the editor, and the step forms live in `lyo-automation-components`. A local page lists `./plans`. A future gateway can host those same components. It does not get this drawer.

The drawer starts on the left. Pause holds the current plan and Play continues it. Stop ends recording and enables Save. Start begins another plan in the same window. Save writes a JSON file into the output directory shown above the plan (default `./plans`). You can drag the dock edge to resize it. Steps in the plan are numbered.

```text
dotnet run --project Lyo.Net/Apps/Web/Automation/Lyo.Web.Automation.Studio -- \
  --url https://example.com --name checkout --plan plans/checkout.json
```

`--out` is an optional directory. A path that ends in `.json` uses that file's folder. Close the browser, or press Enter in the terminal, to exit. Collapse and dock clicks are not recorded. While a plan is playing, those clicks are not recorded either.
