import {describe, expect, it} from "vitest";
import {createApiClient, createAsyncApiClient, type ApiResponse, type ApiTransport, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncRecipeApiClient, createRecipeApiClient} from "./recipeClient.js";
import {joinRoutePrefix, RecipeRoutes} from "./routes.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("joinRoutePrefix_joinsOptionalPrefix", () => {
    it("joins /Recipe/Recipes and /api/Recipe/Recipes", () => {
        expect(joinRoutePrefix(undefined, "Recipe/Recipes")).toBe("/Recipe/Recipes");
        expect(joinRoutePrefix("api", "Recipe/Recipes")).toBe("/api/Recipe/Recipes");
    });
});

describe("createAsyncRecipeApiClient_hitsCrudAndReplaceRoutes", () => {
    it("posts CRUD and replace routes", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({isSuccess: true, items: [], queryScore: 0});
        };
        const recipes = createAsyncRecipeApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const id = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

        await recipes.recipes.create({title: "Soup", slug: "soup"});
        await recipes.replaceIngredientLines(id, [{ingredientText: "salt", sortOrder: 0}]);
        await recipes.replaceSteps(id, [{instruction: "Mix", sortOrder: 0}]);
        await recipes.recipes.queryConcrete({Amount: 10});

        expect(calls[0]).toMatchObject({method: "POST", url: "http://x/Recipe/Recipes"});
        expect(JSON.parse(calls[0]!.body!)).toMatchObject({title: "Soup", slug: "soup"});
        expect(calls[1]).toMatchObject({
            method: "PUT",
            url: `http://x/Recipe/Recipes/${id}/IngredientLines`,
        });
        expect(calls[2]).toMatchObject({
            method: "PUT",
            url: `http://x/Recipe/Recipes/${id}/Steps`,
        });
        expect(calls[3]).toMatchObject({
            method: "POST",
            url: "http://x/Recipe/Recipes/QueryConcrete",
        });
        expect(JSON.parse(calls[3]!.body!)).toMatchObject({Amount: 10});
        expect(RecipeRoutes.attachmentsUpload).toBe("Recipe/Attachments/upload");
    });
});

describe("createAsyncRecipeApiClient_uploadPostsFormData", () => {
    it("posts FormData to the upload route", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({id: "att-1", fileId: "file-1", entityType: "Recipe", entityId: "r1"});
        };
        const recipes = createAsyncRecipeApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const file = new Blob(["scan-bytes"], {type: "application/octet-stream"});
        await recipes.attachments.upload({
            entityType: "Recipe",
            entityId: "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee",
            file,
            fileName: "scan.bin",
            role: "Scan",
        });

        expect(calls[0]).toMatchObject({
            method: "POST",
            url: "http://x/Recipe/Attachments/upload",
        });
        expect(calls[0]!.body).toBeDefined();
    });
});

describe("createRecipeApiClient", () => {
    it("hits the same extra routes synchronously", () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: ApiTransport = (req) => {
            calls.push({method: req.method, url: req.url});
            return ok([]);
        };
        const recipes = createRecipeApiClient(createApiClient({baseUrl: "http://x", transport}));
        recipes.addNote(RecipeRoutes.recipes, "r1", {content: "hi"});
        expect(calls[0]).toMatchObject({
            method: "POST",
            url: "http://x/Recipe/Recipes/r1/Notes",
        });
    });
});
