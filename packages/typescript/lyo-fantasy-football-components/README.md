# lyo-fantasy-football-components

React workbench for Fantasy Football chats and trades. It talks to `lyo-fantasy-football-api-client`, which calls the plugin API. ESPN cookies stay on the API host.

```tsx
import { FantasyFootballWorkbench } from "lyo-fantasy-football-components";

<FantasyFootballWorkbench client={client} leagueId={leagueId} seasonId={seasonId} />
```

`topicTitle` labels a direct message with the other member. `tradeSummary` lists the players sent and received.
