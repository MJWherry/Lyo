import type {
  GetByIdReq,
  JoinClause,
  ProjectionQueryReq,
  QueryBuilderMode,
  QueryConcreteReq,
  QueryIncludeFilterMode,
  QueryReq,
  QueryTotalCountMode,
  SortBy,
  SortDirection,
  WhereClause,
} from "lyo-query";
import { defaultGroup, defaultQueryOptions } from "lyo-query";
import { ChipInput } from "./ChipInput.js";
import { WhereClauseBuilder } from "./WhereClauseBuilder.js";

export type QueryBuilderValue = {
  mode: QueryBuilderMode;
  concrete: QueryConcreteReq;
  project: ProjectionQueryReq;
  query: QueryReq;
  get: GetByIdReq;
};

export type QueryBuilderProps = {
  value: QueryBuilderValue;
  onChange: (next: QueryBuilderValue) => void;
  fieldPresets?: readonly string[];
  defaultField?: string;
  includePresets?: readonly string[];
  selectPresets?: readonly string[];
  /** Suggested EntityType values for root Query From/Joins. */
  entityTypePresets?: readonly string[];
  classPrefix?: string;
  /** Modes to show (default all four). */
  modes?: readonly QueryBuilderMode[];
};

/** Root Query Select only accepts alias.property on registry CLR props (no nested nav paths). */
function toRootQuerySelect(select: readonly string[], fromAlias: string): string[] {
  return select
    .filter((f) => {
      const trimmed = f.trim();
      if (!trimmed) return false;
      // Drop nested projection paths (contactaddresses.address.city) — invalid for /Query.
      if (trimmed.includes(".") && !trimmed.startsWith(`${fromAlias}.`)) {
        const parts = trimmed.split(".");
        if (parts.length !== 2) return false;
      }
      if (!trimmed.includes(".")) return true;
      const [, prop] = trimmed.split(".", 2);
      return Boolean(prop) && !prop.includes(".");
    })
    .map((f) => (f.includes(".") ? f : `${fromAlias}.${f}`));
}

const MODE_LABELS: Record<QueryBuilderMode, string> = {
  concrete: "Concrete",
  project: "Projection",
  query: "Query",
  get: "Get",
};

const ALL_MODES: QueryBuilderMode[] = ["concrete", "project", "query", "get"];

export function createDefaultQueryBuilderValue(options?: {
  defaultField?: string;
  entityType?: string;
  select?: string[];
  include?: string[];
  amount?: number;
}): QueryBuilderValue {
  const defaultField = options?.defaultField ?? "FirstName";
  const amount = options?.amount ?? 10;
  const opts = defaultQueryOptions({ TotalCountMode: "Exact" });
  const where = defaultGroup(defaultField);
  const select = options?.select ?? ["Id", "FirstName", "LastName"];
  const include = options?.include ?? [];
  const entityType = options?.entityType ?? "PersonEntity";

  const shared = {
    Start: 0,
    Amount: amount,
    Include: include,
    SortBy: [] as SortBy[],
    whereClause: where as WhereClause,
  };

  const fromAlias = "p";

  return {
    mode: "concrete",
    concrete: {
      Options: { ...opts },
      ...shared,
      Include: [...include],
    },
    project: {
      Options: { ...opts },
      ...shared,
      Include: [],
      Select: [...select],
    },
    query: {
      Options: { ...opts },
      ...shared,
      Include: [],
      From: { Alias: fromAlias, EntityType: entityType },
      Joins: [],
      Select: toRootQuerySelect(select, fromAlias),
    },
    get: {
      id: "",
      Include: [...include],
    },
  };
}

