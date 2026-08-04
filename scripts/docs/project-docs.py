#!/usr/bin/env python3
"""
Per-project docs — docs.json is the ONLY source of truth.

  docs.json  (edit this)
      ↓ render
  README.md  (generated — never hand-edit as SoT)
  apps/portfolio/content/{packages.json,packages-full/}
  Lyo.Web.Components/wwwroot/catalog/

Commands:
  render      docs.json → README + portfolio + Blazor  (normal path)
  sync-deps   refresh dependencies[] on each docs.json from csproj/graph
  audit       print coverage stats
  extract     DANGEROUS legacy import: README → docs.json (lossy; overwrites SoT)
"""
from __future__ import annotations

import json
import re
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
_SCRIPTS = ROOT / "scripts"
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.deps import load_dependency_map  # noqa: E402
from lyo_tooling.dotnet import find_project_csproj, read_target_frameworks  # noqa: E402
LYO_NET = ROOT / "Lyo.Net"
PORTFOLIO = ROOT / "apps" / "portfolio" / "content"
PORTFOLIO_FULL = PORTFOLIO / "packages-full"
BLAZOR = ROOT / "Lyo.Net/Integration/Web/Lyo.Web.Components/wwwroot/catalog"
ROOT_README = ROOT / "README.md"
DOCS_FILENAME = "docs.json"

AREA_ORDER = [
    "Communication",
    "Core",
    "Data",
    "Features",
    "Integration",
    "Apps",
    "Security",
    "Tools",
    "Other",
]

SKIP_DIRS = {"bin", "obj", "node_modules", ".git", "TestResults"}

EXAMPLE_H2 = re.compile(
    r"^(quick\s*start|usage(\s+examples?)?|examples?|getting\s*started|"
    r"registration|setup|basic\s*usage|how\s*to|cookbook|samples?|"
    r"loading|extraction.*|editing.*|dependency\s*injection|"
    r"drop-and-play\s*registration|di(\s+extension.*)?"
    r"|configuration(\s*options)?|events|"
    r"rendering|rerun|retention.*|subscribe.*|watching.*)$",
    re.I,
)
# Also treat H2 as examples when the title is clearly a how-to / API demo.
EXAMPLE_H2_LOOSE = re.compile(
    r"\b(register|registration|setup|load|open|merge|apply|watch|subscribe|"
    r"configure|configuration|dependency\s*injection|\bdi\b|quick\s*start|"
    r"example|usage|getting\s*started)\b",
    re.I,
)
FEATURES_H2 = re.compile(r"^features$", re.I)
SKIP_H2 = re.compile(r"^(table\s*of\s*contents|toc|contents)$", re.I)

EXAMPLE_TITLE_ALIASES = {
    "Basic Usage": "Subscribe to events",
    "Advanced Configuration": "Configure options",
    "Dependency Injection Example": "Register with DI",
    "High-Performance Configuration": "High-performance options",
    "Watch for File Changes": "Handle file change events",
    "Monitor Directory Content Changes": "Handle directory change events",
    "Watch Subdirectories": "Watch subdirectories",
    "Drop-and-play registration": "Register jobs (Postgres)",
    "DI": "Register with DI",
    "Dependency injection": "Register with DI",
    "Loading": "Open a PDF",
    "Editing and merging (`IPdfWriter`)": "Edit and merge PDFs",
    "Editing and merging (IPdfWriter)": "Edit and merge PDFs",
    "Retention cleanup": "Run retention cleanup",
    "Registration": "Register services",
}

SUITE_BY_PACKAGE = {
    "Lyo.Compression": "compression",
    "Lyo.Encryption": "encryption",
    "Lyo.Cache": "cache",
    "Lyo.Hashing": "hashing",
    "Lyo.Lock": "lock",
    "Lyo.Csv": "csv",
    "Lyo.Xlsx": "xlsx",
    "Lyo.Api": "query",
    "Lyo.Query.Models": "query",
}


