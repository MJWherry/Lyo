# lyo-story-api-client

Story API client. Mirrors `Lyo.Story.Client` (`IStoryClient`) on top of `lyo-api-client`.

## Scope

- Graph save, get, get by slug, list, and delete
- Playthrough start, get, moves, choose, advance, set variables, events, and causes
- Routes match `Lyo.Story.Models` `Constants.Rest.Story`

Wire JSON is camelCase. Enums are camelCase strings.

## Usage

```ts
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { createAsyncStoryApiClient } from "lyo-story-api-client";

const api = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const stories = createAsyncStoryApiClient(api, { routePrefix: "Story" });

await stories.saveGraph({ title: "The Heist", slug: "the-heist", nodes: [] });
const page = await stories.listGraphs({ gameKey: "heist" });
const play = await stories.start({ storyId: page[0].id!, playerRef: "ada" });
await stories.choose(play.playthrough.id, { choiceKey: "smash" });
```
