# lyo-recipe-api-client

Recipe API client for Lyo. Built on `lyo-api-client` and `lyo-query`. There is no React `lyo-recipe-components` package.

## Scope

- CRUD resources for categories, ingredients, recipes, and attachments
- Extra routes matching `IRecipeClient`: ingredient lines, steps, notes, tags
- `attachments.upload` posts `FormData` (`entityType`, `entityId`, `role`, `caption`, `isPrimary`, `file`) with no content-type restriction

## Usage

```ts
import { createAsyncApiClient, fetchTransport } from "lyo-api-client";
import { createAsyncRecipeApiClient, RecipeRoutes, joinRoutePrefix } from "lyo-recipe-api-client";

const api = createAsyncApiClient({
  baseUrl: process.env.LYO_API_BASE_URL!,
  transport: fetchTransport,
});
const recipes = createAsyncRecipeApiClient(api);

await recipes.recipes.create({ title: "Soup", slug: "soup" });
await recipes.replaceIngredientLines(id, [
  { ingredientText: "salt", sortOrder: 0 },
  { ingredientId: catalogId, quantity: 2, unit: "cup", sortOrder: 1 },
]);
await recipes.replaceSteps(id, [
  { instruction: "Mix", sortOrder: 0 },
  { instruction: "Cook", sortOrder: 1 },
]);
await recipes.attachments.upload({
  entityType: "Recipe",
  entityId: id,
  file,
  fileName: "card.pdf",
  role: "Card",
});
```

`joinRoutePrefix(undefined, "Recipe/Recipes")` is `/Recipe/Recipes`. `joinRoutePrefix("api", "Recipe/Recipes")` is `/api/Recipe/Recipes`.

Query request bodies keep PascalCase (`Amount`, `Include`) from `lyo-query`. Domain DTOs are camelCase to match `LyoJsonSerializerOptions`.
