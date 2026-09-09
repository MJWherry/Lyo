#!/usr/bin/env python3
"""HTML emitter for resolved document IR.

This file is named ``html.py`` so ``sys.path`` + ``from html import build`` works.
That shadows the stdlib ``html`` module, so this file re-exports ``escape`` /
``unescape`` (pygments and pytest import ``html`` during bootstrap). Do **not**
``import html`` here — it would be recursive.
"""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Any

_DOCS = Path(__file__).resolve().parent
_SCRIPTS = _DOCS.parent
for _p in (str(_SCRIPTS), str(_DOCS)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

_ESCAPE_TABLE = str.maketrans({"&": "&amp;", "<": "&lt;", ">": "&gt;"})
_ESCAPE_QUOTES = str.maketrans({"&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;"})


def escape(s: object, quote: bool = True) -> str:
    """Stdlib-compatible HTML escape (used when this file shadows ``html``)."""

    text = "" if s is None else str(s)
    return text.translate(_ESCAPE_QUOTES if quote else _ESCAPE_TABLE)


def unescape(s: str) -> str:
    """Minimal entity unescape (amp last)."""

    return (
        (s or "")
        .replace("&quot;", '"')
        .replace("&#x27;", "'")
        .replace("&#39;", "'")
        .replace("&lt;", "<")
        .replace("&gt;", ">")
        .replace("&amp;", "&")
    )


def _attr(name: str, value: str | None) -> str:
    if not value:
        return ""
    return f' {name}="{escape(value, quote=True)}"'


def _doc():
    import document as mod

    return mod


def _render_cell(cell) -> str:
    attrs = ""
    attrs += _attr("class", cell.css_class)
    attrs += _attr("style", cell.style_css or None)
    if cell.colspan != 1:
        attrs += f' colspan="{cell.colspan}"'
    if cell.rowspan != 1:
        attrs += f' rowspan="{cell.rowspan}"'
    return f"<{cell.tag}{attrs}>{cell.html}</{cell.tag}>"


def _render_rows(rows) -> list[str]:
    lines: list[str] = []
    for row in rows:
        attrs = _attr("class", row.css_class) + _attr("style", row.style_css or None)
        lines.append(f"    <tr{attrs}>")
        for cell in row.cells:
            lines.append("      " + _render_cell(cell))
        lines.append("    </tr>")
    return lines


def table_to_html(table, *, include_title: bool = False) -> str:
    """Emit a ``<table>`` fragment (optional title, ``<style>`` from ``base_css``)."""

    parts: list[str] = []
    if table.base_css:
        parts.append(f"<style>{table.base_css}</style>")
    if include_title and table.title:
        parts.append(f"<h1>{table.title}</h1>")
    attrs = _attr("id", table.table_id)
    attrs += _attr("class", table.css_class)
    attrs += _attr("style", table.table_style_css or None)
    out: list[str] = [f"<table{attrs}>"]
    if table.caption:
        out.append(f"  <caption>{table.caption}</caption>")
    if table.thead:
        out.append("  <thead>")
        out.extend(_render_rows(table.thead))
        out.append("  </thead>")
    if table.tbody:
        out.append("  <tbody>")
        out.extend(_render_rows(table.tbody))
        out.append("  </tbody>")
    if table.tfoot:
        out.append("  <tfoot>")
        out.extend(_render_rows(table.tfoot))
        out.append("  </tfoot>")
    out.append("</table>")
    parts.append("\n".join(out))
    return "\n".join(parts)


def _list_item_html(item: Any, *, ordered: bool = False) -> str:
    if isinstance(item, str):
        return f"<li>{item}</li>"
    title = (item.get("title") or "").strip()
    text = (item.get("text") or "").strip()
    children = item.get("items") or []
    if title and text:
        label = f"<strong>{title}.</strong> {text}"
    else:
        label = text or title
    inner = [label] if label else []
    if children:
        tag = "ol" if ordered else "ul"
        nested = "".join(_list_item_html(c) for c in children)
        inner.append(f"<{tag}>{nested}</{tag}>")
    return f"<li>{''.join(inner)}</li>"


def _blocks_to_html(blocks: list[dict[str, Any]]) -> list[str]:
    parts: list[str] = []
    for block in blocks:
        btype = block.get("type")
        css = block.get("css_class")
        cls = _attr("class", css)
        if btype == "heading":
            level = max(1, min(6, int(block.get("level") or 1)))
            parts.append(f"<h{level}{cls}>{block.get('text') or ''}</h{level}>")
        elif btype == "paragraph":
            parts.append(f"<p{cls}>{block.get('text') or ''}</p>")
        elif btype == "list":
            tag = "ol" if block.get("ordered") else "ul"
            items = "".join(
                _list_item_html(item, ordered=bool(block.get("ordered")))
                for item in (block.get("items") or [])
            )
            parts.append(f"<{tag}{cls}>{items}</{tag}>")
        elif btype == "code":
            lang = block.get("language") or "text"
            code = escape(block.get("code") or "", quote=False)
            lang_attr = _attr("class", f"language-{lang}")
            parts.append(f"<pre{cls}><code{lang_attr}>{code}</code></pre>")
        elif btype == "hr":
            parts.append(f"<hr{cls}>")
        elif btype == "html":
            body = block.get("body") or ""
            if css:
                parts.append(f"<div{cls}>{body}</div>")
            else:
                parts.append(body)
        elif btype == "section":
            inner = _blocks_to_html(block.get("blocks") or [])
            title = block.get("title")
            body = ""
            if title:
                body += f"<h2>{title}</h2>\n"
            body += "\n".join(inner)
            parts.append(f"<section{cls}>{body}</section>")
        elif btype == "table":
            table = block.get("table")
            if table is not None:
                parts.append(table_to_html(table, include_title=True))
        else:
            continue
    return parts


def document_to_html(resolved) -> str:
    """Emit an HTML fragment from a resolved document."""

    parts: list[str] = []
    if resolved.name:
        parts.append(f"<h1>{resolved.name}</h1>")
    if resolved.tagline:
        parts.append(f"<p>{resolved.tagline}</p>")
    if resolved.description:
        parts.append(f"<p>{resolved.description}</p>")
    parts.extend(_blocks_to_html(resolved.blocks))
    return "\n".join(parts)


def build(
    config: Any,
    data: Any,
    *,
    format: str = "html",
    strict: bool | None = None,
) -> str:
    """Resolve config + data, then emit HTML or Markdown."""

    resolved = _doc().resolve(config, data, strict=strict)
    fmt = (format or "html").lower()
    if fmt in ("md", "markdown"):
        from markdown import resolved_to_md

        return resolved_to_md(resolved)
    return document_to_html(resolved)


def build_table(
    config: Any,
    data: Any,
    *,
    strict: bool | None = None,
    format: str = "html",
) -> str:
    """Resolve a table (or document) config and emit HTML or Markdown."""

    return build(config, data, format=format, strict=strict)


__all__ = [
    "build",
    "build_table",
    "document_to_html",
    "table_to_html",
    "escape",
    "unescape",
]
