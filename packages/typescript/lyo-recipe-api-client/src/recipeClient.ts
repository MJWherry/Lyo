import type {ApiClient, ApiResponse, CrudResource, SyncCrudResource} from "lyo-api-client";
import {createAsyncCrudResource, createSyncCrudResource} from "lyo-api-client";
import type {AsyncApiClient} from "lyo-api-client";
import type {
    IngredientReq,
    IngredientRes,
    NoteRecord,
    RecipeAttachmentReq,
    RecipeAttachmentRes,
    RecipeCategoryReq,
    RecipeCategoryRes,
    RecipeIngredientLineReq,
    RecipeIngredientLineRes,
    RecipeNoteCreateReq,
    RecipeReq,
    RecipeRes,
    RecipeStepReq,
    RecipeStepRes,
    RecipeTagAddReq,
    RecipeUploadAttachmentArgs,
    TagRecord,
} from "./models.js";
import {
    joinRoutePrefix,
    RecipeRoutes,
    recipeIngredientLinesPath,
    recipeNotesPath,
    recipeStepsPath,
    recipeTagsPath,
} from "./routes.js";

export interface RecipeClientOptions {
    routePrefix?: string;
}

function recipePath(options: RecipeClientOptions | undefined, relative: string): string {
    return joinRoutePrefix(options?.routePrefix, relative);
}

function formDataOf(args: RecipeUploadAttachmentArgs): FormData {
    const form = new FormData();
    form.append("entityType", args.entityType);
    form.append("entityId", args.entityId);
    form.append("role", String(args.role ?? "Document"));
    if (args.caption != null)
        form.append("caption", args.caption);
    form.append("isPrimary", args.isPrimary ? "true" : "false");
    form.append("file", args.file, args.fileName ?? "file.bin");
    return form;
}

export interface RecipeExtraClient {
    getIngredientLines(recipeId: string): ApiResponse<RecipeIngredientLineRes[]>;
    replaceIngredientLines(
        recipeId: string,
        lines: RecipeIngredientLineReq[]
    ): ApiResponse<RecipeIngredientLineRes[]>;
    getSteps(recipeId: string): ApiResponse<RecipeStepRes[]>;
    replaceSteps(recipeId: string, steps: RecipeStepReq[]): ApiResponse<RecipeStepRes[]>;
    getNotes(parentRoute: string, id: string): ApiResponse<NoteRecord[]>;
    addNote(parentRoute: string, id: string, request: RecipeNoteCreateReq): ApiResponse<NoteRecord>;
    deleteNote(parentRoute: string, id: string, noteId: string): ApiResponse<unknown>;
    getTags(parentRoute: string, id: string): ApiResponse<TagRecord[]>;
    addTag(parentRoute: string, id: string, request: RecipeTagAddReq): ApiResponse<TagRecord[]>;
    removeTag(parentRoute: string, id: string, name: string, tagType?: string | null): ApiResponse<unknown>;
}

export interface RecipeApiClient extends RecipeExtraClient {
    categories: SyncCrudResource<RecipeCategoryReq, RecipeCategoryRes>;
    ingredients: SyncCrudResource<IngredientReq, IngredientRes>;
    recipes: SyncCrudResource<RecipeReq, RecipeRes>;
    attachments: SyncCrudResource<RecipeAttachmentReq, RecipeAttachmentRes> & {
        upload(args: RecipeUploadAttachmentArgs): ApiResponse<RecipeAttachmentRes>;
    };
}

