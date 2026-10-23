# lyo-feature-flags-components

React feature-flag UI. Mirrors `Lyo.FeatureFlags.Web.Components`: `FeatureFlagPanel` and `FeatureFlagView`.

`FeatureFlagPanel` lists every flag from `GET /features/manage`. `allowEdit` defaults to false. A switch sets that flag on or off. A percentage or rules flag loses that targeting when the switch changes. `FeatureFlagView` shows its children only when `GET /features` includes the name, compared ordinal-ignore-case.

```tsx
import {FeatureFlagPanel, FeatureFlagView} from "lyo-feature-flags-components";
import "lyo-feature-flags-components/styles.css";
```
