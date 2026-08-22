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


def section_to_md(section: dict, level: int = 2) -> str:
    h = "#" * level
    parts: list[str] = []
    title = section.get("title")
    t = section.get("type")
    if title and t != "details":
        parts.append(f"{h} {title}")
        parts.append("")
    if t == "paragraph":
        parts.append(section.get("text") or "")
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
