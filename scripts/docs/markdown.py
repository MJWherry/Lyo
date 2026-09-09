"""JSON section document → Markdown.

Shared emit path for package READMEs (via project-docs.py) and non-package
reports (CI pipeline summary). Parse helpers stay in project-docs.py.
"""

from __future__ import annotations

import re


def escape_cell(value: str) -> str:
    """Flatten a table cell for a GFM pipe table."""
    return (value or "").replace("\r\n", "\n").replace("\n", " ").replace("|", "\\|").strip()


def list_item_to_md(item, indent: int = 0, ordered: bool = False, index: int = 1) -> list[str]:
    """Emit a list section item (string or nested object) as markdown lines."""
    pad = "  " * indent
    prefix = f"{index}. " if ordered and indent == 0 else "- "
    if isinstance(item, str):
        return [f"{pad}{prefix}{item}"]

    title = (item.get("title") or "").strip()
    text = (item.get("text") or "").strip()
    children = item.get("items") or []
    if title and text:
        label = f"**{title}.** {text}"
    else:
        label = text or title

    lines = [f"{pad}{prefix}{label}"] if label else []
    child_indent = indent + (1 if label else 0)
    for child in children:
        lines.extend(list_item_to_md(child, child_indent, ordered=False))
    return lines


def table_to_markdown(headers: list[str], rows: list[list[str]]) -> str:
    headers = [escape_cell(h) for h in headers]
    rows = [[escape_cell(c) for c in row] for row in rows]
    widths = [len(h) for h in headers]
    for row in rows:
        for i, cell in enumerate(row):
            if i < len(widths):
                widths[i] = max(widths[i], len(cell))

    def fmt(cells: list[str]) -> str:
        parts = []
        for i, w in enumerate(widths):
            cell = cells[i] if i < len(cells) else ""
            parts.append(cell.ljust(w))
        return "| " + " | ".join(parts) + " |"

    sep = "| " + " | ".join("-" * max(3, w) for w in widths) + " |"
    return "\n".join([fmt(headers), sep, *[fmt(r) for r in rows]])


