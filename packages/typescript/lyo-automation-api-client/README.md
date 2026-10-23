# lyo-automation-api-client

Starts an automation plan on the Lyo automation HTTP API and reads the run log and viewport snapshots. The browser stays on the API host.

```ts
import {createAsyncApiClient} from "lyo-api-client";
import {createAsyncAutomationApiClient} from "lyo-automation-api-client";

const automation = createAsyncAutomationApiClient(createAsyncApiClient({baseUrl: "http://localhost:5000"}));
const accepted = await automation.start({plan, headless: true});
const run = await automation.waitUntilSettled(accepted.runId, 400);
```
