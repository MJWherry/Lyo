"use client";

import {useEffect, useLayoutEffect, useMemo, useRef, useState, type CSSProperties, type KeyboardEvent} from "react";
import {ApiClientError, type AsyncApiClient} from "lyo-api-client";
import {createAsyncAlgorithmApiClient, type AlgorithmDefinition, type AlgorithmPreview, type AlgorithmRevision, type AsyncAlgorithmApiClient} from "lyo-algorithms-api-client";
import {clientStore, entriesFrom, LyoAlert, LyoButton, LyoCheckbox, LyoMenuItem, LyoPaper, LyoParameterValueGrid, LyoStack, LyoSwitch, LyoTabSet, LyoTextField, type LyoParameterEntry} from "lyo-web-components";
import {
  ALGORITHM_WORKBENCH_KEY,
  clrTypeName,
  contextObject,
  defaultAlias,
  emptyDraft,
  fieldJson,
  fromPayload,
  insert,
  isIdentifier,
  isNumericAlias,
  issues,
  readWorkbenchState,
  setParameter,
  textFromField,
  toPayload,
  toRevision,
  type AlgorithmContextRow,
  type AlgorithmDraft,
} from "./draft.js";
import {placeSuggest} from "./caret.js";
import {bareNames, identifierAt, suggestions} from "./palette.js";
import {attackContext, attackDraft, fishingContext, fishingDraft, scheduleContext, scheduleDraft} from "./presets.js";

const SAVE_MS = 450;

export interface AlgorithmWorkbenchProps {
  apiClient?: AsyncApiClient;
  routePrefix?: string;
  examples?: AlgorithmContextRow[];
}

