import type { ComparisonOperator, WhereClause } from "lyo-query";
import {
  COMPARISON_OPERATORS,
  coerceValueForComparison,
  defaultCondition,
  defaultGroup,
  fromMultiValueStrings,
  isMultiValueComparison,
  parseConditionValue,
  toMultiValueStrings,
  valueToInput,
} from "lyo-query";
import { ChipInput } from "./ChipInput.js";

export type WhereClauseBuilderProps = {
  value: WhereClause;
  onChange: (next: WhereClause) => void;
  /** Called when the user clicks the node remove (×) control. Root usually omits this. */
  onRemove?: () => void;
  /** Field suggestions for the condition field datalist. */
  fieldPresets?: readonly string[];
  /** Default field used when adding a new condition. */
  defaultField?: string;
  /** CSS class prefix (default `lyo-qb`). */
  classPrefix?: string;
  depth?: number;
};

function looksLikeDateField(field: string): boolean {
  return /date|time|utc|at$/i.test(field);
}

function toDatetimeLocalValue(value: unknown): string {
  if (value == null || value === "") return "";
  const raw = String(value);
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) {
    // Already datetime-local-ish: 2024-01-15T10:30
    if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(raw)) return raw.slice(0, 16);
    return "";
  }
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function fromDatetimeLocalValue(raw: string): string | null {
  if (!raw.trim()) return null;
  const d = new Date(raw);
  if (Number.isNaN(d.getTime())) return raw;
  return d.toISOString();
}

/**
 * Nested And/Or group + condition editor for Lyo.Query `whereClause` payloads.
 * Unstyled beyond BEM-ish classes under {@link WhereClauseBuilderProps.classPrefix};
 * import `lyo-query-components/styles.css` or supply your own.
 */
export function WhereClauseBuilder({
  value,
  onChange,
  onRemove,
  fieldPresets = [],
  defaultField = "Id",
  classPrefix = "lyo-qb",
  depth = 0,
}: WhereClauseBuilderProps) {
  const p = classPrefix;
  const listId = `${p}-fields-${depth}-${value.$type}`;

  const removeBtn = onRemove ? (
    <button
      type="button"
      className={`${p}__remove`}
      onClick={onRemove}
      aria-label="Remove"
      title="Remove"
    >
      ×
    </button>
  ) : null;

  if (value.$type === "group") {
    return (
      <div className={`${p}__node ${p}__node--group`}>
        <div className={`${p}__header`}>
          <span className={`${p}__badge ${p}__badge--group`}>Group</span>
          <label className={`${p}__field`}>
            <span className={`${p}__label`}>Operator</span>
            <select
              value={value.Operator}
              onChange={(e) =>
                onChange({
                  ...value,
                  Operator: e.target.value as "And" | "Or",
                })
              }
            >
              <option value="And">And</option>
              <option value="Or">Or</option>
            </select>
          </label>
          <button
            type="button"
            className={`${p}__btn`}
            onClick={() =>
              onChange({
                ...value,
                Children: [...value.Children, defaultCondition(defaultField)],
              })
            }
          >
            + Condition
          </button>
          <button
            type="button"
            className={`${p}__btn`}
            onClick={() =>
              onChange({
                ...value,
                Children: [...value.Children, defaultGroup(defaultField)],
              })
            }
          >
            + Group
          </button>
          {removeBtn}
        </div>
        <div className={`${p}__children`}>
          {value.Children.map((child, index) => (
            <div key={index} className={`${p}__child`}>
              <WhereClauseBuilder
                value={child}
                depth={depth + 1}
                fieldPresets={fieldPresets}
                defaultField={defaultField}
                classPrefix={classPrefix}
                onChange={(next) => {
                  const Children = value.Children.slice();
                  Children[index] = next;
                  onChange({ ...value, Children });
                }}
                onRemove={
                  value.Children.length > 1
                    ? () => {
                        const Children = value.Children.filter((_, i) => i !== index);
                        onChange({ ...value, Children });
                      }
                    : undefined
                }
              />
            </div>
          ))}
        </div>
      </div>
    );
  }

  const multi = isMultiValueComparison(value.Comparison);
  const dateField = looksLikeDateField(value.Field);

  return (
    <div className={`${p}__node ${p}__node--condition`}>
      <div className={`${p}__header`}>
        <span className={`${p}__badge`}>Condition</span>
        <button
          type="button"
          className={`${p}__btn`}
          onClick={() =>
            onChange({
              $type: "group",
              Operator: "And",
              Children: [value, defaultCondition(defaultField)],
            })
          }
        >
          Wrap in group
        </button>
        {removeBtn}
      </div>
      <div className={`${p}__row`}>
        <label className={`${p}__field`}>
          <span className={`${p}__label`}>Field</span>
          <input
            list={fieldPresets.length ? listId : undefined}
            value={value.Field}
            onChange={(e) => onChange({ ...value, Field: e.target.value })}
          />
          {fieldPresets.length ? (
            <datalist id={listId}>
              {fieldPresets.map((f) => (
                <option key={f} value={f} />
              ))}
            </datalist>
          ) : null}
        </label>
        <label className={`${p}__field`}>
          <span className={`${p}__label`}>Comparison</span>
          <select
            value={value.Comparison}
            onChange={(e) => {
              const Comparison = e.target.value as ComparisonOperator;
              onChange({
                ...value,
                Comparison,
                Value: coerceValueForComparison(Comparison, value.Value),
              });
            }}
          >
            {COMPARISON_OPERATORS.map((op) => (
              <option key={op} value={op}>
                {op}
              </option>
            ))}
          </select>
        </label>
        <label className={`${p}__field ${p}__field--grow`}>
          <span className={`${p}__label`}>
            Value{" "}
            {multi
              ? "(chips — Enter to add)"
              : dateField
                ? "(datetime)"
                : "(use null for null)"}
          </span>
          {multi ? (
            <ChipInput
              values={toMultiValueStrings(value.Value)}
              onChange={(next) =>
                onChange({
                  ...value,
                  Value: fromMultiValueStrings(next),
                })
              }
              placeholder="Value + Enter"
            />
          ) : dateField ? (
            <input
              type="datetime-local"
              value={toDatetimeLocalValue(value.Value)}
              onChange={(e) =>
                onChange({
                  ...value,
                  Value: fromDatetimeLocalValue(e.target.value),
                })
              }
            />
          ) : (
            <input
              value={valueToInput(value.Value)}
              onChange={(e) =>
                onChange({
                  ...value,
                  Value: parseConditionValue(value.Comparison, e.target.value),
                })
              }
            />
          )}
        </label>
      </div>
    </div>
  );
}