export interface AsyncRecipeExtraClient {
    getIngredientLines(recipeId: string): Promise<ApiResponse<RecipeIngredientLineRes[]>>;
    replaceIngredientLines(
        recipeId: string,
        lines: RecipeIngredientLineReq[]
    ): Promise<ApiResponse<RecipeIngredientLineRes[]>>;
    getSteps(recipeId: string): Promise<ApiResponse<RecipeStepRes[]>>;
    replaceSteps(recipeId: string, steps: RecipeStepReq[]): Promise<ApiResponse<RecipeStepRes[]>>;
    getNotes(parentRoute: string, id: string): Promise<ApiResponse<NoteRecord[]>>;
    addNote(parentRoute: string, id: string, request: RecipeNoteCreateReq): Promise<ApiResponse<NoteRecord>>;
    deleteNote(parentRoute: string, id: string, noteId: string): Promise<ApiResponse<unknown>>;
    getTags(parentRoute: string, id: string): Promise<ApiResponse<TagRecord[]>>;
    addTag(parentRoute: string, id: string, request: RecipeTagAddReq): Promise<ApiResponse<TagRecord[]>>;
    removeTag(parentRoute: string, id: string, name: string, tagType?: string | null): Promise<ApiResponse<unknown>>;
}

export interface AsyncRecipeApiClient extends AsyncRecipeExtraClient {
    categories: CrudResource<RecipeCategoryReq, RecipeCategoryRes>;
    ingredients: CrudResource<IngredientReq, IngredientRes>;
    recipes: CrudResource<RecipeReq, RecipeRes>;
    attachments: CrudResource<RecipeAttachmentReq, RecipeAttachmentRes> & {
        upload(args: RecipeUploadAttachmentArgs): Promise<ApiResponse<RecipeAttachmentRes>>;
    };
}

export function createRecipeApiClient(api: ApiClient, options?: RecipeClientOptions): RecipeApiClient {
    const p = (relative: string) => recipePath(options, relative);
    return {
        categories: createSyncCrudResource(api, p(RecipeRoutes.categories)),
        ingredients: createSyncCrudResource(api, p(RecipeRoutes.ingredients)),
        recipes: createSyncCrudResource(api, p(RecipeRoutes.recipes)),
        attachments: {
            ...createSyncCrudResource<RecipeAttachmentReq, RecipeAttachmentRes>(api, p(RecipeRoutes.attachments)),
            upload(args: RecipeUploadAttachmentArgs) {
                return api.request<RecipeAttachmentRes, FormData>({
                    method: "POST",
                    path: p(RecipeRoutes.attachmentsUpload),
                    body: formDataOf(args),
                });
            },
        },
        getIngredientLines(recipeId: string) {
            return api.request<RecipeIngredientLineRes[]>({
                method: "GET",
                path: p(recipeIngredientLinesPath(recipeId)),
            });
        },
        replaceIngredientLines(recipeId: string, lines: RecipeIngredientLineReq[]) {
            return api.request<RecipeIngredientLineRes[], RecipeIngredientLineReq[]>({
                method: "PUT",
                path: p(recipeIngredientLinesPath(recipeId)),
                body: lines,
            });
        },
        getSteps(recipeId: string) {
            return api.request<RecipeStepRes[]>({
                method: "GET",
                path: p(recipeStepsPath(recipeId)),
            });
        },
        replaceSteps(recipeId: string, steps: RecipeStepReq[]) {
            return api.request<RecipeStepRes[], RecipeStepReq[]>({
                method: "PUT",
                path: p(recipeStepsPath(recipeId)),
                body: steps,
            });
        },
        getNotes(parentRoute: string, id: string) {
            return api.request<NoteRecord[]>({
                method: "GET",
                path: p(recipeNotesPath(parentRoute, id)),
            });
        },
        addNote(parentRoute: string, id: string, request: RecipeNoteCreateReq) {
            return api.request<NoteRecord, RecipeNoteCreateReq>({
                method: "POST",
                path: p(recipeNotesPath(parentRoute, id)),
                body: request,
            });
        },
        deleteNote(parentRoute: string, id: string, noteId: string) {
            return api.request({
                method: "DELETE",
                path: `${p(recipeNotesPath(parentRoute, id))}/${noteId}`,
            });
        },
        getTags(parentRoute: string, id: string) {
            return api.request<TagRecord[]>({
                method: "GET",
                path: p(recipeTagsPath(parentRoute, id)),
            });
        },
        addTag(parentRoute: string, id: string, request: RecipeTagAddReq) {
            return api.request<TagRecord[], RecipeTagAddReq>({
                method: "POST",
                path: p(recipeTagsPath(parentRoute, id)),
                body: request,
            });
        },
        removeTag(parentRoute: string, id: string, name: string, tagType?: string | null) {
            return api.request({
                method: "DELETE",
                path: `${p(recipeTagsPath(parentRoute, id))}/${encodeURIComponent(name)}`,
                query: tagType ? {tagType} : undefined,
            });
        },
    };
}