export function AlgorithmWorkbench({apiClient, routePrefix, examples = []}: AlgorithmWorkbenchProps) {
  const client = useMemo(() => (apiClient ? createAsyncAlgorithmApiClient(apiClient, {routePrefix}) : null), [apiClient, routePrefix]);
  const [draft, setDraft] = useState<AlgorithmDraft>(emptyDraft);
  const [context, setContext] = useState<AlgorithmContextRow[]>(examples);
  const [definitions, setDefinitions] = useState<AlgorithmDefinition[]>([]);
  const [revisions, setRevisions] = useState<AlgorithmRevision[]>([]);
  const [preview, setPreview] = useState<AlgorithmPreview | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [focused, setFocused] = useState<string | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const [label, setLabel] = useState("live");
  const [labelRevision, setLabelRevision] = useState(1);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const stored = readWorkbenchState(JSON.stringify(clientStore.get(ALGORITHM_WORKBENCH_KEY)));
    if (stored) {
      setDraft(stored.draft);
      setContext(stored.context);
    }
    setReady(true);
  }, []);

  useEffect(() => {
    if (!client) return;
    void client.list().then(setDefinitions).catch((reason: unknown) => setError(messageOf(reason)));
  }, [client]);

  useEffect(() => {
    if (!ready) return;
    const handle = window.setTimeout(() => clientStore.set(ALGORITHM_WORKBENCH_KEY, {draft, context}), SAVE_MS);
    return () => window.clearTimeout(handle);
  }, [draft, context, ready]);

  const bagNames = [...draft.parameters.map((row) => row.name), ...context.map((row) => row.name)].filter(isIdentifier);
  const catalogKeys = definitions.map((row) => row.key);
  const formulaNames = bareNames(draft.source);
  const draftIssues = issues(draft);
  const entries = useMemo(() => runnerEntries(draft, context), [draft, context]);

  const apply = (next: AlgorithmDraft, rows: AlgorithmContextRow[]) => {
    const declared = next.parameters.some((row) => isIdentifier(row.name)) ? next : declareParameters(next, rows);
    setDraft(declared);
    setContext(rows);
    setPreview(null);
  };

  const openKey = async (key: string) => {
    if (!client) return;
    const rows = await client.getRevisions(key);
    setRevisions(rows);
    const current = rows[0];
    const definition = definitions.find((row) => row.key === key);
    if (current) setDraft({...fromPayload(current.kind, current.source, current.payload), key, name: definition?.name ?? undefined});
    else setDraft((currentDraft) => ({...currentDraft, key, name: definition?.name ?? currentDraft.name}));
  };

  const run = async () => {
    if (!client) {
      setPreview(null);
      setError("Pass apiClient to run a preview.");
      return;
    }
    setPreview(null);
    setError(null);
    try {
      const result = await client.preview({
        key: draft.key,
        kind: draft.kind,
        source: draft.source,
        payload: toPayload(draft),
        context: contextObject(declaredRows(draft, context)),
      });
      setPreview(result);
    } catch (reason) {
      setPreview(null);
      setError(messageOf(reason));
    }
  };

  const removeKeys = async (keys: string[]) => {
    if (!client) {
      setError("Pass apiClient to delete.");
      return;
    }
    const failures: string[] = [];
    const removed: string[] = [];
    for (const key of keys) {
      try {
        await client.delete(key);
        removed.push(key);
      } catch (reason) {
        failures.push(messageOf(reason));
      }
    }
    setSelected((current) => current.filter((key) => !removed.includes(key)));
    try {
      setDefinitions(await client.list());
    } catch (reason) {
      failures.push(messageOf(reason));
    }
    if (removed.includes(draft.key)) {
      setDraft(emptyDraft());
      setContext([]);
      setRevisions([]);
      setPreview(null);
    }
    setError(failures.length === 0 ? null : failures.join(" "));
  };

  return (
    <div style={{display: "flex", gap: 12, height: "calc(100dvh - 88px)", minHeight: 0}}>
      <LyoPaper variant="outlined" sx={{p: 2, width: 280, flex: "0 0 280px", display: "flex", flexDirection: "column", minHeight: 0, overflow: "hidden"}}>
        <LyoStack spacing={1} sx={{flex: "0 0 auto"}}>
          <strong>Catalog</strong>
          <LyoButton variant="contained" onClick={() => { setDraft(emptyDraft()); setContext([]); setPreview(null); }}>New draft</LyoButton>
          <LyoStack direction="row" spacing={1}>
            <LyoButton variant="outlined" disabled={!isKey(draft.key)} onClick={() => void removeKeys([draft.key])}>Delete</LyoButton>
            <LyoButton variant="outlined" disabled={selected.length === 0} onClick={() => void removeKeys(selected)}>Delete selected</LyoButton>
          </LyoStack>
        </LyoStack>
        <div style={{flex: "1 1 auto", minHeight: 0, overflowY: "auto"}}>
          {definitions.map((row) => (
            <div key={row.key} style={{display: "flex", alignItems: "center", gap: 4, background: row.key === draft.key ? "rgba(25,118,210,0.12)" : undefined}}>
              <LyoCheckbox
                checked={selected.includes(row.key)}
                inputProps={{"aria-label": `Select ${row.key}`}}
                onChange={(_event, checked) => setSelected((current) => checked ? [...current, row.key] : current.filter((key) => key !== row.key))}
              />
              <button type="button" onClick={() => void openKey(row.key)} style={{flex: 1, textAlign: "left", background: "transparent", border: 0, textTransform: "none", font: "inherit", cursor: "pointer"}}>
                {row.key}
              </button>
            </div>
          ))}
        </div>
      </LyoPaper>
      <LyoPaper variant="outlined" sx={{p: 2, flex: 1, minWidth: 0, minHeight: 0, overflow: "auto"}}>
        <LyoTabSet
          keepPanelsAlive
          queryParameter={null}
          tabs={[
            {
              id: "editor",
              text: "Editor",
              content: (
                <LyoStack spacing={1}>
                  <LyoStack direction="row" spacing={1}>
                    <LyoButton variant="outlined" size="small" onClick={() => apply(fishingDraft(), fishingContext())}>Fishing derby</LyoButton>
                    <LyoButton variant="outlined" size="small" onClick={() => apply(attackDraft(), attackContext())}>Attack</LyoButton>
                    <LyoButton variant="outlined" size="small" onClick={() => apply(scheduleDraft(), scheduleContext())}>Schedule</LyoButton>
                  </LyoStack>
                  <div style={{display: "flex", gap: 8, flexWrap: "wrap"}}>
                    <LyoTextField size="small" label="Key" value={draft.key} onChange={(event) => setDraft({...draft, key: event.target.value})} sx={{flex: "1 1 8rem"}} />
                    <LyoTextField size="small" label="Name" value={draft.name ?? ""} onChange={(event) => setDraft({...draft, name: event.target.value})} sx={{flex: "1 1 8rem"}} />
                    <LyoTextField select size="small" label="Kind" value={draft.kind} onChange={(event) => setDraft({...draft, kind: event.target.value as AlgorithmDraft["kind"]})} sx={{flex: "1 1 8rem"}}>
                      {["expression", "named", "discreteMap", "rangeBand", "linear", "composite"].map((kind) => (
                        <LyoMenuItem key={kind} value={kind}>{kind}</LyoMenuItem>
                      ))}
                    </LyoTextField>
                    <LyoTextField size="small" label="Result" value={draft.result} onChange={(event) => setDraft({...draft, result: event.target.value})} sx={{flex: "1 1 8rem"}} />
                  </div>
                  {draft.kind === "expression" || draft.kind === "named" ? (
                    draft.kind === "named" ? (
                      <LyoTextField size="small" label="Startup key" value={draft.source} onChange={(event) => setDraft({...draft, source: event.target.value})} />
                    ) : (
                      <ExpressionEditor source={draft.source} bagNames={bagNames} catalogKeys={catalogKeys} focused={focused} onChange={(source) => setDraft({...draft, source})} onFocus={setFocused} onChip={addMissing} />
                    )
                  ) : null}
                  {draft.kind === "discreteMap" ? <MapEditor draft={draft} onChange={setDraft} /> : null}
                  {draft.kind === "rangeBand" ? <BandEditor draft={draft} onChange={setDraft} /> : null}
                  {draft.kind === "linear" ? <LinearEditor draft={draft} onChange={setDraft} /> : null}
                  {draft.kind === "composite" ? <CompositeEditor draft={draft} onChange={setDraft} /> : null}
                  <TypePanel draft={draft} formulaNames={draft.kind === "expression" ? formulaNames : null} onChange={setDraft} />
                  {draftIssues.map((issue) => <LyoAlert key={issue} severity="warning">{issue}</LyoAlert>)}
                  <LyoButton variant="contained" disabled={!isKey(draft.key) || draftIssues.length > 0} onClick={() => void save(client)}>Save revision</LyoButton>
                  {revisions.map((revision) => (
                    <LyoStack key={revision.revision} direction="row" spacing={1}>
                      <span>{revision.revision} · {revision.kind}</span>
                      <LyoButton size="small" onClick={() => setDraft({...fromPayload(revision.kind, revision.source, revision.payload), key: draft.key, name: draft.name})}>Open</LyoButton>
                      <LyoButton size="small" onClick={() => void client?.revert(draft.key, {revision: revision.revision}).then(() => openKey(draft.key))}>Revert</LyoButton>
                    </LyoStack>
                  ))}
                  <LyoStack direction="row" spacing={1}>
                    <LyoTextField size="small" label="Label" value={label} onChange={(event) => setLabel(event.target.value)} />
                    <LyoTextField size="small" label="Revision" type="number" value={labelRevision} onChange={(event) => setLabelRevision(Number(event.target.value))} />
                    <LyoButton variant="outlined" onClick={() => void client?.setLabel(draft.key, label, {revision: labelRevision, isMutable: true})}>Set label</LyoButton>
                  </LyoStack>
                </LyoStack>
              ),
            },
            {
              id: "runner",
              text: "Runner",
              content: (
                <LyoStack spacing={1}>
                  <LyoParameterValueGrid
                    entries={entries}
                    highlightedKey={focused}
                    showDivider={false}
                    title="Parameters"
                    onChange={(next) => setContext(contextFromEntries(draft, context, next))}
                  />
                  <LyoButton variant="contained" onClick={() => void run()}>Run</LyoButton>
                  {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
                  {preview ? <strong>{preview.result} = {JSON.stringify(preview.value)}</strong> : null}
                  <Breakdown steps={preview?.steps ?? []} />
                </LyoStack>
              ),
            },
          ]}
        />
      </LyoPaper>
    </div>
  );

  function addMissing(name: string) {
    setFocused(name);
    if (draft.parameters.some((row) => row.name === name)) return;
    const alias = draft.parameters.find((row) => row.name === name)?.alias ?? defaultAlias(name);
    setDraft(setParameter(draft, name, alias));
    setContext((rows) => rows.some((row) => row.name === name) ? rows : [...rows, {name, alias, text: ""}]);
  }

  async function save(current: AsyncAlgorithmApiClient | null) {
    if (!current) {
      setError("Pass apiClient to save a revision.");
      return;
    }
    try {
      await current.saveRevision(draft.key, toRevision(draft));
      setDefinitions(await current.list());
      await openKey(draft.key);
      setError(null);
    } catch (reason) {
      setError(messageOf(reason));
    }
  }
}

