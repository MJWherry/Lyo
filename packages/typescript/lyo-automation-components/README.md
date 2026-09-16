# lyo-automation-components

Three layers share one plan document. None of them reach into another host's browser.

- `lyo-automation-components/plan` has no React. The CLI and these components both use it.
- This package is the editor, the step builder, and the plan library. They take props and callbacks. They do not read disk, call `fetch`, or know whether the host is the studio or a gateway.
- `lyo-automation-components/run` exports `AutomationRunPanel`, which posts a plan through `lyo-automation-api-client`. A future gateway imports the editor and the library and does not import that entry.

Recording, network tools, and in-browser playback stay on the studio drawer.

## Plan module

```ts
import {createEmptyPlan, planFileName, stepCatalog} from "lyo-automation-components/plan";
```

`createEmptyPlan` is a named plan with no steps. `planFileName` is one `.json` segment. `stepCatalog` is the source of the editor dropdown.

## Forms

Each form shows a readable label. The JSON `type` stays the discriminator the runner accepts.

- Navigate, Reload, Delay, Find element, Find and act, Extract element
- Extract sources: variable, attribute names, and the element list
- Store literal, Store string list, Store template, Page URL, Page title
- Capture network: variable, URL regex, listen milliseconds
- Download URLs: URL-list variable and directory (`downloadUrlsToDirectory`)

A step the catalog does not own stays in the document. Edit it in the JSON pane (`JsonEditor`, tree and raw). Apply replaces the steps from that JSON. Invalid JSON leaves the plan alone.

## Library and editor

```tsx
import {AutomationPlanEditor, AutomationPlanLibrary} from "lyo-automation-components";

<AutomationPlanLibrary plans={plans} onCreate={create} onOpen={open} />
<AutomationPlanEditor value={plan} onChange={setPlan} />
```

## Run panel

```tsx
import {AutomationRunPanel} from "lyo-automation-components/run";

<AutomationRunPanel client={automation} plan={plan} />
```
