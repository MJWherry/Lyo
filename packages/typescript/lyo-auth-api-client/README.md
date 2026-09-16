# lyo-auth-api-client

Auth HTTP client. Session routes under `/auth`, self-service tokens under `/tokens`, and admin query routes `Auth/User`, `Auth/Token`, `Auth/Claim`, `Auth/Scope`, `Auth/LinkedIdentity`, and `Auth/Event`.

Login is a browser redirect from `loginUrl`. There is no password grant in this package.

`tokens.create` returns `plaintext` once. `tokens.list` strips `secret`, `secretHash`, and `plaintext`. `tokens.revoke` calls `DELETE /tokens/{id}` and keeps the row. `admin.tokens.delete` is the hard delete.

```ts
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncAuthApiClient, loginUrl} from "lyo-auth-api-client";

const auth = createAsyncAuthApiClient(createAsyncApiClient({baseUrl: "http://localhost:5251", transport: fetchTransport}));
loginUrl("google", "/home");
const me = await auth.me();
const minted = await auth.tokens.create({displayName: "laptop", scopes: ["auth.tokens.read"]});
```