function declareParameters(draft: AlgorithmDraft, rows: AlgorithmContextRow[]): AlgorithmDraft {
  return rows.filter((row) => isIdentifier(row.name)).reduce((current, row) => setParameter(current, row.name, row.alias), draft);
}

function declaredRows(draft: AlgorithmDraft, rows: AlgorithmContextRow[]): AlgorithmContextRow[] {
  return draft.parameters.filter((row) => isIdentifier(row.name)).map((row) => rows.find((item) => item.name === row.name) ?? {name: row.name, alias: row.alias, text: ""});
}

function runnerEntries(draft: AlgorithmDraft, rows: AlgorithmContextRow[]): LyoParameterEntry[] {
  return entriesFrom(declaredRows(draft, rows).map((row) => ({
    key: row.name,
    type: clrTypeName(row.alias),
    value: fieldJson(row.alias, row.text),
    required: false,
  })));
}

function contextFromEntries(draft: AlgorithmDraft, rows: AlgorithmContextRow[], entries: readonly LyoParameterEntry[]): AlgorithmContextRow[] {
  const next = rows.map((row) => ({...row}));
  for (const entry of entries) {
    const parameter = draft.parameters.find((row) => row.name === entry.definition.key);
    if (!parameter) continue;
    const text = textFromField(parameter.alias, entry.value);
    const existing = next.find((row) => row.name === parameter.name);
    if (existing) {
      existing.alias = parameter.alias;
      existing.text = text;
    } else {
      next.push({name: parameter.name, alias: parameter.alias, text});
    }
  }
  return next;
}

