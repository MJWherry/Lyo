import { env, toInt } from "./env.js";
import { buildOptions } from "./queryFactory.js";
import { DEFAULT_PERSON_SELECT_FIELDS } from "./personModels.js";
import {
  selectProjectionQuery as buildSelectProjectionQuery,
  projectionRootScalarsQuery as buildProjectionRootScalarsQuery,
  projectionNestedSelectQuery as buildProjectionNestedSelectQuery,
  projectionUnifiedCollectionQuery as buildProjectionUnifiedCollectionQuery,
  computedCollectionParallelQuery as buildComputedCollectionParallelQuery,
  computedScalarTemplateQuery as buildComputedScalarTemplateQuery,
} from "../../../packages/lyo-person-api-client/dist/index.js";

export const PERSON_SELECT_FIELDS = env("SELECT_FIELDS", DEFAULT_PERSON_SELECT_FIELDS)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean);

/** ProjectionQueryReq body for mixed select + sort (POST /person/QueryProject). */
export function selectProjectionQuery({ start = 0, amount = 1200, include = [] } = {}) {
  return buildSelectProjectionQuery({ start, amount, include, fields: PERSON_SELECT_FIELDS });
}

/** Root scalars only — exercises SQL projection without collection merge. */
export function projectionRootScalarsQuery({ start = 0, amount = 200 } = {}) {
  const fields = env(
    "PROJECTION_ROOT_FIELDS",
    "Id,FirstName,LastName,SourceEntityType,IsActive"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  const query = buildProjectionRootScalarsQuery({ start, amount, fields });
  query.Options = buildOptions({
    totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
    includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
  });
  return query;
}

/** Nested navigation under Select (single collection + leaf paths). */
export function projectionNestedSelectQuery({ start = 0, amount = 200 } = {}) {
  const fields = env(
    "PROJECTION_NESTED_FIELDS",
    "Id,contactaddresses.address.city,contactaddresses.address.postalcode"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  const query = buildProjectionNestedSelectQuery({ start, amount, fields });
  query.Options = buildOptions({
    totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
    includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
  });
  return query;
}

/**
 * Mixed depths under one collection root (unified-root SQL merge + row zip).
 * Override with PROJECTION_UNIFIED_FIELDS (comma-separated).
 */
export function projectionUnifiedCollectionQuery({ start = 0, amount = 200 } = {}) {
  const fields = env(
    "PROJECTION_UNIFIED_FIELDS",
    "contactaddresses.id,contactaddresses.address.streettype,contactaddresses.address.streetname"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  const zipRaw = (env("PROJECTION_ZIP_SIBLING", "true") || "").toLowerCase();
  const zipSibling =
    zipRaw === "false" ? false : zipRaw === "null" ? null : true;

  const query = buildProjectionUnifiedCollectionQuery({
    start,
    amount,
    fields,
    zipSiblingCollectionSelections: zipSibling,
  });
  query.Options = {
    ...buildOptions({
      totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
    }),
    ZipSiblingCollectionSelections: zipSibling,
  };
  return query;
}

/**
 * Computed field with collection-parallel template (dependencies auto-selected server-side).
 * Name and template overridable via COMPUTED_NAME, COMPUTED_TEMPLATE.
 */
export function computedCollectionParallelQuery({ start = 0, amount = 200 } = {}) {
  const name = env("COMPUTED_NAME", "streetLine");
  const template = env(
    "COMPUTED_TEMPLATE",
    "{contactaddresses.address.streettype} {contactaddresses.address.streetname}"
  );

  const zipRaw = (env("COMPUTED_ZIP_SIBLING", "true") || "").toLowerCase();
  const zipSibling =
    zipRaw === "false" ? false : zipRaw === "null" ? null : true;

  const query = buildComputedCollectionParallelQuery({
    start,
    amount,
    name,
    template,
    zipSiblingCollectionSelections: zipSibling,
  });
  query.Options = {
    ...buildOptions({
      totalCountMode: env("COMPUTED_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("COMPUTED_INCLUDE_FILTER_MODE", "Full"),
    }),
    ZipSiblingCollectionSelections: zipSibling,
  };
  return query;
}

/** Scalar-row computed (no collection parallel path). */
export function computedScalarTemplateQuery({ start = 0, amount = 200 } = {}) {
  const query = buildComputedScalarTemplateQuery({
    start,
    amount,
    name: env("COMPUTED_SCALAR_NAME", "fullName"),
    template: env("COMPUTED_SCALAR_TEMPLATE", "{FirstName} {LastName}"),
  });
  query.Options = buildOptions({
    totalCountMode: env("COMPUTED_TOTAL_COUNT_MODE", "None"),
    includeFilterMode: env("COMPUTED_INCLUDE_FILTER_MODE", "Full"),
  });
  return query;
}

export function projectionSlowMs(kind) {
  const key =
    kind === "computed"
      ? "COMPUTED_SLOW_MS"
      : "PROJECTION_SLOW_MS";
  return toInt(key, kind === "computed" ? 2500 : 2200);
}
