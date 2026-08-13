"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { WhereClauseBuilder, defaultGroup, type WhereClause } from "lyo-query-components";
import {
  CHAPTER_WHERE_PRESETS,
  SERIES_WHERE_PRESETS,
  VOLUME_WHERE_PRESETS,
  type ComicQueryScope,
} from "lyo-comic-api-client";
import type { ComicCardRow } from "lyo-comic-api-client";
import { SeriesCard } from "./SeriesCard";
import { bffFetch } from "@/lib/api/bffFetch";

const TYPES = ["manga", "manhwa", "manhua", "webtoon", "western"];
const STATUSES = ["ongoing", "completed", "hiatus", "cancelled"];
const PAGE_SIZE = 24;

type SearchResponse = {
  isSuccess?: boolean;
  items?: ComicCardRow[];
  total?: number | null;
  error?: string;
};

export function SearchForm({
  initialQ,
  tags,
}: {
  initialQ: string;
  tags: string[];
}) {
  const router = useRouter();
  const params = useSearchParams();
  const [scope, setScope] = useState<ComicQueryScope>((params.get("scope") as ComicQueryScope) || "series");
  const [title, setTitle] = useState(initialQ);
  const [type, setType] = useState(params.get("type") ?? "");
  const [status, setStatus] = useState(params.get("status") ?? "");
  const [language, setLanguage] = useState(params.get("lang") ?? "");
  const [author, setAuthor] = useState(params.get("author") ?? "");
  const [year, setYear] = useState(params.get("year") ?? "");
  const [selectedTags, setSelectedTags] = useState<string[]>(
    (params.get("tags") ?? "").split(",").map((t) => t.trim()).filter(Boolean)
  );
  const [layout, setLayout] = useState<"grid" | "list">("grid");
  const [where, setWhere] = useState<WhereClause>(defaultGroup("Title"));
  const [advancedOn, setAdvancedOn] = useState(false);
  const [items, setItems] = useState<ComicCardRow[] | null>(null);
  const [total, setTotal] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [page, setPage] = useState(() => Math.max(1, Number(params.get("page") ?? 1) || 1));

  useEffect(() => {
    void run();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const presets = useMemo(() => {
    if (scope === "volumes") return [...VOLUME_WHERE_PRESETS];
    if (scope === "chapters") return [...CHAPTER_WHERE_PRESETS];
    return [...SERIES_WHERE_PRESETS];
  }, [scope]);

  async function run(e?: FormEvent, nextPage?: number) {
    e?.preventDefault();
    const targetPage = nextPage ?? (e ? 1 : page);
    setPending(true);
    setError(null);
    const qs = new URLSearchParams();
    if (title) qs.set("q", title);
    if (scope !== "series") qs.set("scope", scope);
    if (type) qs.set("type", type);
    if (status) qs.set("status", status);
    if (language) qs.set("lang", language);
    if (author) qs.set("author", author);
    if (year) qs.set("year", year);
    if (selectedTags.length) qs.set("tags", selectedTags.join(","));
    qs.set("page", String(targetPage));
    router.replace(`/search?${qs.toString()}`);
    setPage(targetPage);

    try {
      const res = await bffFetch("/api/search", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          scope,
          start: (targetPage - 1) * PAGE_SIZE,
          amount: PAGE_SIZE,
          simple: {
            title: title || undefined,
            type: type || undefined,
            status: status || undefined,
            language: language || undefined,
            author: author || undefined,
            year: year || undefined,
            tags: selectedTags,
          },
          whereClause: advancedOn ? where : null,
        }),
      });
      const data = (await res.json()) as SearchResponse;
      if (!res.ok) {
        setError(data.error ?? "Search failed");
        setItems([]);
      } else {
        setItems(data.items ?? []);
        setTotal(data.total ?? null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Search failed");
    } finally {
      setPending(false);
    }
  }

  function toggleTag(tag: string) {
    setSelectedTags((cur) => (cur.includes(tag) ? cur.filter((t) => t !== tag) : [...cur, tag]));
  }

  const pageCount = total != null ? Math.max(1, Math.ceil(total / PAGE_SIZE)) : 1;

  return (
    <div>
      <form className="search-toolbar" onSubmit={run}>
        <div className="field">
          <label htmlFor="q">Title</label>
          <input id="q" value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="scope">Scope</label>
          <select id="scope" value={scope} onChange={(e) => setScope(e.target.value as ComicQueryScope)}>
            <option value="series">Series</option>
            <option value="volumes">Volume</option>
            <option value="chapters">Chapter</option>
          </select>
        </div>
        <div className="field">
          <label htmlFor="type">Type</label>
          <select id="type" value={type} onChange={(e) => setType(e.target.value)}>
            <option value="">Any</option>
            {TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="status">Status</label>
          <select id="status" value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Any</option>
            {STATUSES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label htmlFor="lang">Language</label>
          <input id="lang" value={language} onChange={(e) => setLanguage(e.target.value)} placeholder="en, ja…" />
        </div>
        <div className="field">
          <label htmlFor="author">Author</label>
          <input id="author" value={author} onChange={(e) => setAuthor(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor="year">Year</label>
          <input id="year" value={year} onChange={(e) => setYear(e.target.value)} inputMode="numeric" />
        </div>
        <button className="btn" type="submit" disabled={pending}>
          {pending ? "Searching…" : "Search"}
        </button>
        <button className="btn btn--ghost" type="button" onClick={() => setLayout(layout === "grid" ? "list" : "grid")}>
          {layout === "grid" ? "List" : "Grid"}
        </button>
      </form>

      {tags.length > 0 ? (
        <div className="chip-row" style={{ marginBottom: "1rem" }}>
          {tags.map((tag) => (
            <button
              key={tag}
              type="button"
              className={selectedTags.includes(tag) ? "chip chip--on" : "chip"}
              onClick={() => toggleTag(tag)}
            >
              {tag}
            </button>
          ))}
        </div>
      ) : null}

      <details open={advancedOn} onToggle={(e) => setAdvancedOn((e.target as HTMLDetailsElement).open)}>
        <summary>Advanced where clause</summary>
        <div style={{ marginTop: "0.75rem" }}>
          <WhereClauseBuilder value={where} onChange={setWhere} fieldPresets={presets} defaultField="Title" />
        </div>
      </details>

      {error ? <p className="error">{error}</p> : null}
      {total != null ? <p className="muted">{total} result{total === 1 ? "" : "s"}</p> : null}

      <div className={layout === "grid" ? "card-grid" : "chapter-list"} style={{ marginTop: "1rem" }}>
        {(items ?? []).map((row) => (
          <SeriesCard key={String(row.id ?? cardKey(row))} row={row} layout={layout} />
        ))}
      </div>

      {pageCount > 1 ? (
        <nav className="pager" aria-label="Search results pages">
          <button
            type="button"
            className="btn btn--ghost"
            disabled={pending || page <= 1}
            onClick={() => void run(undefined, page - 1)}
          >
            Prev
          </button>
          <span className="pager__status">
            Page {page} of {pageCount}
          </span>
          <button
            type="button"
            className="btn btn--ghost"
            disabled={pending || page >= pageCount}
            onClick={() => void run(undefined, page + 1)}
          >
            Next
          </button>
        </nav>
      ) : null}
    </div>
  );
}

function cardKey(row: ComicCardRow): string {
  return `${row.id}-${row.slug}-${row.chapterNumber}-${row.volumeNumber}`;
}