function TypePanel({draft, formulaNames, onChange}: {draft: AlgorithmDraft; formulaNames: readonly string[] | null; onChange: (draft: AlgorithmDraft) => void}) {
  const numeric = ["byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal"];
  const parameters = formulaNames === null
    ? draft.parameters.map((row, index) => ({row, index}))
    : formulaNames.flatMap((name) => {
        const index = draft.parameters.findIndex((row) => row.name === name);
        return index < 0 ? [] : [{row: draft.parameters[index]!, index}];
      });
  return (
    <div style={{display: "flex", flexDirection: "column", gap: 4}}>
      {draft.kind === "rangeBand" || draft.kind === "linear" ? (
        <LyoTextField select size="small" label="Input type" value={draft.inputType || "double"} onChange={(event) => onChange({...draft, inputType: event.target.value})} sx={{maxWidth: "12rem"}}>
          {numeric.filter((alias) => isNumericAlias(alias)).map((alias) => <LyoMenuItem key={alias} value={alias}>{alias}</LyoMenuItem>)}
        </LyoTextField>
      ) : null}
      {parameters.map(({row, index}) => (
        <div key={`p${index}`} style={{display: "flex", alignItems: "center", gap: 8}}>
          {formulaNames === null ? (
            <input aria-label="Name" value={row.name} onChange={(event) => patchRow(draft.parameters, index, {name: event.target.value}, (next) => onChange({...draft, parameters: next}))} style={lineInput} />
          ) : (
            <span style={{flex: 1, minWidth: 0, fontFamily: "ui-monospace, monospace", fontSize: "0.875rem"}}>{row.name}</span>
          )}
          <input aria-label="Alias" value={row.alias} onChange={(event) => patchRow(draft.parameters, index, {alias: event.target.value}, (next) => onChange({...draft, parameters: next}))} style={lineInput} />
          <LyoButton size="small" onClick={() => onChange({...draft, parameters: draft.parameters.filter((_, item) => item !== index)})}>Remove</LyoButton>
        </div>
      ))}
      {formulaNames === null ? <LyoButton size="small" onClick={() => onChange({...draft, parameters: [...draft.parameters, {name: "", alias: "double"}]})}>Add parameter</LyoButton> : null}
      {draft.calls.map((row, index) => (
        <div key={`c${index}`} style={{display: "flex", alignItems: "center", gap: 8}}>
          <input aria-label="Catalog key" placeholder="Catalog key" value={row.name} onChange={(event) => patchRow(draft.calls, index, {name: event.target.value}, (calls) => onChange({...draft, calls}))} style={lineInput} />
          <input aria-label="Call result" placeholder="Result" value={row.alias} onChange={(event) => patchRow(draft.calls, index, {alias: event.target.value}, (calls) => onChange({...draft, calls}))} style={lineInput} />
          <LyoButton size="small" onClick={() => onChange({...draft, calls: draft.calls.filter((_, item) => item !== index)})}>Remove</LyoButton>
        </div>
      ))}
      <LyoButton size="small" onClick={() => onChange({...draft, calls: [...draft.calls, {name: "", alias: "double"}]})}>Add call</LyoButton>
    </div>
  );
}

