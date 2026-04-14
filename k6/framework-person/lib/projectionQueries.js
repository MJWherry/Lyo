import { env, toInt } from "./env.js";
import { buildOptions } from "./queryFactory.js";

/** Root scalars only — exercises SQL projection without collection merge. */
export function projectionRootScalarsQuery({ start = 0, amount = 200 } = {}) {
  const fields = env(
    "PROJECTION_ROOT_FIELDS",
    "Id,FirstName,LastName,IsActive"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  return {
    Options: buildOptions({
      totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
    }),
    Start: start,
    Amount: amount,
    Keys: [],
    whereClause: null,
    Include: [],
    Select: fields,
    ComputedFields: [],
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
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

  return {
    Options: buildOptions({
      totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
    }),
    Start: start,
    Amount: amount,
    Keys: [],
    whereClause: null,
    Include: [],
    Select: fields,
    ComputedFields: [],
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
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

  return {
    Options: {
      ...buildOptions({
        totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
        includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
      }),
      ZipSiblingCollectionSelections: zipSibling,
    },
    Start: start,
    Amount: amount,
    Keys: [],
    whereClause: null,
    Include: [],
    Select: fields,
    ComputedFields: [],
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
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

  return {
    Options: {
      ...buildOptions({
        totalCountMode: env("COMPUTED_TOTAL_COUNT_MODE", "None"),
        includeFilterMode: env("COMPUTED_INCLUDE_FILTER_MODE", "Full"),
      }),
      ZipSiblingCollectionSelections: zipSibling,
    },
    Start: start,
    Amount: amount,
    Keys: [],
    whereClause: null,
    Include: [],
    Select: ["contactaddresses.id"],
    ComputedFields: [{ Name: name, Template: template }],
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
}

/** Scalar-row computed (no collection parallel path). */
export function computedScalarTemplateQuery({ start = 0, amount = 200 } = {}) {
  return {
    Options: buildOptions({
      totalCountMode: env("COMPUTED_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("COMPUTED_INCLUDE_FILTER_MODE", "Full"),
    }),
    Start: start,
    Amount: amount,
    Keys: [],
    whereClause: null,
    Include: [],
    Select: ["FirstName", "LastName"],
    ComputedFields: [
      {
        Name: env("COMPUTED_SCALAR_NAME", "fullName"),
        Template: env("COMPUTED_SCALAR_TEMPLATE", "{FirstName} {LastName}"),
      },
    ],
    SortBy: [
      { PropertyName: "LastName", Direction: "Asc", Priority: 0 },
      { PropertyName: "FirstName", Direction: "Asc", Priority: 1 },
    ],
  };
}

export function projectionSlowMs(kind) {
  const key =
    kind === "computed"
      ? "COMPUTED_SLOW_MS"
      : "PROJECTION_SLOW_MS";
  return toInt(key, kind === "computed" ? 2500 : 2200);
}