def strip_emoji(text: str) -> str:
    out = []
    for ch in text:
        o = ord(ch)
        if ch in "\uFE0E\uFE0F\u200D\u20E3":
            continue
        if 0x1F000 <= o <= 0x1FFFF or 0x2600 <= o <= 0x27BF:
            continue
        name = unicodedata.name(ch, "")
        if "EMOJI" in name or name.startswith("REGIONAL INDICATOR"):
            continue
        out.append(ch)
    s = "".join(out)
    # Keep list indentation; only collapse internal runs of spaces per line.
    s = re.sub(r"(?m)^(#{1,6})[ \t]+", r"\1 ", s)
    lines = []
    for line in s.split("\n"):
        m = re.match(r"^([ \t]*)(.*)$", line)
        lead, rest = m.group(1), m.group(2)
        rest = re.sub(r"[ \t]{2,}", " ", rest)
        lines.append(lead + rest)
    s = "\n".join(lines)
    s = re.sub(r"\n{3,}", "\n\n", s)
    return s.strip()


def collapse_ws(text: str) -> str:
    return re.sub(r"\s+", " ", text).strip()


def strip_md_links(text: str) -> str:
    return re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text).strip()


def extract_lead_title(text: str) -> str | None:
    """Pull a short title from a bold label immediately above a code fence."""
    lines = [l.strip() for l in text.strip().splitlines() if l.strip()]
    if not lines:
        return None
    last = lines[-1]
    m = re.fullmatch(r"\*\*([^*]+)\*\*:?", last)
    if not m:
        return None
    title = m.group(1).strip().rstrip(":")
    # Reject captions that are clearly not titles (too long / look like prose).
    if len(title) > 72 or title.count(" ") > 10:
        return None
    return title


def uniquify_title(title: str, existing: list[dict]) -> str:
    titles = {e.get("title") for e in existing}
    if title not in titles:
        return title
    n = 2
    while f"{title} ({n})" in titles:
        n += 1
    return f"{title} ({n})"


def make_tagline(description: str, limit: int = 200) -> str:
    """First paragraph, trimmed to a clean sentence (no mid-word cutoffs)."""
    if not description:
        return ""
    first = collapse_ws(strip_md_links(description.split("\n\n")[0]))
    if len(first) <= limit:
        return first
    window = first[: limit + 1]
    # Prefer ending on sentence punctuation.
    for sep in (". ", "! ", "? "):
        idx = window.rfind(sep)
        if idx >= 80:
            return window[: idx + 1].strip()
    # Else break on last space before limit.
    idx = window.rfind(" ")
    if idx >= 80:
        return window[:idx].rstrip(",;:") + "…"
    return first[:limit].rstrip() + "…"


def area_from_path(rel: str) -> str:
    parts = rel.replace("\\", "/").split("/")
    if parts and parts[0] == "Lyo.Net" and len(parts) > 1 and parts[1] in AREA_ORDER:
        return parts[1]
    return "Other"


def package_topic(package_id: str) -> str:
    """Family key for catalog grouping: ``Lyo.Email`` from ``Lyo.Email.Postgres``."""
    parts = package_id.split(".")
    if len(parts) < 2:
        return package_id
    return f"{parts[0]}.{parts[1]}"


def discover_projects() -> list[dict]:
    found = []
    for csproj in sorted(LYO_NET.rglob("*.csproj")):
        if any(p in SKIP_DIRS for p in csproj.parts):
            continue
        name = csproj.stem
        if not name.startswith("Lyo."):
            continue
        if name.endswith(".Tests") or name.endswith(".Benchmarks"):
            continue
        readme = csproj.parent / "README.md"
        if not readme.is_file():
            continue
        rel = readme.relative_to(ROOT).as_posix()
        found.append(
            {
                "id": name,
                "name": name,
                "dir": csproj.parent,
                "readme": readme,
                "readmePath": rel,
                "docsPath": csproj.parent / DOCS_FILENAME,
                "area": area_from_path(rel),
            }
        )
    return found


def split_h2(md: str) -> tuple[str, str, list[tuple[str, str]]]:
    """Return (title, intro, [(h2, body), ...])."""
    lines = md.replace("\r\n", "\n").split("\n")
    i = 0
    while i < len(lines) and not lines[i].strip():
        i += 1
    title = ""
    if i < len(lines) and lines[i].startswith("# "):
        title = lines[i][2:].strip()
        i += 1
    intro_lines = []
    while i < len(lines) and not lines[i].startswith("## "):
        intro_lines.append(lines[i])
        i += 1
    sections = []
    while i < len(lines):
        if not lines[i].startswith("## "):
            i += 1
            continue
        h2 = lines[i][3:].strip()
        i += 1
        body = []
        while i < len(lines) and not lines[i].startswith("## "):
            body.append(lines[i])
            i += 1
        sections.append((h2, "\n".join(body).strip()))
    return title, "\n".join(intro_lines).strip(), sections