const lineInput: CSSProperties = {flex: 1, minWidth: 0, height: 28, padding: "0 8px", borderRadius: 4, border: "1px solid rgba(255,255,255,0.23)", background: "transparent", color: "inherit", font: "inherit"};

function ExpressionEditor({
  source, bagNames, catalogKeys, focused, onChange, onFocus, onChip,
}: {
  source: string;
  bagNames: string[];
  catalogKeys: string[];
  focused: string | null;
  onChange: (source: string) => void;
  onFocus: (name: string | null) => void;
  onChip: (name: string) => void;
}) {
  const area = useRef<HTMLTextAreaElement>(null);
  const wrap = useRef<HTMLDivElement>(null);
  const list = useRef<HTMLUListElement>(null);
  const caretRef = useRef(0);
  const [items, setItems] = useState<string[]>([]);
  const [active, setActive] = useState(0);
  const chips = bareNames(source);
  const marks = highlightSegments(source, chips);

  const refresh = (value: string, caret: number) => {
    caretRef.current = caret;
    const token = tokenAt(value, caret);
    setItems(suggestions(token.prefix, bagNames, catalogKeys));
    setActive(0);
    onFocus(identifierAt(value, caret));
  };

  useLayoutEffect(() => {
    const menu = list.current;
    const field = area.current;
    if (!menu || !field || items.length === 0) return;
    placeSuggest(menu, field, caretRef.current);
    const item = menu.children[active];
    if (item instanceof HTMLElement) {
      const top = item.offsetTop;
      const bottom = top + item.offsetHeight;
      if (top < menu.scrollTop) menu.scrollTop = top;
      else if (bottom > menu.scrollTop + menu.clientHeight) menu.scrollTop = bottom - menu.clientHeight;
    }
  }, [items, active, source]);

  useEffect(() => {
    const box = wrap.current;
    const onView = () => {
      const menu = list.current;
      const field = area.current;
      if (!menu || !field || items.length === 0) return;
      placeSuggest(menu, field, caretRef.current);
    };
    box?.addEventListener("scroll", onView, {passive: true});
    window.addEventListener("scroll", onView, true);
    window.addEventListener("resize", onView);
    return () => {
      box?.removeEventListener("scroll", onView);
      window.removeEventListener("scroll", onView, true);
      window.removeEventListener("resize", onView);
    };
  }, [items.length]);

  const insertAt = (token: string) => {
    const el = area.current;
    const caret = el?.selectionStart ?? source.length;
    const bounds = tokenAt(source, caret);
    const text = completion(source, bounds.start, token);
    onChange(insert(source, bounds.start, bounds.end, text));
    setItems([]);
    window.requestAnimationFrame(() => {
      el?.focus();
      const position = bounds.start + text.length;
      el?.setSelectionRange(position, position);
    });
  };

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (items.length === 0) return;
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActive((index) => Math.min(index + 1, items.length - 1));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setActive((index) => Math.max(index - 1, 0));
    } else if (event.key === "Enter" || event.key === "Tab") {
      event.preventDefault();
      insertAt(items[active] ?? items[0]!);
    } else if (event.key === "Escape") {
      event.preventDefault();
      setItems([]);
    }
  };

  return (
    <div className="algorithm-expression" style={{display: "flex", flexDirection: "column", minWidth: 0}}>
      <LyoPaper ref={wrap} variant="outlined" sx={{maxHeight: "10rem", overflow: "auto", bgcolor: "background.paper"}}>
        <div style={{display: "grid", gridTemplateColumns: "1fr", minHeight: "4.5rem"}}>
          <pre aria-hidden="true" style={{...formulaText, pointerEvents: "none", userSelect: "none"}}>
            {marks.map((segment, index) => segment.mark ? <mark key={index} style={{background: "rgba(25,118,210,0.28)", color: "inherit"}}>{segment.text}</mark> : <span key={index}>{segment.text}</span>)}
            <span> </span>
          </pre>
          <textarea
            ref={area}
            value={source}
            spellCheck={false}
            role="combobox"
            aria-autocomplete="list"
            aria-expanded={items.length > 0}
            style={{...formulaText, gridArea: "1 / 1", background: "transparent", color: "transparent", caretColor: "inherit", resize: "none", outline: "none", overflow: "hidden", width: "100%"}}
            onChange={(event) => {
              onChange(event.target.value);
              refresh(event.target.value, event.target.selectionStart ?? 0);
            }}
            onClick={(event) => refresh(source, event.currentTarget.selectionStart ?? 0)}
            onKeyUp={(event) => {
              if (event.key === "ArrowDown" || event.key === "ArrowUp" || event.key === "Enter" || event.key === "Tab" || event.key === "Escape") return;
              refresh(source, event.currentTarget.selectionStart ?? 0);
            }}
            onKeyDown={onKeyDown}
            onBlur={() => window.setTimeout(() => setItems([]), 150)}
          />
        </div>
      </LyoPaper>
      {items.length > 0 ? (
        <LyoPaper component="ul" ref={list} role="listbox" elevation={8} sx={{position: "fixed", top: -9999, left: 0, zIndex: 1400, listStyle: "none", m: 0, py: 0.5, px: 0, minWidth: "12rem", maxHeight: "12rem", overflow: "auto", bgcolor: "background.paper"}}>
          {items.map((item, index) => (
            <li
              key={`${item}-${index}`}
              role="option"
              aria-selected={index === active}
              style={{padding: "4px 12px", cursor: "pointer", fontSize: "0.8125rem", background: index === active ? "rgba(25,118,210,0.16)" : undefined}}
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => insertAt(item)}
            >
              <code>{item}</code>
            </li>
          ))}
        </LyoPaper>
      ) : null}
      <div style={{display: "flex", flexWrap: "wrap", gap: 6, marginTop: 8}}>
        {chips.map((name) => (
          <button key={name} type="button" onClick={() => { onFocus(name); onChip(name); }} style={{border: 0, borderRadius: 999, padding: "2px 10px", fontSize: "0.75rem", cursor: "pointer", color: "inherit", background: "rgba(25,118,210,0.28)", outline: name === focused ? "2px solid #1976d2" : undefined}}>
            {name}
          </button>
        ))}
      </div>
    </div>
  );
}

