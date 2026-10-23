# lyo-story-components

React story builder. Mirrors `Lyo.Story.Web.Components` using `lyo-web-components` and `lyo-story-api-client`.

The graph is the same computed layout as the Blazor package: boxes are 220 by 72, the column gap is 96, and the row gap is 20. Choice arrows follow the last `gotoNode` effect. There is no free-form canvas.

Peer React 19 + Emotion. Wrap the tree in `LyoProvider`.

## Usage

```tsx
"use client";
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { StoryManagement } from "lyo-story-components";
import "lyo-story-components/styles.css";

const api = createAsyncApiClient({
  baseUrl: "/api",
  transport: fetchTransport,
});

export function StoriesPage() {
  return <StoryManagement apiClient={api} routePrefix="Story" tenantId={tenantId} gameKey="heist" />;
}
```

`apiClient` may be that base client or a client from `createAsyncStoryApiClient`.

## Catalog

Same names as the Blazor package.

| Component | Role |
| --- | --- |
| `StoryManagement` | Library of graph headers. Create, open, duplicate, delete. Opens the workbench. |
| `StoryBuilderWorkbench` | Tabs: **Visual**, **Request JSON**, **Play**. |
| `StoryGraphView` | SVG of the computed layout. Click a node to select it. |
| `StoryNodeEditor` | Key, title, scene, presentation (none / text / cutscene), body, media, ending. |
| `StoryChoiceEditor` | Prompt, order, `visibleWhen` and `enabledWhen`. |
| `StoryEffectListEditor` | Set, unset, increment, goto, end playthrough, invoke action. Reorder. |
| `StoryVariableEditor` | Boolean, integer, string, default. |
| `StoryActionEditor` | Named action and its effect list. |
| `StoryReactionEditor` | Trigger, scope node or variable, once, when, effects. |
| `StoryPlayView` | Start, current node, enabled choices, Advance, variable bag, event log, causes for one variable. |

Save stays disabled while checks fail. The open graph autosaves in `clientStore` (`story_builder_state_v1`). Conditions use `WhereClauseBuilder` with `var.*`, `chose.*`, and `visited.*` field presets.
