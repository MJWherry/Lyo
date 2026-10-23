# lyo-fantasy-football-api-client

HTTP client for the Lyo Fantasy Football plugin API. Cookies for ESPN stay on the API host. This package only calls the plugin routes.

```ts
import { createAsyncApiClient } from "lyo-api-client";
import { createAsyncFantasyFootballApiClient } from "lyo-fantasy-football-api-client";

const api = createAsyncFantasyFootballApiClient(createAsyncApiClient({ baseUrl: "http://localhost:5251" }));
const league = await api.refresh(leagueId, seasonId);
const conversations = await api.listConversations(leagueId, seasonId);
```

`routePrefix` defaults to `FantasyFootball`, matching `MapFantasyFootballApi`.