const formulaText: CSSProperties = {
  gridArea: "1 / 1", boxSizing: "border-box", margin: 0, padding: "8px 12px", border: 0,
  fontFamily: "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace", fontSize: "0.875rem", lineHeight: 1.5,
  whiteSpace: "pre-wrap", overflowWrap: "break-word", tabSize: 4, minHeight: "100%",
};

function highlightSegments(source: string, names: readonly string[]): Array<{text: string; mark: boolean}> {
  const marked = new Set(names);
  const parts: Array<{text: string; mark: boolean}> = [];
  const pattern = /"(?:\\.|[^"\\])*"|[A-Za-z_][A-Za-z0-9_]*/g;
  let last = 0;
  for (const match of source.matchAll(pattern)) {
    const index = match.index ?? 0;
    if (index > last) parts.push({text: source.slice(last, index), mark: false});
    const token = match[0];
    parts.push({text: token, mark: !token.startsWith("\"") && marked.has(token)});
    last = index + token.length;
  }
  if (last < source.length) parts.push({text: source.slice(last), mark: false});
  return parts;
}

function tokenAt(source: string, caret: number): {start: number; end: number; prefix: string} {
  const index = Math.min(Math.max(caret, 0), source.length);
  let start = index;
  while (start > 0 && /[A-Za-z0-9_]/.test(source[start - 1]!)) start--;
  let end = index;
  while (end < source.length && /[A-Za-z0-9_]/.test(source[end]!)) end++;
  return {start, end, prefix: source.slice(start, index)};
}

function completion(source: string, start: number, suggestion: string): string {
  if (suggestion.startsWith(".") && start > 0 && source[start - 1] === ".") return suggestion.slice(1);
  const dot = suggestion.lastIndexOf(".");
  if (dot > 0) {
    const head = suggestion.slice(0, dot + 1);
    if (source.slice(start - head.length, start) === head) return suggestion.slice(dot + 1);
  }
  return suggestion;
}