function patchModeShared(
  value: QueryBuilderValue,
  patch: Partial<Pick<QueryConcreteReq, "Start" | "Amount" | "whereClause" | "SortBy" | "Include">> & {
    Options?: Partial<QueryConcreteReq["Options"]>;
    Select?: string[];
  }
): QueryBuilderValue {
  const next = { ...value };
  const applyShared = <T extends QueryConcreteReq | ProjectionQueryReq | QueryReq>(req: T): T => {
    const Options = patch.Options
      ? { ...req.Options, ...patch.Options }
      : req.Options;
    return {
      ...req,
      ...(patch.Start !== undefined ? { Start: patch.Start } : {}),
      ...(patch.Amount !== undefined ? { Amount: patch.Amount } : {}),
      ...(patch.whereClause !== undefined ? { whereClause: patch.whereClause } : {}),
      ...(patch.SortBy !== undefined ? { SortBy: patch.SortBy } : {}),
      ...(patch.Include !== undefined ? { Include: patch.Include } : {}),
      Options,
    };
  };

  next.concrete = applyShared(value.concrete);
  next.project = applyShared(value.project);
  next.query = applyShared(value.query);

  if (patch.Select !== undefined) {
    next.project = { ...next.project, Select: patch.Select };
    next.query = { ...next.query, Select: patch.Select };
  }

  return next;
}

/**
 * Full Lyo query workbench UI: Concrete / Project / Root Query / Get.
 * Does not expose host or endpoint URL configuration — callers wire fixed routes.
 */
export function QueryBuilder({
  value,
  onChange,
  fieldPresets = [],
  defaultField = "FirstName",
  includePresets = [],
  selectPresets = [],
  entityTypePresets = [],
  classPrefix = "lyo-qbuilder",
  modes = ALL_MODES,
}: QueryBuilderProps) {
  const p = classPrefix;
  const mode = value.mode;

  const activeListReq =
    mode === "concrete" ? value.concrete : mode === "project" ? value.project : value.query;

  const setMode = (nextMode: QueryBuilderMode) => onChange({ ...value, mode: nextMode });

  const updateSort = (items: SortBy[]) => {
    onChange(patchModeShared(value, { SortBy: items }));
  };

  const updateWhere = (whereClause: WhereClause) => {
    onChange(patchModeShared(value, { whereClause }));
  };

  return (
    <div className={p}>
      <div className={`${p}__modes`} role="tablist" aria-label="Query mode">
        {modes.map((m) => (
          <button
            key={m}
            type="button"
            role="tab"
            aria-selected={mode === m}
            className={`${p}__mode${mode === m ? ` ${p}__mode--active` : ""}`}
            onClick={() => setMode(m)}
          >
            {MODE_LABELS[m]}
          </button>
        ))}
      </div>

      {mode === "get" ? (
        <GetForm
          classPrefix={p}
          value={value.get}
          includePresets={includePresets}
          onChange={(get) => onChange({ ...value, get })}
        />
      ) : (
        <>
          <section className={`${p}__section`}>
            <h3 className={`${p}__section-title`}>Pagination</h3>
            <div className={`${p}__row`}>
              <label className={`${p}__field`}>
                <span className={`${p}__label`}>Start</span>
                <input
                  type="number"
                  min={0}
                  value={activeListReq.Start ?? 0}
                  onChange={(e) =>
                    onChange(
                      patchModeShared(value, {
                        Start: Math.max(0, Number(e.target.value) || 0),
                      })
                    )
                  }
                />
              </label>
              <label className={`${p}__field`}>
                <span className={`${p}__label`}>Amount</span>
                <input
                  type="number"
                  min={1}
                  max={50}
                  value={activeListReq.Amount ?? 10}
                  onChange={(e) =>
                    onChange(
                      patchModeShared(value, {
                        Amount: Math.min(50, Math.max(1, Number(e.target.value) || 1)),
                      })
                    )
                  }
                />
              </label>
            </div>
          </section>

          <section className={`${p}__section`}>
            <h3 className={`${p}__section-title`}>Options</h3>
            <div className={`${p}__row`}>
              <label className={`${p}__field`}>
                <span className={`${p}__label`}>Total Count Mode</span>
                <select
                  value={activeListReq.Options.TotalCountMode}
                  onChange={(e) =>
                    onChange(
                      patchModeShared(value, {
                        Options: {
                          TotalCountMode: e.target.value as QueryTotalCountMode,
                        },
                      })
                    )
                  }
                >
                  <option value="Exact">Exact</option>
                  <option value="None">None</option>
                  <option value="HasMore">HasMore</option>
                </select>
              </label>
              {mode === "concrete" || mode === "project" ? (
                <label className={`${p}__field`}>
                  <span className={`${p}__label`}>Include Filter Mode</span>
                  <select
                    value={activeListReq.Options.IncludeFilterMode}
                    onChange={(e) =>
                      onChange(
                        patchModeShared(value, {
                          Options: {
                            IncludeFilterMode: e.target.value as QueryIncludeFilterMode,
                          },
                        })
                      )
                    }
                  >
                    <option value="Full">Full</option>
                    <option value="MatchedOnly">MatchedOnly</option>
                  </select>
                </label>
              ) : null}
            </div>
          </section>

          {mode === "concrete" ? (
            <section className={`${p}__section`}>
              <h3 className={`${p}__section-title`}>Include</h3>
              <p className={`${p}__hint`}>
                Navigation paths to expand (Enter to add).
                {includePresets.length
                  ? ` Suggestions: ${includePresets.slice(0, 3).join(", ")}…`
                  : ""}
              </p>
              <ChipInput
                values={value.concrete.Include ?? []}
                onChange={(Include) =>
                  onChange({
                    ...value,
                    concrete: { ...value.concrete, Include },
                    get: { ...value.get, Include },
                  })
                }
                placeholder="contactaddresses.address"
              />
            </section>
          ) : null}

          {mode === "project" || mode === "query" ? (
            <section className={`${p}__section`}>
              <h3 className={`${p}__section-title`}>Select</h3>
              <p className={`${p}__hint`}>
                {mode === "query"
                  ? "Root Query requires alias.property (e.g. p.FirstName, ca.PersonId)."
                  : "Projection paths (Enter to add)."}
                {selectPresets.length
                  ? ` e.g. ${selectPresets.slice(0, 3).join(", ")}`
                  : ""}
              </p>
              <ChipInput
                values={mode === "project" ? value.project.Select : value.query.Select}
                onChange={(Select) => {
                  if (mode === "project") {
                    onChange({
                      ...value,
                      project: { ...value.project, Select },
                    });
                  } else {
                    const alias = value.query.From.Alias || "p";
                    onChange({
                      ...value,
                      query: {
                        ...value.query,
                        Select: toRootQuerySelect(Select, alias),
                      },
                    });
                  }
                }}
                placeholder={mode === "query" ? "p.FirstName" : "FirstName"}
              />
            </section>
          ) : null}

          {mode === "query" ? (
            <RootFromJoinsForm
              classPrefix={p}
              value={value.query}
              entityTypePresets={entityTypePresets}
              onChange={(query) => onChange({ ...value, query })}
            />
          ) : null}

          <section className={`${p}__section`}>
            <h3 className={`${p}__section-title`}>Sort By</h3>
            <SortByEditor
              classPrefix={p}
              items={activeListReq.SortBy ?? []}
              fieldPresets={fieldPresets}
              onChange={updateSort}
            />
          </section>

          <section className={`${p}__section`}>
            <h3 className={`${p}__section-title`}>Filter</h3>
            <WhereClauseBuilder
              value={activeListReq.whereClause ?? defaultGroup(defaultField)}
              onChange={updateWhere}
              fieldPresets={fieldPresets}
              defaultField={defaultField}
            />
          </section>
        </>
      )}
    </div>
  );
}

