export type RecipeStatus = "Draft" | "Active" | "Archived" | 0 | 1 | 2;
export type RecipeDifficulty = "Easy" | "Medium" | "Hard" | 0 | 1 | 2;
export type RecipeAttachmentRole = "Document" | "Scan" | "Card" | "Image" | "Other" | 0 | 1 | 2 | 3 | 4;

export interface RecipeCategoryReq {
    parentId?: string | null;
    name: string;
    slug: string;
}

export interface RecipeCategoryRes {
    id: string;
    parentId?: string | null;
    name: string;
    slug: string;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

export interface IngredientReq {
    name: string;
    slug: string;
    defaultUnit?: string | null;
    notes?: string | null;
}

export interface IngredientRes {
    id: string;
    name: string;
    slug: string;
    defaultUnit?: string | null;
    notes?: string | null;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

export interface RecipeReq {
    title: string;
    slug: string;
    description?: string | null;
    servings?: number | null;
    prepMinutes?: number | null;
    cookMinutes?: number | null;
    difficulty?: RecipeDifficulty;
    status?: RecipeStatus;
    sourceName?: string | null;
    sourceUrl?: string | null;
    yieldLabel?: string | null;
    categoryId?: string | null;
}

export interface RecipeRes {
    id: string;
    title: string;
    slug: string;
    description?: string | null;
    servings?: number | null;
    prepMinutes?: number | null;
    cookMinutes?: number | null;
    difficulty?: RecipeDifficulty;
    status?: RecipeStatus;
    sourceName?: string | null;
    sourceUrl?: string | null;
    yieldLabel?: string | null;
    categoryId?: string | null;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

export interface RecipeIngredientLineReq {
    ingredientId?: string | null;
    ingredientText?: string | null;
    sortOrder: number;
    quantity?: number | null;
    unit?: string | null;
    preparation?: string | null;
    isOptional?: boolean;
    groupLabel?: string | null;
}

export interface RecipeIngredientLineRes extends RecipeIngredientLineReq {
    id: string;
    recipeId: string;
}

export interface RecipeStepReq {
    sortOrder: number;
    instruction: string;
    durationMinutes?: number | null;
    groupLabel?: string | null;
}

export interface RecipeStepRes extends RecipeStepReq {
    id: string;
    recipeId: string;
}

export interface RecipeAttachmentReq {
    entityType: string;
    entityId: string;
    fileId: string;
    role?: RecipeAttachmentRole;
    caption?: string | null;
    isPrimary?: boolean;
    sortOrder?: number;
}

export interface RecipeAttachmentRes extends RecipeAttachmentReq {
    id: string;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

export interface RecipeNoteCreateReq {
    content: string;
}

export interface RecipeTagAddReq {
    name: string;
    tagType?: string;
    slug?: string | null;
}

export interface NoteRecord {
    id: string;
    content: string;
    subjectEntityType?: string;
    subjectEntityId?: string;
}

export interface TagRecord {
    id?: string;
    name: string;
    tagType?: string;
    slug?: string | null;
}

export interface RecipeUploadAttachmentArgs {
    entityType: string;
    entityId: string;
    file: Blob;
    fileName?: string;
    role?: RecipeAttachmentRole;
    caption?: string | null;
    isPrimary?: boolean;
}