def split_h3(body: str) -> list[tuple[str | None, str]]:
    lines = body.split("\n")
    parts: list[tuple[str | None, str]] = []
    title: str | None = None
    buf: list[str] = []

    def flush():
        nonlocal buf, title
        text = "\n".join(buf).strip()
        if text or title:
            parts.append((title, text))
        buf = []

    for line in lines:
        if line.startswith("### "):
            flush()
            title = line[4:].strip()
            continue
        buf.append(line)
    flush()
    return parts or [(None, body.strip())]


def iter_code_and_text(body: str):
    re_code = re.compile(r"```([^\n`]*)\n([\s\S]*?)```")
    last = 0
    for m in re_code.finditer(body):
        if m.start() > last:
            t = body[last : m.start()].strip()
            if t:
                yield ("text", None, t)
        lang = (m.group(1) or "csharp").strip() or "csharp"
        yield ("code", lang, m.group(2).rstrip("\n"))
        last = m.end()
    tail = body[last:].strip()
    if tail:
        yield ("text", None, tail)


def parse_feature_list(text: str) -> list[str]:
    """Top-level bullets; nested children collapsed onto parent with —."""
    items: list[str] = []
    current: str | None = None
    children: list[str] = []

    def flush():
        nonlocal current, children
        if current is None:
            return
        parent = strip_emoji(current)
        if children:
            items.append(f"{parent} — {'; '.join(strip_emoji(c) for c in children)}")
        else:
            items.append(parent)
        current = None
        children = []

    for line in text.split("\n"):
        m = re.match(r"^(\s*)([-*+]|\d+\.)\s+(.*)$", line)
        if m:
            indent = len(m.group(1))
            body = m.group(3).rstrip()
            # Top-level bullets sit at column 0 (or a single space); nested are indented 2+.
            if indent < 2:
                flush()
                current = body
            elif current is not None:
                children.append(body)
            else:
                current = body
            continue
        if current is not None and re.match(r"^\s+\S", line):
            if children:
                children[-1] = f"{children[-1]} {line.strip()}"
            else:
                current = f"{current} {line.strip()}"
            continue
        if not line.strip():
            continue
        if current is not None:
            break
    flush()
    return items


def parse_bullet_list(text: str) -> list[str] | None:
    items: list[str] = []
    for line in text.split("\n"):
        m = re.match(r"^\s*([-*+]|\d+\.)\s+(.*)$", line)
        if m:
            items.append(strip_emoji(m.group(2).rstrip()))
            continue
        if items and re.match(r"^\s+\S", line):
            items[-1] = f"{items[-1]} {line.strip()}"
            continue
        if not line.strip():
            continue
        if items:
            break
    return items or None


def split_table_cells(line: str) -> list[str]:
    line = line.strip()
    if line.startswith("|"):
        line = line[1:]
    if line.endswith("|"):
        line = line[:-1]
    return [c.strip() for c in line.split("|")]


def is_table_separator(line: str) -> bool:
    cells = split_table_cells(line)
    if not cells:
        return False
    return all(re.fullmatch(r":?-{3,}:?", c.replace(" ", "")) for c in cells if c)


def parse_markdown_table(text: str) -> dict | None:
    """
    Parse a markdown pipe-table into {headers, rows, lead?, trail?}.
    Returns None when the body is not primarily a table.
    """
    text = text.strip()
    if not text or "|" not in text:
        return None

    lines = text.replace("\r\n", "\n").split("\n")
    # Find first table header + separator.
    start = None
    for i in range(len(lines) - 1):
        if (
            lines[i].strip().startswith("|")
            and is_table_separator(lines[i + 1])
        ):
            start = i
            break
    if start is None:
        return None

    headers = split_table_cells(lines[start])
    if not headers:
        return None

    rows: list[list[str]] = []
    end = start + 2
    while end < len(lines):
        line = lines[end].strip()
        if not line.startswith("|"):
            break
        if is_table_separator(line):
            end += 1
            continue
        cells = split_table_cells(lines[end])
        # Pad / trim to header width
        if len(cells) < len(headers):
            cells = cells + [""] * (len(headers) - len(cells))
        elif len(cells) > len(headers):
            cells = cells[: len(headers)]
        rows.append(cells)
        end += 1

    if not rows:
        return None

    lead = "\n".join(lines[:start]).strip()
    trail = "\n".join(lines[end:]).strip()
    # Reject if surrounding prose dominates (mixed narrative docs stay markdown).
    non_table = (lead + "\n" + trail).strip()
    table_lines = end - start
    if non_table and len(non_table) > 400 and table_lines < 4:
        return None

    out: dict = {"headers": headers, "rows": rows}
    if lead:
        out["lead"] = lead
    if trail:
        out["trail"] = trail
    return out