export function createAsyncRecipeApiClient(
    api: AsyncApiClient,
    options?: RecipeClientOptions
): AsyncRecipeApiClient {
    const p = (relative: string) => recipePath(options, relative);
    return {
        categories: createAsyncCrudResource(api, p(RecipeRoutes.categories)),
        ingredients: createAsyncCrudResource(api, p(RecipeRoutes.ingredients)),
        recipes: createAsyncCrudResource(api, p(RecipeRoutes.recipes)),
        attachments: {
            ...createAsyncCrudResource<RecipeAttachmentReq, RecipeAttachmentRes>(api, p(RecipeRoutes.attachments)),
            upload(args: RecipeUploadAttachmentArgs) {
                return api.request<RecipeAttachmentRes, FormData>({
                    method: "POST",
                    path: p(RecipeRoutes.attachmentsUpload),
                    body: formDataOf(args),
                });
            },
        },
        getIngredientLines(recipeId: string) {
            return api.request<RecipeIngredientLineRes[]>({
                method: "GET",
                path: p(recipeIngredientLinesPath(recipeId)),
            });
        },
        replaceIngredientLines(recipeId: string, lines: RecipeIngredientLineReq[]) {
            return api.request<RecipeIngredientLineRes[], RecipeIngredientLineReq[]>({
                method: "PUT",
                path: p(recipeIngredientLinesPath(recipeId)),
                body: lines,
            });
        },
        getSteps(recipeId: string) {
            return api.request<RecipeStepRes[]>({
                method: "GET",
                path: p(recipeStepsPath(recipeId)),
            });
        },
        replaceSteps(recipeId: string, steps: RecipeStepReq[]) {
            return api.request<RecipeStepRes[], RecipeStepReq[]>({
                method: "PUT",
                path: p(recipeStepsPath(recipeId)),
                body: steps,
            });
        },
        getNotes(parentRoute: string, id: string) {
            return api.request<NoteRecord[]>({
                method: "GET",
                path: p(recipeNotesPath(parentRoute, id)),
            });
        },
        addNote(parentRoute: string, id: string, request: RecipeNoteCreateReq) {
            return api.request<NoteRecord, RecipeNoteCreateReq>({
                method: "POST",
                path: p(recipeNotesPath(parentRoute, id)),
                body: request,
            });
        },
        deleteNote(parentRoute: string, id: string, noteId: string) {
            return api.request({
                method: "DELETE",
                path: `${p(recipeNotesPath(parentRoute, id))}/${noteId}`,
            });
        },
        getTags(parentRoute: string, id: string) {
            return api.request<TagRecord[]>({
                method: "GET",
                path: p(recipeTagsPath(parentRoute, id)),
            });
        },
        addTag(parentRoute: string, id: string, request: RecipeTagAddReq) {
            return api.request<TagRecord[], RecipeTagAddReq>({
                method: "POST",
                path: p(recipeTagsPath(parentRoute, id)),
                body: request,
            });
        },
        removeTag(parentRoute: string, id: string, name: string, tagType?: string | null) {
            return api.request({
                method: "DELETE",
                path: `${p(recipeTagsPath(parentRoute, id))}/${encodeURIComponent(name)}`,
                query: tagType ? {tagType} : undefined,
            });
        },
    };
}