def _escape_summary(text: str) -> str:
    return (text or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def _unescape_md(text: str) -> str:
    return (
        (text or "")
        .replace("&quot;", '"')
        .replace("&lt;", "<")
        .replace("&gt;", ">")
        .replace("&amp;", "&")
    )


def section_to_md(section: dict, level: int = 2) -> str:
    h = "#" * level
    parts: list[str] = []
    title = section.get("title")
    t = section.get("type")
    if title and t not in ("details", "section", "heading"):
        parts.append(f"{h} {title}")
        parts.append("")
    if t == "paragraph":
        parts.append(_unescape_md(section.get("text") or ""))
        parts.append("")
    elif t == "list":
        ordered = bool(section.get("ordered"))
        for idx, item in enumerate(section.get("items") or []):
            parts.extend(list_item_to_md(item, indent=0, ordered=ordered, index=idx + 1))
        parts.append("")
    elif t == "code":
        parts.append(f"```{section.get('language') or 'text'}")
        parts.append(section.get("code") or "")
        parts.append("```")
        parts.append("")
    elif t == "table":
        if section.get("lead"):
            parts.append(section["lead"])
            parts.append("")
        parts.append(table_to_markdown(section.get("headers") or [], section.get("rows") or []))
        parts.append("")
        if section.get("trail"):
            parts.append(section["trail"])
            parts.append("")
    elif t == "markdown":
        parts.append(section.get("body") or "")
        parts.append("")
    elif t == "heading":
        heading_level = max(1, min(6, int(section.get("level") or level)))
        parts.append(f"{'#' * heading_level} {_unescape_md(section.get('text') or '')}")
        parts.append("")
    elif t == "hr":
        parts.append("---")
        parts.append("")
    elif t == "section":
        if title:
            parts.append(f"{h} {_unescape_md(title)}")
            parts.append("")
        for nested in section.get("blocks") or []:
            parts.append(section_to_md(nested, level + 1).rstrip())
            parts.append("")
    elif t == "details":
        open_attr = " open" if section.get("open") else ""
        parts.append(f"<details{open_attr}>")
        parts.append(f"<summary>{_escape_summary(title or 'Details')}</summary>")
        parts.append("")
        ordered = bool(section.get("ordered"))
        for idx, item in enumerate(section.get("items") or []):
            parts.extend(list_item_to_md(item, indent=0, ordered=ordered, index=idx + 1))
        body = (section.get("body") or "").strip()
        if body:
            if section.get("items"):
                parts.append("")
            parts.append(body)
        parts.append("")
        parts.append("</details>")
        parts.append("")
    return "\n".join(parts)


def _cell_md(cell) -> str:
    text = _unescape_md(getattr(cell, "text", None) or getattr(cell, "html", "") or "")
    href = getattr(cell, "href", None)
    if href:
        return f"[{text}]({_unescape_md(href)})"
    return text


def _pad_row(cells: list[str], width: int) -> list[str]:
    row = list(cells)
    while len(row) < width:
        row.append("")
    return row[:width]


def _rows_to_grid(rows) -> list[list[str]]:
    """Expand rowspan/colspan occupancy into a rectangular GFM grid.

    Continuation cells of a vertical span are empty (readable grouping without HTML).
    """

    grid: list[list[str]] = []
    blocked: list[int] = []
    max_cols = 0
    for row in rows or []:
        occ = list(blocked)
        line: list[str] = []

        def ensure(n: int) -> None:
            while len(occ) < n:
                occ.append(0)
            while len(line) < n:
                line.append("")

        cells = list(getattr(row, "cells", None) or [])
        ci = 0
        col = 0
        while True:
            blocked_here = col < len(occ) and occ[col] > 0
            if not blocked_here and ci >= len(cells):
                break
            ensure(col + 1)
            if col < len(occ) and occ[col] > 0:
                line[col] = ""
                col += 1
                continue
            cell = cells[ci]
            ci += 1
            cs = max(1, int(getattr(cell, "colspan", 1) or 1))
            rs = max(1, int(getattr(cell, "rowspan", 1) or 1))
            ensure(col + cs)
            line[col] = _cell_md(cell)
            for extra in range(1, cs):
                line[col + extra] = ""
            while len(occ) < col + cs:
                occ.append(0)
            for i in range(cs):
                occ[col + i] = max(occ[col + i], rs)
            col += cs
        blocked = [max(0, n - 1) for n in occ]
        grid.append(line)
        max_cols = max(max_cols, len(line))
    return [_pad_row(line, max_cols) for line in grid]


def flatten_rendered_table(table) -> tuple[list[str], list[list[str]], str | None]:
    """Headers + body rows for GFM. Multi-row headers collapse to the last header row."""

    caption = getattr(table, "caption", None) or None
    thead = list(getattr(table, "thead", None) or [])
    tbody = list(getattr(table, "tbody", None) or [])
    tfoot = list(getattr(table, "tfoot", None) or [])
    header_grid = _rows_to_grid(thead)
    body_grid = _rows_to_grid(tbody + tfoot)
    width = 0
    if header_grid:
        width = max(width, len(header_grid[0]))
    if body_grid:
        width = max(width, len(body_grid[0]))
    if not width:
        return [], [], caption
    if header_grid:
        headers = _pad_row(header_grid[-1], width)
    else:
        headers = [""] * width
    rows = [_pad_row(r, width) for r in body_grid]
    return headers, rows, caption


def resolved_to_md(doc) -> str:
    """Emit GFM from a resolved document (or a dict with the same shape)."""

    if isinstance(doc, dict):
        name = (doc.get("name") or doc.get("id") or "") or ""
        tagline = doc.get("tagline") or ""
        description = doc.get("description") or ""
        blocks = doc.get("blocks") or doc.get("sections") or []
    else:
        name = getattr(doc, "name", None) or ""
        tagline = getattr(doc, "tagline", None) or ""
        description = getattr(doc, "description", None) or ""
        blocks = getattr(doc, "blocks", None) or []

    lines: list[str] = []
    name = _unescape_md(str(name)).strip()
    if name:
        lines += [f"# {name}", ""]
    tagline = _unescape_md(str(tagline)).strip()
    if tagline:
        lines += [tagline, ""]
    description = _unescape_md(str(description)).strip()
    if description:
        lines += [description, ""]
    lines.append(_blocks_to_md(blocks, 2).rstrip())
    text = "\n".join(lines)
    text = re.sub(r"\n{3,}", "\n\n", text).strip() + "\n"
    return text


def _blocks_to_md(blocks, level: int = 2) -> str:
    parts: list[str] = []
    for block in blocks or []:
        btype = block.get("type") if isinstance(block, dict) else getattr(block, "type", None)
        if btype == "table":
            table = block.get("table") if isinstance(block, dict) else getattr(block, "table", None)
            headers, rows, caption = flatten_rendered_table(table)
            section = {"type": "table", "headers": headers, "rows": rows}
            if caption:
                section["lead"] = _unescape_md(caption)
            parts.append(section_to_md(section, level).rstrip())
            parts.append("")
        elif btype == "paragraph":
            text = block.get("text") if isinstance(block, dict) else ""
            parts.append(section_to_md({"type": "paragraph", "text": _unescape_md(text)}, level).rstrip())
            parts.append("")
        elif btype == "list":
            items = block.get("items") if isinstance(block, dict) else []
            ordered = bool(block.get("ordered")) if isinstance(block, dict) else False
            parts.append(section_to_md({"type": "list", "items": items, "ordered": ordered}, level).rstrip())
            parts.append("")
        elif btype == "code":
            parts.append(
                section_to_md(
                    {
                        "type": "code",
                        "language": (block.get("language") if isinstance(block, dict) else None) or "text",
                        "code": (block.get("code") if isinstance(block, dict) else "") or "",
                    },
                    level,
                ).rstrip()
            )
            parts.append("")
        elif btype == "heading":
            parts.append(section_to_md(block if isinstance(block, dict) else {"type": "heading"}, level).rstrip())
            parts.append("")
        elif btype == "hr":
            parts.append("---")
            parts.append("")
        elif btype == "html":
            body = _unescape_md((block.get("body") if isinstance(block, dict) else "") or "")
            parts.append(section_to_md({"type": "markdown", "body": body}, level).rstrip())
            parts.append("")
        elif btype == "section":
            parts.append(section_to_md(block if isinstance(block, dict) else {"type": "section"}, level).rstrip())
            parts.append("")
        elif btype == "markdown":
            parts.append(section_to_md(block if isinstance(block, dict) else {"type": "markdown"}, level).rstrip())
            parts.append("")
    return "\n".join(parts)


def document_to_md(doc: dict) -> str:
    """Render a non-package document: name, tagline, description, sections."""
    name = (doc.get("name") or doc.get("id") or "").strip()
    lines: list[str] = []
    if name:
        lines += [f"# {name}", ""]
    tagline = (doc.get("tagline") or "").strip()
    if tagline:
        lines += [tagline, ""]
    desc = (doc.get("description") or "").strip()
    if desc:
        lines += [desc, ""]
    for section in doc.get("sections") or []:
        lines.append(section_to_md(section, 2).rstrip())
        lines.append("")
    text = "\n".join(lines)
    text = re.sub(r"\n{3,}", "\n\n", text).strip() + "\n"
    return text