def table_to_markdown(headers: list[str], rows: list[list[str]]) -> str:
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


def text_to_section(title: str, text: str) -> dict | None:
    text = strip_emoji(text)
    if not text:
        return None
    bullets = parse_bullet_list(text)
    if bullets:
        non_empty = [l for l in text.split("\n") if l.strip()]
        bulletish = sum(
            1
            for l in non_empty
            if re.match(r"^\s*([-*+]|\d+\.)\s+", l) or re.match(r"^\s{2,}\S", l)
        )
        if non_empty and bulletish / len(non_empty) >= 0.55:
            return {"type": "list", "title": title, "items": bullets}

    table = parse_markdown_table(text)
    if table:
        section = {"type": "table", "title": title, "headers": table["headers"], "rows": table["rows"]}
        if table.get("lead"):
            section["lead"] = table["lead"]
        if table.get("trail"):
            section["trail"] = table["trail"]
        return section

    if len(text) < 700 and "\n\n\n" not in text and not re.search(r"^#{1,6}\s", text, re.M):
        return {"type": "paragraph", "title": title, "text": collapse_ws(text)}
    return {"type": "markdown", "title": title, "body": text}


def detect_benchmarks(pkg: dict) -> dict | None:
    items = []
    readme_dir = (ROOT / pkg["readmePath"]).parent
    candidates = [
        readme_dir / "BENCHMARK_SUMMARY.md",
        readme_dir.parent / f"{pkg['id']}.Benchmarks" / "BENCHMARK_SUMMARY.md",
        readme_dir / f"{pkg['id']}.Benchmarks" / "BENCHMARK_SUMMARY.md",
    ]
    if pkg["id"].startswith("Lyo.Encryption"):
        candidates.append(readme_dir.parent / "Lyo.Encryption.Benchmarks" / "BENCHMARK_SUMMARY.md")
    if pkg["id"].startswith("Lyo.Compression"):
        candidates.append(readme_dir.parent / "Lyo.Compression.Benchmarks" / "BENCHMARK_SUMMARY.md")
    seen = set()
    for abs_path in candidates:
        if not abs_path.is_file():
            continue
        rel = abs_path.relative_to(ROOT).as_posix()
        if rel in seen:
            continue
        seen.add(rel)
        items.append({"label": "Benchmark summary", "href": rel})
    suite = SUITE_BY_PACKAGE.get(pkg["id"])
    if not items and not suite:
        return None
    out: dict = {}
    if suite:
        out["suite"] = suite
    if items:
        out["items"] = items
    return out or None