function GetForm({
  classPrefix: p,
  value,
  onChange,
  includePresets,
}: {
  classPrefix: string;
  value: GetByIdReq;
  onChange: (next: GetByIdReq) => void;
  includePresets: readonly string[];
}) {
  return (
    <>
      <section className={`${p}__section`}>
        <h3 className={`${p}__section-title`}>Get by Id</h3>
        <p className={`${p}__hint`}>GET /person/{"{id}"} — no where clause or paging.</p>
        <label className={`${p}__field ${p}__field--grow`}>
          <span className={`${p}__label`}>Id</span>
          <input
            value={value.id}
            onChange={(e) => onChange({ ...value, id: e.target.value.trim() })}
            placeholder="guid"
          />
        </label>
      </section>
      <section className={`${p}__section`}>
        <h3 className={`${p}__section-title`}>Include</h3>
        <p className={`${p}__hint`}>
          Optional navigation includes.
          {includePresets.length ? ` e.g. ${includePresets.slice(0, 2).join(", ")}` : ""}
        </p>
        <ChipInput
          values={value.Include ?? []}
          onChange={(Include) => onChange({ ...value, Include })}
          placeholder="contactaddresses.address"
        />
      </section>
    </>
  );
}

function SortByEditor({
  classPrefix: p,
  items,
  onChange,
  fieldPresets,
}: {
  classPrefix: string;
  items: SortBy[];
  onChange: (next: SortBy[]) => void;
  fieldPresets: readonly string[];
}) {
  return (
    <div className={`${p}__sort-list`}>
      {items.length === 0 ? (
        <p className={`${p}__hint`}>No sort — API default order.</p>
      ) : null}
      {items.map((item, index) => (
        <div key={index} className={`${p}__sort-item`}>
          <div className={`${p}__row`}>
            <label className={`${p}__field ${p}__field--grow`}>
              <span className={`${p}__label`}>Property</span>
              <input
                list={fieldPresets.length ? `${p}-sort-fields` : undefined}
                value={item.PropertyName}
                onChange={(e) => {
                  const next = items.slice();
                  next[index] = { ...item, PropertyName: e.target.value };
                  onChange(next);
                }}
              />
            </label>
            <label className={`${p}__field`}>
              <span className={`${p}__label`}>Direction</span>
              <select
                value={item.Direction}
                onChange={(e) => {
                  const next = items.slice();
                  next[index] = {
                    ...item,
                    Direction: e.target.value as SortDirection,
                  };
                  onChange(next);
                }}
              >
                <option value="Asc">Asc</option>
                <option value="Desc">Desc</option>
              </select>
            </label>
            <button
              type="button"
              className="lyo-qb__btn"
              onClick={() => onChange(items.filter((_, i) => i !== index))}
            >
              Remove
            </button>
          </div>
        </div>
      ))}
      {fieldPresets.length ? (
        <datalist id={`${p}-sort-fields`}>
          {fieldPresets.map((f) => (
            <option key={f} value={f} />
          ))}
        </datalist>
      ) : null}
      <button
        type="button"
        className="lyo-qb__btn"
        onClick={() =>
          onChange([
            ...items,
            {
              PropertyName: fieldPresets[0] ?? "Id",
              Direction: "Asc",
              Priority: items.length,
            },
          ])
        }
      >
        + Sort
      </button>
    </div>
  );
}

