# lyo-config-components

React Config management UI. Mirrors `Lyo.Config.Web.Components` (`ConfigManagement`) using `lyo-web-components` and `lyo-config-api-client`.

Peer React 19 + Emotion. Wrap the tree in `LyoProvider`.

```tsx
"use client";
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {ConfigManagement} from "lyo-config-components";
import "lyo-config-components/styles.css";

const api = createAsyncApiClient({baseUrl: "/api", transport: fetchTransport});

export function ConfigPage() {
  return <ConfigManagement apiClient={api} initialSubjectEntityType="App" initialSubjectEntityId="gateway:local" />;
}
```

Tabs: Definitions (projected grid on `Config/Definition`), Resolved, Definition history, Value history. Delete and revert go through the config client. Values stay plaintext. `isEncrypted` is only a flag.