def readme_to_docs(md: str, meta: dict) -> dict:
    md = strip_emoji(md)
    title, intro, h2s = split_h2(md)
    description = strip_emoji(intro)
    tagline = make_tagline(description) or meta["id"]

    features: list[str] = []
    examples: list[dict] = []
    sections: list[dict] = []

    for h2_raw, body in h2s:
        h2 = strip_emoji(h2_raw)
        if not body or SKIP_H2.match(h2):
            continue

        if FEATURES_H2.match(h2):
            features.extend(parse_feature_list(body) or parse_bullet_list(body) or [])
            continue

        as_examples = bool(EXAMPLE_H2.match(h2) or EXAMPLE_H2_LOOSE.search(h2))
        h3parts = split_h3(body)

        if as_examples:
            # Named examples: one per code block; prefer ### title, then bold lead-in, then H2.
            for h3, part_body in h3parts:
                part_title = strip_emoji(h3) if h3 else None
                code_idx = 0
                pending_lead = None
                prose_bits = []
                for kind, lang, value in iter_code_and_text(part_body):
                    if kind != "code":
                        lead = extract_lead_title(value)
                        if lead:
                            pending_lead = lead
                        if value.strip():
                            prose_bits.append(value.strip())
                        continue
                    code_idx += 1
                    title_ex = (
                        pending_lead
                        or part_title
                        or (h2 if code_idx == 1 else f"{h2} ({code_idx})")
                    )
                    title_ex = EXAMPLE_TITLE_ALIASES.get(title_ex, title_ex)
                    if not pending_lead and not part_title:
                        title_ex = EXAMPLE_TITLE_ALIASES.get(h2, title_ex)
                    examples.append(
                        {
                            "title": uniquify_title(title_ex, examples),
                            "language": lang or "csharp",
                            "code": value,
                        }
                    )
                    pending_lead = None
                # Keep non-code prose (tables, etc.) as a section under the H2/H3.
                if prose_bits:
                    st = part_title or h2
                    # Drop pure bold labels that were only example captions.
                    joined = "\n\n".join(
                        p
                        for p in prose_bits
                        if not re.fullmatch(r"\*\*[^*]+\*\*:?", p.strip())
                    )
                    if joined.strip():
                        section = text_to_section(st, joined)
                        if section:
                            sections.append(section)
            continue

        # Non-example sections: keep each ## / ### as ONE block so interleaved
        # prose + code does not repeat the same heading for every chunk.
        for h3, part_body in h3parts:
            part_title = strip_emoji(h3) if h3 else None
            section_title = f"{h2} — {part_title}" if part_title else h2
            part_body = part_body.strip()
            if not part_body:
                continue

            chunks = list(iter_code_and_text(part_body))
            code_chunks = [c for c in chunks if c[0] == "code"]
            text_chunks = [c for c in chunks if c[0] == "text"]

            # Pure list (Events, etc.)
            if len(code_chunks) == 0 and text_chunks:
                section = text_to_section(section_title, part_body)
                if section:
                    sections.append(section)
                continue

            # Single code fence with little/no prose → code section
            if len(code_chunks) == 1 and sum(len(t[2]) for t in text_chunks) < 80:
                sections.append(
                    {
                        "type": "code",
                        "title": section_title,
                        "language": code_chunks[0][1] or "csharp",
                        "code": code_chunks[0][2],
                    }
                )
                continue

            # Mixed prose + one or more code blocks → one markdown section (keeps fences).
            sections.append(
                {
                    "type": "markdown",
                    "title": section_title,
                    "body": part_body,
                }
            )

    # Packages without a ## Features heading: promote the first overview-style list.
    if not features:
        promote = re.compile(
            r"^(registration|overview|highlights|capabilities|what it (is|does)|summary)$",
            re.I,
        )
        for section in sections:
            if section.get("type") != "list":
                continue
            st = section.get("title") or ""
            if promote.match(st) or promote.match(st.split(" — ")[0]):
                features = list(section.get("items") or [])
                break

    pkg = {
        "id": meta["id"],
        "name": title or meta["id"],
        "area": meta["area"],
        "tagline": tagline,
        "description": description or tagline,
        "features": features,
        "examples": examples,
        "sections": sections,
        "links": [],
        "readmePath": meta["readmePath"],
    }
    # Promote leftover standalone code sections into examples (keeps docs scannable).
    remaining_sections = []
    for section in sections:
        if section.get("type") == "code" and (section.get("code") or "").strip():
            title = section.get("title") or "Example"
            # Prefer the leaf after " — " for display.
            if " — " in title:
                leaf = title.split(" — ")[-1].strip()
                title = EXAMPLE_TITLE_ALIASES.get(leaf, leaf)
            title = EXAMPLE_TITLE_ALIASES.get(title, title)
            if not any(e.get("code") == section["code"] for e in examples):
                examples.append(
                    {
                        "title": title,
                        "language": section.get("language") or "csharp",
                        "code": section["code"],
                    }
                )
            continue
        remaining_sections.append(section)
    pkg["sections"] = remaining_sections
    pkg["examples"] = examples

    benches = detect_benchmarks(pkg)
    if benches:
        pkg["benchmarks"] = benches
    return pkg


