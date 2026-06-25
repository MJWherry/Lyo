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
import {
  buildSortBy,
  createSeededRng,
  navBranchRates,
  parseFieldPool,
  requestSeed,
  sampleWithoutReplacement,
  shouldRandomize,
  weightedPick,
} from "./workloadShape.js";

export const PERSON_SELECT_FIELDS = env("SELECT_FIELDS", DEFAULT_PERSON_SELECT_FIELDS)
  .split(",")
  .map((x) => x.trim())
  .filter(Boolean);

const DEFAULT_ROOT_FIELDS = "Id,FirstName,LastName,SourceEntityType,IsActive,PreferredName";
const DEFAULT_ADDRESS_FIELDS =
  "contactaddresses.id,contactaddresses.address.city,contactaddresses.address.postalcode,contactaddresses.address.streettype,contactaddresses.address.streetname";
const DEFAULT_PHONE_FIELDS =
  "contactphonenumbers.id,contactphonenumbers.phonenumber.number,contactphonenumbers.phonenumber.countrycode,contactphonenumbers.type";
const DEFAULT_EMAIL_FIELDS =
  "contactemailaddresses.id,contactemailaddresses.emailaddress.email,contactemailaddresses.type";
const DEFAULT_PROJECTION_SORT_FIELDS =
  "LastName,FirstName,Id,SourceEntityType,IsActive,PreferredName";

function projectionRng(args = {}, namespace = "projection") {
  return createSeededRng(
    requestSeed({
      namespace,
      ...args,
    })
  );
}

function withRandomSort(query, args = {}) {
  if (!shouldRandomize("RANDOMIZE_SORTS", false) || !shouldRandomize("QUERYPROJECT_RANDOMIZE_SORTS", false)) {
    // Randomized multi-key sorting over unindexed columns is the dominant cost in this harness; keep it off by default.
    // No queryproject case carries a fixed sort, so fall back to the server default (PK) order.
    query.SortBy = [];
    return query;
  }
  const pool = parseFieldPool(
    ["QUERYPROJECT_SORT_FIELDS", "PROJECTION_SORT_FIELDS", "SORT_FIELDS"],
    DEFAULT_PROJECTION_SORT_FIELDS
  );
  if (pool.length === 0) {
    return query;
  }
  query.SortBy = buildSortBy({
    rng: projectionRng(args, "projection-sort"),
    fieldPool: pool,
    prefix: "QUERYPROJECT",
  });
  return query;
}

function projectionFieldPools() {
  return {
    root: parseFieldPool(["PROJECTION_ROOT_POOL", "PROJECTION_ROOT_FIELDS"], DEFAULT_ROOT_FIELDS),
    address: parseFieldPool(["PROJECTION_ADDRESS_POOL", "PROJECTION_ADDRESS_FIELDS"], DEFAULT_ADDRESS_FIELDS),
    phone: parseFieldPool(["PROJECTION_PHONE_POOL", "PROJECTION_PHONE_FIELDS"], DEFAULT_PHONE_FIELDS),
    email: parseFieldPool(["PROJECTION_EMAIL_POOL", "PROJECTION_EMAIL_FIELDS"], DEFAULT_EMAIL_FIELDS),
  };
}

function selectFromPools({
  args = {},
  minCount = null,
  maxCount = null,
  forceBranch = null,
  rootOnly = false,
} = {}) {
  const rng = projectionRng(args, "projection-select");
  const pools = projectionFieldPools();
  const rates = navBranchRates("QUERYPROJECT");
  const min = minCount ?? toInt("PROJECTION_FIELD_MIN", 2);
  const max = maxCount ?? toInt("PROJECTION_FIELD_MAX", 6);
  const targetCount = Math.max(min, Math.min(max, min + Math.floor(rng() * Math.max(1, max - min + 1))));

  const selected = [];
  const rootPickCount = Math.min(
    Math.max(1, Math.min(pools.root.length, toInt("PROJECTION_ROOT_MIN_FIELDS", 1))),
    pools.root.length
  );
  selected.push(...sampleWithoutReplacement(pools.root, rootPickCount, rng));

  if (!rootOnly) {
    const branchNames = forceBranch
      ? [forceBranch]
      : ["address", "phone", "email"].filter((name) => rng() < (rates[name] ?? 0));
    const effectiveBranches =
      branchNames.length > 0
        ? branchNames
        : [weightedPick(["address", "phone", "email"], (k) => rates[k] ?? 0, rng) || "address"];

    for (const branch of effectiveBranches) {
      const pool = pools[branch] || [];
      if (pool.length === 0) {
        continue;
      }
      const branchMin = Math.min(pool.length, Math.max(1, toInt("PROJECTION_BRANCH_MIN_FIELDS", 1)));
      const branchMax = Math.min(pool.length, Math.max(branchMin, toInt("PROJECTION_BRANCH_MAX_FIELDS", 2)));
      const branchCount = branchMin + Math.floor(rng() * Math.max(1, branchMax - branchMin + 1));
      selected.push(...sampleWithoutReplacement(pool, branchCount, rng));
    }
  }

  const dedup = [...new Set(selected)];
  if (dedup.length > targetCount) {
    return sampleWithoutReplacement(dedup, targetCount, rng);
  }
  if (dedup.length >= min) {
    return dedup;
  }

  // Fill shortfall from all available fields.
  const all = [...new Set([...pools.root, ...pools.address, ...pools.phone, ...pools.email])];
  const missing = min - dedup.length;
  return [...dedup, ...sampleWithoutReplacement(all.filter((f) => !dedup.includes(f)), missing, rng)];
}

