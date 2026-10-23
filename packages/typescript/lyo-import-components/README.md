# lyo-import-components

React import workbench. One component covers JSON, CSV, XLSX, YAML, XML, and TOML. Duplicate targets are blocked before upload. The preview shows at most 50 good rows and every issue. The host saves the returned rows.

```tsx
import {ImportWorkbench} from "lyo-import-components";
import "lyo-import-components/styles.css";

<ImportWorkbench apiClient={api} onImported={(response) => save(response)} />
```