def section_to_md(section: dict, level: int = 2) -> str:
    h = "#" * level
    parts = []
    title = section.get("title")
    if title:
        parts.append(f"{h} {title}")
        parts.append("")
    t = section.get("type")
    if t == "paragraph":
        parts.append(section.get("text") or "")
        parts.append("")
    elif t == "list":
        for idx, item in enumerate(section.get("items") or []):
            if section.get("ordered"):
                parts.append(f"{idx + 1}. {item}")
            else:
                parts.append(f"- {item}")
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
    return "\n".join(parts)


def docs_to_readme(pkg: dict) -> str:
    tagline = (pkg.get("tagline") or "").strip()
    desc = (pkg.get("description") or "").strip()
    lines = [
        f"# {pkg.get('name') or pkg['id']}",
        "",
    ]
    # Prefer full description; fall back to tagline. Avoid printing both when one is a prefix of the other.
    if desc:
        lines.extend([desc, ""])
    elif tagline:
        lines.extend([tagline, ""])

    if pkg.get("features"):
        lines.append("## Features")
        lines.append("")
        for f in pkg["features"]:
            lines.append(f"- {f}")
        lines.append("")

    if pkg.get("examples"):
        lines.append("## Examples")
        lines.append("")
        for ex in pkg["examples"]:
            if ex.get("title"):
                lines.append(f"### {ex['title']}")
                lines.append("")
            lines.append(f"```{ex.get('language') or 'csharp'}")
            lines.append(ex.get("code") or "")
            lines.append("```")
            lines.append("")

    benches = pkg.get("benchmarks")
    if benches and (benches.get("headline") or benches.get("suite") or benches.get("items")):
        lines.append("## Benchmarks")
        lines.append("")
        if benches.get("headline"):
            lines.append(benches["headline"])
            lines.append("")
        if benches.get("suite"):
            lines.append(f"- Portfolio suite: `{benches['suite']}`")
        for item in benches.get("items") or []:
            note = f" — {item['note']}" if item.get("note") else ""
            lines.append(f"- [{item['label']}]({item['href']}){note}")
        lines.append("")

    for section in pkg.get("sections") or []:
        if is_legacy_deps_section(section):
            continue
        lines.append(section_to_md(section, 2).rstrip())
        lines.append("")

    if pkg.get("links"):
        lines.append("## Links")
        lines.append("")
        for link in pkg["links"]:
            lines.append(f"- [{link['label']}]({link['href']})")
        lines.append("")

    deps = pkg.get("dependencies") or []
    if deps:
        lines.append("## Dependencies")
        lines.append("")
        lines.append(
            "Generated from `ProjectReference` / `PackageReference` "
            "(same model as `docs/Lyo.ProjectGraph.html`)."
        )
        lines.append("")
        for dep in deps:
            name = dep.get("name") or ""
            tags = dep.get("tags") or []
            tag_str = ", ".join(tags)
            version = dep.get("version")
            label = f"`{name}`"
            ver = f" `{version}`" if version else ""
            lines.append(f"- {label}{ver} — ({tag_str})" if tag_str else f"- {label}{ver}")
        lines.append("")

    text = "\n".join(lines)
    text = re.sub(r"\n{3,}", "\n\n", text).strip() + "\n"
    return strip_emoji(text)


LEGACY_DEPS_TITLE = re.compile(
    r"^(related\s+packages|related\s+projects|dependencies)(\b| — |-|:)",
    re.I,
)


def is_legacy_deps_section(section: dict) -> bool:
    title = (section.get("title") or "").strip()
    return bool(LEGACY_DEPS_TITLE.match(title))


def is_package_reference_example(example: dict) -> bool:
    code = (example.get("code") or "").strip()
    if "PackageReference" not in code:
        return False
    # Pure PackageReference snippet (no real API usage)
    lines = [l for l in code.splitlines() if l.strip()]
    return all("PackageReference" in l or l.strip().startswith("<") or l.strip().startswith("//") for l in lines)


def sync_dependencies_into_docs() -> int:
    """Write `dependencies` + `targetFrameworks` onto every docs.json; strip legacy deps sections."""
    dep_map = load_dependency_map(include_tests=False)
    updated = 0
    for proj in discover_projects():
        if not proj["docsPath"].is_file():
            continue
        pkg = json.loads(proj["docsPath"].read_text(encoding="utf-8"))
        deps = dep_map.get(proj["id"], [])
        pkg["dependencies"] = deps
        csproj = find_project_csproj(proj["dir"]) or (proj["dir"] / f"{proj['id']}.csproj")
        pkg["targetFrameworks"] = read_target_frameworks(csproj)
        pkg["sections"] = [s for s in (pkg.get("sections") or []) if not is_legacy_deps_section(s)]
        pkg["examples"] = [e for e in (pkg.get("examples") or []) if not is_package_reference_example(e)]
        write_json(proj["docsPath"], pkg)
        updated += 1
    return updated