function MapEditor({draft, onChange}: {draft: AlgorithmDraft; onChange: (draft: AlgorithmDraft) => void}) {
  const showMissing = !isNumericAlias(draft.result) && draft.missing === "Zero";
  return (
    <LyoStack spacing={1}>
      <LyoTextField size="small" label="Input identifier" value={draft.input} onChange={(event) => onChange({...draft, input: event.target.value})} />
      <LyoTextField select size="small" label="Missing" value={draft.missing} onChange={(event) => onChange({...draft, missing: event.target.value as AlgorithmDraft["missing"]})}>
        <LyoMenuItem value="Zero">Zero</LyoMenuItem>
        <LyoMenuItem value="Reject">Reject</LyoMenuItem>
      </LyoTextField>
      {showMissing ? <LyoTextField size="small" label="Missing value" value={draft.missingValue ?? ""} onChange={(event) => onChange({...draft, missingValue: event.target.value})} /> : null}
      {draft.entries.map((entry, index) => (
        <LyoStack key={index} direction="row" spacing={1}>
          <LyoTextField size="small" label="Key" value={entry.key} onChange={(event) => patchRow(draft.entries, index, {key: event.target.value}, (entries) => onChange({...draft, entries}))} />
          <LyoTextField size="small" label="Value" value={entry.value} onChange={(event) => patchRow(draft.entries, index, {value: event.target.value}, (entries) => onChange({...draft, entries}))} />
        </LyoStack>
      ))}
      <LyoButton onClick={() => onChange({...draft, entries: [...draft.entries, {key: "", value: ""}]})}>Add entry</LyoButton>
    </LyoStack>
  );
}

function BandEditor({draft, onChange}: {draft: AlgorithmDraft; onChange: (draft: AlgorithmDraft) => void}) {
  const showMissing = !isNumericAlias(draft.result) && draft.outOfRange === "Zero";
  return (
    <LyoStack spacing={1}>
      <LyoTextField size="small" label="Input identifier" value={draft.input} onChange={(event) => onChange({...draft, input: event.target.value})} />
      <LyoTextField select size="small" label="Out of range" value={draft.outOfRange} onChange={(event) => onChange({...draft, outOfRange: event.target.value as AlgorithmDraft["outOfRange"]})}>
        <LyoMenuItem value="Zero">Zero</LyoMenuItem>
        <LyoMenuItem value="Nearest">Nearest</LyoMenuItem>
        <LyoMenuItem value="Reject">Reject</LyoMenuItem>
      </LyoTextField>
      {showMissing ? <LyoTextField size="small" label="Missing value" value={draft.missingValue ?? ""} onChange={(event) => onChange({...draft, missingValue: event.target.value})} /> : null}
      {draft.bands.map((band, index) => (
        <LyoStack key={index} direction="row" spacing={1}>
          <LyoTextField size="small" label="Min" value={band.min} onChange={(event) => patchRow(draft.bands, index, {min: event.target.value}, (bands) => onChange({...draft, bands}))} />
          <LyoTextField size="small" label="Max" value={band.max} onChange={(event) => patchRow(draft.bands, index, {max: event.target.value}, (bands) => onChange({...draft, bands}))} />
          <LyoTextField size="small" label="Value" value={band.value} onChange={(event) => patchRow(draft.bands, index, {value: event.target.value}, (bands) => onChange({...draft, bands}))} />
        </LyoStack>
      ))}
      <LyoButton onClick={() => onChange({...draft, bands: [...draft.bands, {min: "0", max: "1", value: ""}]})}>Add band</LyoButton>
    </LyoStack>
  );
}

function LinearEditor({draft, onChange}: {draft: AlgorithmDraft; onChange: (draft: AlgorithmDraft) => void}) {
  return (
    <LyoStack spacing={1}>
      <LyoTextField size="small" label="Input identifier" value={draft.input} onChange={(event) => onChange({...draft, input: event.target.value})} />
      <LyoSwitch checked={draft.clamp} onChange={(_, checked) => onChange({...draft, clamp: checked})} />
      <LyoTextField size="small" label="Input min" value={draft.inputMin} onChange={(event) => onChange({...draft, inputMin: event.target.value})} />
      <LyoTextField size="small" label="Input max" value={draft.inputMax} onChange={(event) => onChange({...draft, inputMax: event.target.value})} />
      <LyoTextField size="small" label="Result at min" value={draft.pointsMin} onChange={(event) => onChange({...draft, pointsMin: event.target.value})} />
      <LyoTextField size="small" label="Result at max" value={draft.pointsMax} onChange={(event) => onChange({...draft, pointsMax: event.target.value})} />
    </LyoStack>
  );
}

