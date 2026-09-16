/** Typed JSON payload. Clients send plaintext. The API encrypts when the definition says so. */
export interface ConfigValue {
    typeName: string;
    json: string;
}

/** Allowed key for an entity type. */
export interface ConfigDefinitionRecord {
    id?: string;
    subjectEntityType: string;
    key: string;
    forValueType: string;
    description?: string | null;
    isRequired?: boolean;
    defaultValue?: ConfigValue | null;
    isEncrypted?: boolean;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

/** Value bound to one entity instance. Plaintext in `value.json`. */
export interface ConfigBindingRecord {
    id?: string;
    definitionId: string;
    key: string;
    subjectEntityType: string;
    subjectEntityId: string;
    value: ConfigValue;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
}

export interface ConfigDefinitionRevisionRecord extends ConfigDefinitionRecord {
    definitionId?: string;
    revision?: number;
}

export interface ConfigBindingRevisionRecord extends ConfigBindingRecord {
    bindingId?: string;
    revision?: number;
}

export interface ResolvedConfigItemRecord {
    definition: ConfigDefinitionRecord;
    binding?: ConfigBindingRecord | null;
}

export interface ResolvedConfigRecord {
    subjectEntityType: string;
    subjectEntityId: string;
    items: ResolvedConfigItemRecord[];
}

export interface ConfigRevertRevisionRequest {
    revision: number;
}

/** 304 means the caller's etag still matches. There is no record. */
export interface ConfigResolveResult {
    outcome: 200 | 304;
    record?: ResolvedConfigRecord;
    etag?: string;
}