def write_json(path: Path, data) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def cmd_extract(*, force: bool = False) -> None:
    """Legacy README → docs.json import. Overwrites the source of truth — refuse without --force."""
    if not force:
        raise SystemExit(
            "extract overwrites docs.json (the source of truth) from generated READMEs and is lossy.\n"
            "Refuse to run. If you truly need a one-time import: "
            "python3 scripts/docs/project-docs.py extract --force\n"
            "Normal workflow: edit docs.json, then: python3 scripts/docs/project-docs.py render"
        )
    projects = discover_projects()
    ok = 0
    for proj in projects:
        md = proj["readme"].read_text(encoding="utf-8")
        pkg = readme_to_docs(md, proj)
        write_json(proj["docsPath"], pkg)
        ok += 1
    print(f"extract --force: overwrote {ok} {DOCS_FILENAME} files from READMEs (SoT replaced)")


def load_all_docs() -> list[dict]:
    packages = []
    for proj in discover_projects():
        if not proj["docsPath"].is_file():
            print(f"warn: missing {proj['docsPath']}", file=sys.stderr)
            continue
        pkg = json.loads(proj["docsPath"].read_text(encoding="utf-8"))
        # keep path fields honest
        pkg["id"] = pkg.get("id") or proj["id"]
        pkg["readmePath"] = proj["readmePath"]
        pkg["area"] = pkg.get("area") or proj["area"]
        packages.append(pkg)
    packages.sort(
        key=lambda p: (
            AREA_ORDER.index(p["area"]) if p.get("area") in AREA_ORDER else 99,
            p["id"],
        )
    )
    return packages


def cmd_sync_deps() -> None:
    n = sync_dependencies_into_docs()
    print(f"sync-deps: wrote dependencies onto {n} docs.json files")


def replace_marked_region(source: str, start_marker: str, end_marker: str, replacement: str) -> str:
    start = source.find(start_marker)
    end = source.find(end_marker)
    if start == -1 or end == -1 or end < start:
        raise ValueError(f"Missing markers {start_marker!r} / {end_marker!r}")
    return (
        source[: start + len(start_marker)]
        + "\n\n"
        + replacement.strip()
        + "\n\n"
        + source[end:]
    )


def render_root_packages_list(packages: list[dict]) -> str:
    by_area: dict[str, list[dict]] = {a: [] for a in AREA_ORDER}
    for pkg in packages:
        area = pkg.get("area") if pkg.get("area") in by_area else "Other"
        by_area.setdefault(area, []).append(pkg)
    parts: list[str] = []
    for area in AREA_ORDER:
        items = by_area.get(area) or []
        if not items or area == "Other":
            continue
        parts.append(f"### {area}")
        parts.append("")
        for pkg in items:
            tagline = (pkg.get("tagline") or "").replace("\n", " ").strip()
            parts.append(f"- [{pkg.get('name') or pkg['id']}]({pkg['readmePath']}): {tagline}")
        parts.append("")
    return "\n".join(parts).strip()


