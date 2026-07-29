import { useCallback, useId, useState, type ClipboardEvent, type KeyboardEvent } from "react";

const VALUE_SEPARATORS = /[,;\t\n\r\uFF0C]+/;

export type ChipInputProps = {
  values: readonly string[];
  onChange: (next: string[]) => void;
  placeholder?: string;
  /** Allow Backspace on empty input to remove the last chip. */
  allowBackspaceDelete?: boolean;
  showClearButton?: boolean;
  classPrefix?: string;
  disabled?: boolean;
};

function parseValues(raw: string, splitSeparators: boolean): string[] {
  const trimmed = raw.trim();
  if (!trimmed) return [];
  if (!splitSeparators) return [trimmed];
  return trimmed
    .split(VALUE_SEPARATORS)
    .map((v) => v.trim())
    .filter((v) => v.length > 0);
}

/**
 * Enter / comma chip list input (Blazor {@code LyoChipInput} parity).
 * Used for multi-value comparisons ({@code In}/{@code NotIn}) and Select/Include lists.
 */
export function ChipInput({
  values,
  onChange,
  placeholder = "Type and press Enter to add",
  allowBackspaceDelete = true,
  showClearButton = true,
  classPrefix = "lyo-chip",
  disabled = false,
}: ChipInputProps) {
  const p = classPrefix;
  const inputId = useId();
  const [draft, setDraft] = useState("");

  const addFromRaw = useCallback(
    (raw: string, splitSeparators: boolean) => {
      const pending = parseValues(raw, splitSeparators);
      if (pending.length === 0) {
        setDraft("");
        return;
      }
      const next = [...values];
      for (const value of pending) {
        if (!next.includes(value)) next.push(value);
      }
      onChange(next);
      setDraft("");
    },
    [onChange, values]
  );

  const removeAt = useCallback(
    (index: number) => {
      onChange(values.filter((_, i) => i !== index));
    },
    [onChange, values]
  );

  const onKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" || e.key === ",") {
      e.preventDefault();
      if (draft.trim()) addFromRaw(draft, e.key === ",");
      return;
    }
    if (
      e.key === "Backspace" &&
      allowBackspaceDelete &&
      draft.length === 0 &&
      values.length > 0
    ) {
      e.preventDefault();
      onChange(values.slice(0, -1));
    }
  };

  const onPaste = (e: ClipboardEvent<HTMLInputElement>) => {
    const text = e.clipboardData.getData("text");
    if (!text || !VALUE_SEPARATORS.test(text)) return;
    e.preventDefault();
    addFromRaw(`${draft}${text}`, true);
  };

  return (
    <div className={`${p}`}>
      <div className={`${p}__box`} aria-disabled={disabled || undefined}>
        {values.map((item, index) => (
          <span key={`${item}-${index}`} className={`${p}__chip`}>
            <span className={`${p}__chip-text`}>{item}</span>
            <button
              type="button"
              className={`${p}__chip-remove`}
              aria-label={`Remove ${item}`}
              disabled={disabled}
              onClick={() => removeAt(index)}
            >
              ×
            </button>
          </span>
        ))}
        <input
          id={inputId}
          className={`${p}__input`}
          value={draft}
          disabled={disabled}
          placeholder={values.length === 0 ? placeholder : ""}
          onChange={(e) => setDraft(e.target.value)}
          onKeyDown={onKeyDown}
          onPaste={onPaste}
          onBlur={() => {
            if (draft.trim()) addFromRaw(draft, true);
          }}
        />
        {showClearButton && values.length > 0 ? (
          <button
            type="button"
            className={`${p}__clear`}
            aria-label="Clear all"
            title="Clear all"
            disabled={disabled}
            onClick={() => {
              onChange([]);
              setDraft("");
            }}
          >
            Clear
          </button>
        ) : null}
      </div>
    </div>
  );
}