const DEFAULT_JOIN_ENTITY = "ContactAddressEntity";

function nextJoinAlias(existing: readonly JoinClause[]): string {
  const used = new Set(existing.map((j) => j.Alias.toLowerCase()));
  if (!used.has("ca")) return "ca";
  let i = 2;
  while (used.has(`j${i}`)) i += 1;
  return `j${i}`;
}

function RootFromJoinsForm({
  classPrefix: p,
  value,
  onChange,
  entityTypePresets,
}: {
  classPrefix: string;
  value: QueryReq;
  onChange: (next: QueryReq) => void;
  entityTypePresets: readonly string[];
}) {
  const joins = value.Joins ?? [];
  const entityListId = `${p}-entity-types`;

  return (
    <>
      <section className={`${p}__section`}>
        <h3 className={`${p}__section-title`}>From</h3>
        <div className={`${p}__row`}>
          <label className={`${p}__field`}>
            <span className={`${p}__label`}>Alias</span>
            <input
              value={value.From.Alias}
              onChange={(e) =>
                onChange({
                  ...value,
                  From: { ...value.From, Alias: e.target.value },
                })
              }
            />
          </label>
          <label className={`${p}__field ${p}__field--grow`}>
            <span className={`${p}__label`}>EntityType</span>
            <input
              value={value.From.EntityType}
              list={entityTypePresets.length ? entityListId : undefined}
              onChange={(e) =>
                onChange({
                  ...value,
                  From: { ...value.From, EntityType: e.target.value },
                })
              }
              placeholder="PersonEntity"
            />
          </label>
        </div>
      </section>

      <section className={`${p}__section`}>
        <div className={`${p}__row`} style={{ justifyContent: "space-between" }}>
          <h3 className={`${p}__section-title`} style={{ margin: 0 }}>
            Joins
          </h3>
          <button
            type="button"
            className="lyo-qb__btn"
            onClick={() => {
              const alias = nextJoinAlias(joins);
              const fromAlias = value.From.Alias || "p";
              const join: JoinClause = {
                Alias: alias,
                EntityType: DEFAULT_JOIN_ENTITY,
                Type: "Left",
                On: [{ From: `${fromAlias}.Id`, To: `${alias}.PersonId` }],
              };
              const select = [...(value.Select ?? [])];
              for (const path of [`${alias}.Id`, `${alias}.PersonId`]) {
                if (!select.includes(path)) select.push(path);
              }
              onChange({ ...value, Joins: [...joins, join], Select: select });
            }}
          >
            + Join
          </button>
        </div>
        {joins.length === 0 ? (
          <p className={`${p}__hint`}>
            No joins — Select from the From alias only. + Join defaults to{" "}
            {DEFAULT_JOIN_ENTITY} on PersonId.
          </p>
        ) : null}
        <div className={`${p}__join-list`}>
          {joins.map((join, index) => (
            <div key={index} className={`${p}__join-item`}>
              <div className={`${p}__row`}>
                <label className={`${p}__field`}>
                  <span className={`${p}__label`}>Alias</span>
                  <input
                    value={join.Alias}
                    onChange={(e) => {
                      const Joins = joins.slice();
                      Joins[index] = { ...join, Alias: e.target.value };
                      onChange({ ...value, Joins });
                    }}
                  />
                </label>
                <label className={`${p}__field ${p}__field--grow`}>
                  <span className={`${p}__label`}>EntityType</span>
                  <input
                    value={join.EntityType}
                    list={entityTypePresets.length ? entityListId : undefined}
                    onChange={(e) => {
                      const Joins = joins.slice();
                      Joins[index] = { ...join, EntityType: e.target.value };
                      onChange({ ...value, Joins });
                    }}
                  />
                </label>
                <label className={`${p}__field`}>
                  <span className={`${p}__label`}>Type</span>
                  <select
                    value={join.Type}
                    onChange={(e) => {
                      const Joins = joins.slice();
                      Joins[index] = {
                        ...join,
                        Type: e.target.value as JoinClause["Type"],
                      };
                      onChange({ ...value, Joins });
                    }}
                  >
                    <option value="Left">Left</option>
                    <option value="Inner">Inner</option>
                  </select>
                </label>
                <button
                  type="button"
                  className="lyo-qb__btn"
                  onClick={() =>
                    onChange({
                      ...value,
                      Joins: joins.filter((_, i) => i !== index),
                    })
                  }
                >
                  Remove
                </button>
              </div>
              <div className={`${p}__row`}>
                <label className={`${p}__field ${p}__field--grow`}>
                  <span className={`${p}__label`}>On.From</span>
                  <input
                    value={join.On[0]?.From ?? ""}
                    onChange={(e) => {
                      const Joins = joins.slice();
                      const On = [...(join.On.length ? join.On : [{ From: "", To: "" }])];
                      On[0] = { ...On[0], From: e.target.value };
                      Joins[index] = { ...join, On };
                      onChange({ ...value, Joins });
                    }}
                  />
                </label>
                <label className={`${p}__field ${p}__field--grow`}>
                  <span className={`${p}__label`}>On.To</span>
                  <input
                    value={join.On[0]?.To ?? ""}
                    onChange={(e) => {
                      const Joins = joins.slice();
                      const On = [...(join.On.length ? join.On : [{ From: "", To: "" }])];
                      On[0] = { ...On[0], To: e.target.value };
                      Joins[index] = { ...join, On };
                      onChange({ ...value, Joins });
                    }}
                  />
                </label>
              </div>
            </div>
          ))}
        </div>
        {entityTypePresets.length ? (
          <datalist id={entityListId}>
            {entityTypePresets.map((t) => (
              <option key={t} value={t} />
            ))}
          </datalist>
        ) : null}
      </section>
    </>
  );
}

/** Serialize the active mode request for JSON preview / BFF body. */
export function activeRequestPreview(value: QueryBuilderValue): unknown {
  switch (value.mode) {
    case "concrete":
      return value.concrete;
    case "project":
      return value.project;
    case "query":
      return value.query;
    case "get":
      return {
        method: "GET",
        path: `/person/${value.get.id || "{id}"}`,
        include: value.get.Include ?? [],
      };
  }
}
