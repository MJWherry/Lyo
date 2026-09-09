/** Wire shape of Lyo.Api.Models PropertyMetadata (camelCase JSON). */
export type PropertyMetadata = {
    name: string;
    type: string;
    nullable: boolean;
};

/** Wire shape of Lyo.Api.Models TypeMetadata. */
export type TypeMetadata = {
    typeName: string;
    properties: PropertyMetadata[];
};

/**
 * Response for typed CreateBuilder `GET {baseRoute}/Metadata`
 * (`EndpointMetadataResponse`).
 */
export type EndpointMetadataResponse = {
    entity?: TypeMetadata | null;
    request?: TypeMetadata | null;
    response?: TypeMetadata | null;
    keyPropertyName: string;
    keyType: string;
};

/** Wire shape of Lyo.Api.Models EntityTypeMetadata (dynamic CRUD). */
export type EntityTypeMetadata = {
    entityType: string;
    keyPropertyName: string;
    keyType: string;
    properties: PropertyMetadata[];
};

/**
 * Response for dynamic CRUD `GET {baseRoute}/Metadata`
 * (`CrudMetadataResponse`).
 */
export type CrudMetadataResponse = {
    entityTypes: EntityTypeMetadata[];
};