function maybeRandomizeSelect(existingFields, args = {}, options = {}) {
  if (!shouldRandomize("RANDOMIZE_PROJECTION_FIELDS", true)) {
    return existingFields;
  }
  if (!shouldRandomize("QUERYPROJECT_RANDOMIZE_PROJECTION_FIELDS", true)) {
    return existingFields;
  }
  return selectFromPools({ args, ...options });
}

/** ProjectionQueryReq body for mixed select + sort (POST /person/QueryProject). */
export function selectProjectionQuery({ start = 0, amount = 1200, include = [], iter = 0, vu = 0, profile = "" } = {}) {
  const query = buildSelectProjectionQuery({
    start,
    amount,
    include,
    fields: maybeRandomizeSelect(PERSON_SELECT_FIELDS, {
      caseId: "select_projection",
      endpointKind: "queryproject",
      iter,
      vu,
      profile,
    }),
  });
  query.Include = []; // QueryProject ignores client Include; keep explicit.
  return withRandomSort(query, {
    caseId: "select_projection",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

/** Root scalars only — exercises SQL projection without collection merge. */
export function projectionRootScalarsQuery({ start = 0, amount = 200, iter = 0, vu = 0, profile = "" } = {}) {
  const fields = env(
    "PROJECTION_ROOT_FIELDS",
    "Id,FirstName,LastName,SourceEntityType,IsActive"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  const query = buildProjectionRootScalarsQuery({
    start,
    amount,
    fields: maybeRandomizeSelect(fields, {
      caseId: "projection_roots",
      endpointKind: "queryproject",
      iter,
      vu,
      profile,
    }, {
      rootOnly: true,
      minCount: Math.max(2, toInt("PROJECTION_ROOT_MIN", 2)),
      maxCount: Math.max(2, toInt("PROJECTION_ROOT_MAX", 6)),
    }),
  });
  query.Options = buildOptions({
    totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
    includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
  });
  return withRandomSort(query, {
    caseId: "projection_roots",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

/** Nested navigation under Select (single collection + leaf paths). */
export function projectionNestedSelectQuery({ start = 0, amount = 200, iter = 0, vu = 0, profile = "" } = {}) {
  const fields = env(
    "PROJECTION_NESTED_FIELDS",
    "Id,contactaddresses.address.city,contactaddresses.address.postalcode"
  )
    .split(",")
    .map((x) => x.trim())
    .filter(Boolean);

  const query = buildProjectionNestedSelectQuery({
    start,
    amount,
    fields: maybeRandomizeSelect(fields, {
      caseId: "projection_nested",
      endpointKind: "queryproject",
      iter,
      vu,
      profile,
    }, {
      minCount: Math.max(2, toInt("PROJECTION_NESTED_MIN", 2)),
      maxCount: Math.max(3, toInt("PROJECTION_NESTED_MAX", 7)),
    }),
  });
  query.Options = buildOptions({
    totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
    includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
  });
  return withRandomSort(query, {
    caseId: "projection_nested",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

/**
 * Mixed depths under one collection root (unified-root SQL merge + row zip).
 * Override with PROJECTION_UNIFIED_FIELDS (comma-separated).
 */
export function projectionUnifiedCollectionQuery({ start = 0, amount = 200, iter = 0, vu = 0, profile = "" } = {}) {
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

  const branchWeights = navBranchRates("QUERYPROJECT");
  const branchRng = projectionRng(
    {
      caseId: "projection_unified",
      endpointKind: "queryproject",
      iter,
      vu,
      profile,
    },
    "projection-unified-branch"
  );
  const forceBranch = weightedPick(["address", "phone", "email"], (k) => branchWeights[k] ?? 0, branchRng) || "address";

  const query = buildProjectionUnifiedCollectionQuery({
    start,
    amount,
    fields: maybeRandomizeSelect(fields, {
      caseId: "projection_unified",
      endpointKind: "queryproject",
      iter,
      vu,
      profile,
    }, {
      forceBranch,
      minCount: Math.max(2, toInt("PROJECTION_UNIFIED_MIN", 2)),
      maxCount: Math.max(3, toInt("PROJECTION_UNIFIED_MAX", 6)),
    }),
    zipSiblingCollectionSelections: zipSibling,
  });
  query.Options = {
    ...buildOptions({
      totalCountMode: env("PROJECTION_TOTAL_COUNT_MODE", "None"),
      includeFilterMode: env("PROJECTION_INCLUDE_FILTER_MODE", "Full"),
    }),
    ZipSiblingCollectionSelections: zipSibling,
  };
  return withRandomSort(query, {
    caseId: "projection_unified",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

/**
 * Computed field with collection-parallel template (dependencies auto-selected server-side).
 * Name and template overridable via COMPUTED_NAME, COMPUTED_TEMPLATE.
 */
export function computedCollectionParallelQuery({ start = 0, amount = 200, iter = 0, vu = 0, profile = "" } = {}) {
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
  return withRandomSort(query, {
    caseId: "computed_collection_parallel",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

/** Scalar-row computed (no collection parallel path). */
export function computedScalarTemplateQuery({ start = 0, amount = 200, iter = 0, vu = 0, profile = "" } = {}) {
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
  query.Select = parseFieldPool(
    ["COMPUTED_SCALAR_SELECT_FIELDS"],
    "FirstName,LastName,Id,SourceEntityType"
  );
  return withRandomSort(query, {
    caseId: "computed_scalar",
    endpointKind: "queryproject",
    iter,
    vu,
    profile,
  });
}

export function projectionSlowMs(kind) {
  const key =
    kind === "computed"
      ? "COMPUTED_SLOW_MS"
      : "PROJECTION_SLOW_MS";
  return toInt(key, kind === "computed" ? 2500 : 2200);
}
