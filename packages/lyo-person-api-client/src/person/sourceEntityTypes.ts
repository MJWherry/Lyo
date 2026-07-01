export const ENDATO_PS_PERSON_ENTITY_TYPE =
    "Lyo.Endato.Postgres.Database.EndatoPsPersonEntity";

export const ENDATO_CE_PERSON_ENTITY_TYPE =
    "Lyo.Endato.Postgres.Database.EndatoCePersonEntity";

export const PERSON_SOURCE_ENTITY_TYPES = [
    ENDATO_PS_PERSON_ENTITY_TYPE,
    ENDATO_CE_PERSON_ENTITY_TYPE,
] as const;

export const DEFAULT_SOURCE_FILTER_VALUES = PERSON_SOURCE_ENTITY_TYPES.join(",");