def cmd_render() -> None:
    # Always refresh dependencies from csproj / graph before emitting consumers.
    cmd_sync_deps()
    packages = load_all_docs()
    if not packages:
        raise SystemExit(
            "No docs.json found — create one from docs/catalog/templates/package.template.json "
            "(docs.json is the source of truth; do not extract from README)"
        )

    for pkg in packages:
        readme_path = ROOT / pkg["readmePath"]
        readme_path.write_text(docs_to_readme(pkg), encoding="utf-8")

    # Root README package index (hand-maintained capabilities table stays untouched).
    if ROOT_README.is_file():
        root_md = ROOT_README.read_text(encoding="utf-8")
        try:
            root_md = replace_marked_region(
                root_md,
                "<!-- catalog:packages:start -->",
                "<!-- catalog:packages:end -->",
                render_root_packages_list(packages),
            )
            ROOT_README.write_text(root_md, encoding="utf-8")
        except ValueError as err:
            print(f"warn: root README package list not updated: {err}", file=sys.stderr)

    # Portfolio index + full docs (derived copies of project JSON)
    PORTFOLIO.mkdir(parents=True, exist_ok=True)
    if PORTFOLIO_FULL.exists():
        for old in PORTFOLIO_FULL.glob("*.json"):
            old.unlink()
    else:
        PORTFOLIO_FULL.mkdir(parents=True)

    index = [
        {
            "id": p["id"],
            "name": p.get("name") or p["id"],
            "area": p.get("area") or "Other",
            "topic": package_topic(p["id"]),
            "tagline": p.get("tagline") or "",
            "readme": p["readmePath"],
            "targetFrameworks": p.get("targetFrameworks") or [],
        }
        for p in packages
    ]
    write_json(PORTFOLIO / "packages.json", index)
    for p in packages:
        write_json(PORTFOLIO_FULL / f"{p['id']}.json", p)

    # Blazor catalog mirror
    blazor_packages = BLAZOR / "packages"
    blazor_packages.mkdir(parents=True, exist_ok=True)
    for old in blazor_packages.glob("*.json"):
        old.unlink()
    for p in packages:
        write_json(blazor_packages / f"{p['id']}.json", p)
    write_json(
        BLAZOR / "index.json",
        {
            "generatedAt": __import__("datetime").datetime.now(__import__("datetime").timezone.utc).isoformat(),
            "packageCount": len(packages),
            "packages": [
                {
                    "id": p["id"],
                    "area": p.get("area"),
                    "topic": package_topic(p["id"]),
                    "name": p.get("name"),
                    "tagline": p.get("tagline"),
                    "targetFrameworks": p.get("targetFrameworks") or [],
                }
                for p in packages
            ],
        },
    )

    print(f"render: {len(packages)} READMEs + root README + portfolio + Blazor from project {DOCS_FILENAME}")

    # Adhoc tooling READMEs (scripts/*, k6 matrix, etc.) — same SoT rule via tooling-docs.
    tooling = Path(__file__).with_name("tooling-docs.py")
    if tooling.is_file():
        import importlib.util

        spec = importlib.util.spec_from_file_location("lyo_tooling_docs", tooling)
        if spec and spec.loader:
            mod = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(mod)
            mod.render()
    else:
        print("warn: tooling-docs.py not found; skipped adhoc tooling READMEs", file=sys.stderr)


def cmd_audit() -> None:
    packages = load_all_docs()
    empty_f = empty_e = good = 0
    samples = [
        "Lyo.FileSystemWatcher",
        "Lyo.Compression",
        "Lyo.Cache",
        "Lyo.Encryption",
    ]
    for p in packages:
        fc = len(p.get("features") or [])
        ec = len(p.get("examples") or [])
        if not fc:
            empty_f += 1
        if not ec:
            empty_e += 1
        if fc and ec:
            good += 1
    print(
        json.dumps(
            {
                "total": len(packages),
                "emptyFeatures": empty_f,
                "emptyExamples": empty_e,
                "withBoth": good,
            },
            indent=2,
        )
    )
    for sid in samples:
        p = next((x for x in packages if x["id"] == sid), None)
        if not p:
            print(sid, "MISSING")
            continue
        print(
            sid,
            {
                "features": len(p.get("features") or []),
                "examples": len(p.get("examples") or []),
                "sections": len(p.get("sections") or []),
                "exTitles": [e.get("title") for e in (p.get("examples") or [])[:8]],
                "feat0": (p.get("features") or [""])[0][:90],
            },
        )


def main():
    args = [a.lower() for a in sys.argv[1:]]
    cmd = args[0] if args else "render"
    force = "--force" in args
    if cmd == "extract":
        cmd_extract(force=force)
    elif cmd in ("sync-deps", "deps"):
        cmd_sync_deps()
    elif cmd == "render":
        cmd_render()
    elif cmd == "audit":
        cmd_audit()
    elif cmd == "all":
        # Never extract here — docs.json is SoT; "all" means render + audit only.
        cmd_render()
        cmd_audit()
    else:
        print(
            "Usage: project-docs.py render|sync-deps|audit|all\n"
            "       project-docs.py extract --force   # dangerous: README → overwrites docs.json",
            file=sys.stderr,
        )
        sys.exit(1)


if __name__ == "__main__":
    main()
