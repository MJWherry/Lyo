# lyo-auth-components

React auth UI. Mirrors `Lyo.Authentication.Web.Components`: login, profile, tokens, debug, and `AuthAdminPanel`. Profile shows a read-only grant list when an authorization client is passed. Auth admin includes a Grants tab.

Login links redirect to `/auth/login/{provider}`. The password card renders only when the host passes `onPasswordSignIn`.

Token revoke calls `DELETE /tokens/{id}` and keeps the row. The admin token grid hard-deletes through `Auth/Token`.

```tsx
import {LoginPage, AuthAdminPanel} from "lyo-auth-components";
import "lyo-auth-components/styles.css";
```