function CompositeEditor({draft, onChange}: {draft: AlgorithmDraft; onChange: (draft: AlgorithmDraft) => void}) {
  return (
    <LyoStack spacing={1}>
      <LyoTextField select size="small" label="Combiner" value={draft.combiner} onChange={(event) => onChange({...draft, combiner: event.target.value as AlgorithmDraft["combiner"]})}>
        <LyoMenuItem value="Sum">Sum</LyoMenuItem>
        <LyoMenuItem value="WeightedSum">WeightedSum</LyoMenuItem>
        <LyoMenuItem value="Formula">Formula</LyoMenuItem>
      </LyoTextField>
      {draft.combiner === "Formula" ? <LyoTextField size="small" label="Formula" value={draft.formula ?? ""} onChange={(event) => onChange({...draft, formula: event.target.value})} /> : null}
      {draft.children.map((child, index) => (
        <LyoPaper key={index} variant="outlined" sx={{p: 1}}>
          <LyoStack spacing={1}>
            <LyoTextField size="small" label="Key" value={child.key ?? ""} onChange={(event) => patchRow(draft.children, index, {key: event.target.value}, (children) => onChange({...draft, children}))} />
            <LyoTextField size="small" label="Catalog ref" value={child.ref ?? ""} onChange={(event) => patchRow(draft.children, index, {ref: event.target.value}, (children) => onChange({...draft, children}))} />
            {draft.combiner === "WeightedSum" ? <LyoTextField size="small" label="Weight" value={child.weight} onChange={(event) => patchRow(draft.children, index, {weight: event.target.value}, (children) => onChange({...draft, children}))} /> : null}
            {!child.ref?.trim() ? (
              <LyoTextField select size="small" label="Inline kind" value={child.kind ?? ""} onChange={(event) => {
                const kind = event.target.value as AlgorithmDraft["kind"];
                patchRow(draft.children, index, {kind, ref: undefined, inline: {...emptyDraft(), kind}}, (children) => onChange({...draft, children}));
              }}>
                <LyoMenuItem value="discreteMap">Discrete map</LyoMenuItem>
                <LyoMenuItem value="rangeBand">Range band</LyoMenuItem>
                <LyoMenuItem value="linear">Linear</LyoMenuItem>
              </LyoTextField>
            ) : null}
          </LyoStack>
        </LyoPaper>
      ))}
      <LyoButton onClick={() => onChange({...draft, children: [...draft.children, {ref: "catalog.key", weight: "1"}]})}>Add catalog ref</LyoButton>
      <LyoButton onClick={() => onChange({...draft, children: [...draft.children, {key: "child", kind: "discreteMap", weight: "1", inline: {...emptyDraft(), kind: "discreteMap"}}]})}>Add inline scorer</LyoButton>
    </LyoStack>
  );
}

function Breakdown({steps}: {steps: AlgorithmPreview["steps"]}) {
  if (steps.length === 0) return <span>Run a preview to see the breakdown.</span>;
  return (
    <LyoStack spacing={0}>
      {flatten(steps, 0).map(({step, depth}, index) => (
        <span key={`${step.key}-${index}`} style={{paddingLeft: depth * 16}}>
          {step.key} {step.kind} {step.input ?? ""} points {step.points} {step.value == null ? "" : JSON.stringify(step.value)}
        </span>
      ))}
    </LyoStack>
  );
}

function flatten(steps: AlgorithmPreview["steps"], depth: number): Array<{step: AlgorithmPreview["steps"][number]; depth: number}> {
  const rows: Array<{step: AlgorithmPreview["steps"][number]; depth: number}> = [];
  for (const step of steps) {
    rows.push({step, depth});
    rows.push(...flatten(step.children ?? [], depth + 1));
  }
  return rows;
}

function patchRow<T>(rows: T[], index: number, patch: Partial<T>, onChange: (rows: T[]) => void) {
  onChange(rows.map((row, item) => (item === index ? {...row, ...patch} : row)));
}

function isKey(key: string): boolean {
  return /^[a-z][a-z0-9_.]*$/.test(key);
}

function messageOf(reason: unknown): string {
  if (reason instanceof ApiClientError) {
    const details = reason.details;
    if (typeof details === "string" && details.trim()) return details;
    if (details && typeof details === "object") {
      const record = details as {detail?: unknown; message?: unknown};
      if (typeof record.detail === "string" && record.detail.trim()) return record.detail;
      if (typeof record.message === "string" && record.message.trim()) return record.message;
    }
  }
  return reason instanceof Error ? reason.message : "Request failed.";
}
