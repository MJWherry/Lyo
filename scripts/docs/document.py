#!/usr/bin/env python3
"""Pydantic document/table config and resolve step ({{ }} → IR).

Emit via ``html.py`` / ``markdown.py``. Expressions live in ``lyo_tooling.expression``.
"""

from __future__ import annotations

import logging
import re
import secrets
import sys
from pathlib import Path
from typing import Any, Literal

from pydantic import BaseModel, Field, model_validator

_SCRIPTS = Path(__file__).resolve().parents[1]
if str(_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS))

from lyo_tooling.expression import (
    MISSING,
    StrictModeError,
    eval_condition,
    eval_path,
    extract_token,
    fail_or_warn,
    render_template,
    reset_strict,
    resolve_path,
    set_strict,
)

logger = logging.getLogger(__name__)

HtmlTableError = StrictModeError

_CAMEL_RE = re.compile(r"([a-z0-9])([A-Z])")
_DEFAULT_STRIPE = {"background_color": "#f8fafc"}
_GROUP_TMPL = re.compile(r"^([A-Za-z_][A-Za-z0-9_]*)\.(.+)$")


def normalize_css_key(key: str) -> str:
    """``border_bottom`` / ``borderBottom`` / ``border-bottom`` → ``border-bottom``."""

    key = str(key).strip()
    if key.startswith("--"):
        return key.replace("_", "-")
    return _CAMEL_RE.sub(r"\1-\2", key).replace("_", "-").lower()


def _css_value(value: Any) -> str:
    return str(value.value) if hasattr(value, "value") and not isinstance(value, (str, bytes)) else str(value)


def style_to_css(style: dict[str, Any] | None) -> dict[str, str]:
    if not style:
        return {}
    out: dict[str, str] = {}
    for key, value in style.items():
        if key == "extends" or value is None:
            continue
        if isinstance(value, dict):
            continue
        out[normalize_css_key(key)] = _css_value(value)
    return out


def _css_to_str(css: dict[str, str]) -> str:
    return ";".join(f"{k}:{v}" for k, v in css.items())


def _resolve_named_style(
    name: str | None,
    registry: dict[str, dict[str, Any]],
    *,
    stack: list[str] | None = None,
) -> dict[str, str]:
    if not name:
        return {}
    stack = stack or []
    if name in stack:
        fail_or_warn("Style extends cycle detected: %s", " -> ".join([*stack, name]))
        return {}
    style = registry.get(name)
    if style is None:
        fail_or_warn("Unknown style_name %r", name)
        return {}
    merged: dict[str, str] = {}
    extends = style.get("extends")
    if extends:
        merged.update(_resolve_named_style(str(extends), registry, stack=[*stack, name]))
    merged.update(style_to_css(style))
    return merged


def _apply_style_ref(
    css: dict[str, str],
    *,
    style_name: str | None,
    style: dict[str, Any] | None,
    registry: dict[str, dict[str, Any]],
) -> None:
    if style_name:
        css.update(_resolve_named_style(style_name, registry))
    if style:
        extends = style.get("extends")
        if extends:
            css.update(_resolve_named_style(str(extends), registry))
        css.update(style_to_css(style))


class StyleRule(BaseModel):
    """Conditional style applied when ``when`` is a truthy ``{{ ... }}`` expression."""

    when: str
    style: dict[str, Any] | None = None
    style_name: str | None = None

    @model_validator(mode="after")
    def _require_style_or_name(self) -> StyleRule:
        if self.style is None and not self.style_name:
            raise ValueError("StyleRule requires style or style_name")
        return self


class ColumnConfig(BaseModel):
    style: dict[str, Any] | None = None
    style_name: str | None = None
    empty_text: str | None = None
    value: str | None = None
    css_class: str | None = None


class CellConfig(BaseModel):
    value: str = ""
    link: str | None = None
    style: dict[str, Any] | None = None
    style_name: str | None = None
    style_rules: list[StyleRule] = Field(default_factory=list)
    empty_text: str | None = None
    hide_when: str | None = None
    css_class: str | None = None
    colspan: int = 1
    rowspan: int = 1
    raw: bool = False


class RowConfig(BaseModel):
    cells: list[CellConfig] = Field(default_factory=list)
    style: dict[str, Any] | None = None
    style_name: str | None = None
    style_rules: list[StyleRule] = Field(default_factory=list)
    hide_when: str | None = None
    filter_when: str | None = None
    repeat_for: str | None = None
    item_alias: str = "item"
    sort_by: str | list[str] | None = None
    sort_desc: bool = False
    limit: int | None = None
    css_class: str | None = None
    group_by: str | list[str] | None = None


class HtmlTableConfig(BaseModel):
    """Single-table HTML/Markdown config."""

    model_config = {"extra": "forbid"}

    type: Literal["table"] | None = None
    id: str | None = None
    title: str | None = None
    caption: str | None = None
    base_css: str | None = None
    styles: dict[str, dict[str, Any]] = Field(default_factory=dict)
    columns: list[ColumnConfig] = Field(default_factory=list)
    headers: list[RowConfig] = Field(default_factory=list)
    rows: list[RowConfig] = Field(default_factory=list)
    footers: list[RowConfig] = Field(default_factory=list)
    table_style: dict[str, Any] | None = None
    default_cell_style: dict[str, Any] | None = None
    default_cell_style_name: str | None = None
    css_class: str | None = None
    striped: bool = False
    stripe_style_name: str | None = None
    strict: bool = False
    hide_when: str | None = None


ReportConfig = HtmlTableConfig


class HeadingBlock(BaseModel):
    type: Literal["heading"] = "heading"
    level: int = 1
    value: str = ""
    css_class: str | None = None
    hide_when: str | None = None


class ParagraphBlock(BaseModel):
    type: Literal["paragraph"] = "paragraph"
    value: str = ""
    css_class: str | None = None
    hide_when: str | None = None


class ListBlock(BaseModel):
    type: Literal["list"] = "list"
    ordered: bool = False
    items: list[Any] = Field(default_factory=list)
    item: str | None = None
    repeat_for: str | None = None
    item_alias: str = "item"
    css_class: str | None = None
    hide_when: str | None = None


class CodeBlock(BaseModel):
    type: Literal["code"] = "code"
    language: str = "text"
    value: str = ""
    code: str | None = None
    css_class: str | None = None
    hide_when: str | None = None


class HrBlock(BaseModel):
    type: Literal["hr"] = "hr"
    css_class: str | None = None
    hide_when: str | None = None


class HtmlBlock(BaseModel):
    type: Literal["html"] = "html"
    value: str = ""
    raw: bool = True
    css_class: str | None = None
    hide_when: str | None = None


class SectionBlock(BaseModel):
    type: Literal["section"] = "section"
    title: str | None = None
    css_class: str | None = None
    hide_when: str | None = None
    blocks: list[dict[str, Any]] = Field(default_factory=list)


class DocumentConfig(BaseModel):
    """Multi-block document (sections, tables, lists, …)."""

    model_config = {"extra": "forbid"}

    id: str | None = None
    name: str | None = None
    title: str | None = None
    tagline: str | None = None
    description: str | None = None
    styles: dict[str, dict[str, Any]] = Field(default_factory=dict)
    blocks: list[dict[str, Any]] = Field(default_factory=list)
    strict: bool = False


class RenderedCell(BaseModel):
    html: str
    text: str = ""
    href: str | None = None
    tag: str
    style_css: str
    css_class: str | None = None
    colspan: int = 1
    rowspan: int = 1


class RenderedRow(BaseModel):
    cells: list[RenderedCell] = Field(default_factory=list)
    style_css: str = ""
    css_class: str | None = None


class RenderedTable(BaseModel):
    title: str | None = None
    caption: str | None = None
    thead: list[RenderedRow] = Field(default_factory=list)
    tbody: list[RenderedRow] = Field(default_factory=list)
    tfoot: list[RenderedRow] = Field(default_factory=list)
    table_style_css: str = ""
    css_class: str | None = None
    table_id: str = ""
    base_css: str | None = None


class ResolvedDocument(BaseModel):
    name: str | None = None
    tagline: str | None = None
    description: str | None = None
    blocks: list[dict[str, Any]] = Field(default_factory=list)


def _join_css_classes(*parts: str | None) -> str | None:
    seen: list[str] = []
    for part in parts:
        if not part:
            continue
        for token in part.split():
            if token and token not in seen:
                seen.append(token)
    return " ".join(seen) if seen else None


def _new_table_id() -> str:
    return "ht_" + secrets.token_hex(4)


class _Desc:
    __slots__ = ("value",)

    def __init__(self, value: Any) -> None:
        self.value = value

    def __lt__(self, other: object) -> bool:
        if not isinstance(other, _Desc):
            return NotImplemented
        try:
            return self.value > other.value
        except TypeError:
            return str(self.value) > str(other.value)

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, _Desc):
            return NotImplemented
        return self.value == other.value


def _group_fields(group_by: str | list[str] | None) -> list[str]:
    if not group_by:
        return []
    if isinstance(group_by, str):
        return [group_by] if group_by else []
    return [g for g in group_by if g]


def _expr_path(template: str) -> str | None:
    inner = extract_token(template.strip()) if template else None
    if inner is None:
        return None
    expr = inner
    if "?" not in expr:
        colon = expr.find(":")
        if colon != -1:
            expr = expr[:colon]
    return expr.strip()


def _path_matches_field(expr: str | None, alias: str, field: str) -> bool:
    if not expr:
        return False
    if expr == field:
        return True
    return expr == f"{alias}.{field}"


def _reorder_for_group(
    row: RowConfig,
    config: HtmlTableConfig,
    fields: list[str],
) -> tuple[RowConfig, HtmlTableConfig, int]:
    """Move/inject group-by cells to the left. Returns (row, config, injected_count)."""

    alias = row.item_alias
    cells = list(row.cells)
    columns = list(config.columns)
    used: set[int] = set()
    ordered_cells: list[CellConfig] = []
    ordered_cols: list[ColumnConfig] = []
    injected = 0

    for field in fields:
        found: int | None = None
        for i, cell in enumerate(cells):
            if i in used:
                continue
            col = columns[i] if i < len(columns) else None
            tmpl = cell.value if cell.value != "" else (col.value if col and col.value is not None else "")
            if _path_matches_field(_expr_path(tmpl), alias, field):
                found = i
                break
        if found is not None:
            used.add(found)
            ordered_cells.append(cells[found])
            ordered_cols.append(columns[found] if found < len(columns) else ColumnConfig())
        else:
            ordered_cells.append(CellConfig(value="{{" + alias + "." + field + "}}"))
            ordered_cols.append(ColumnConfig())
            injected += 1

    for i, cell in enumerate(cells):
        if i not in used:
            ordered_cells.append(cell)
            ordered_cols.append(columns[i] if i < len(columns) else ColumnConfig())

    new_row = row.model_copy(update={"cells": ordered_cells})
    new_config = config.model_copy(update={"columns": ordered_cols})
    return new_row, new_config, injected


def _header_cells_for_group(
    header: RowConfig,
    orig_row: RowConfig,
    orig_config: HtmlTableConfig,
    fields: list[str],
) -> list[CellConfig]:
    alias = orig_row.item_alias
    cells = list(orig_row.cells)
    used: set[int] = set()
    out: list[CellConfig] = []
    for field in fields:
        found: int | None = None
        for i, cell in enumerate(cells):
            if i in used:
                continue
            col = orig_config.columns[i] if i < len(orig_config.columns) else None
            tmpl = cell.value if cell.value != "" else (col.value if col and col.value is not None else "")
            if _path_matches_field(_expr_path(tmpl), alias, field):
                found = i
                break
        if found is not None:
            used.add(found)
            out.append(header.cells[found] if found < len(header.cells) else CellConfig(value=field))
        else:
            out.append(CellConfig(value=field.replace("_", " ").title()))
    for i in range(len(cells)):
        if i not in used:
            out.append(header.cells[i] if i < len(header.cells) else CellConfig())
    return out


def _group_key(element: Any, fields: list[str]) -> tuple[Any, ...]:
    parts: list[Any] = []
    for field in fields:
        val = resolve_path(field, element)
        parts.append(None if val is MISSING else val)
    return tuple(parts)


def _group_runs(items: list[Any], fields: list[str]) -> list[int]:
    """rowspan for each item index (1 if not first in run; N on first)."""

    if not items or not fields:
        return [1] * len(items)
    keys = [_group_key(el, fields) for el in items]
    spans = [1] * len(items)
    i = 0
    while i < len(keys):
        j = i + 1
        while j < len(keys) and keys[j] == keys[i]:
            j += 1
        spans[i] = j - i
        for k in range(i + 1, j):
            spans[k] = 0
        i = j
    return spans


class TableResolver:
    def __init__(self, logger: logging.Logger | None = None) -> None:
        self.logger = logger or globals()["logger"]

    def resolve_table(self, config: HtmlTableConfig, data: Any) -> RenderedTable:
        table_id = config.id or _new_table_id()
        context = _build_context(data, table_id)
        config = self._apply_group_header_permute(config)
        base_css = (
            render_template(config.base_css, context, escape=False)
            if config.base_css
            else None
        )
        return RenderedTable(
            title=render_template(config.title, context),
            caption=render_template(config.caption, context),
            thead=self._resolve_section(config.headers, config, context, section="headers"),
            tbody=self._resolve_section(config.rows, config, context, section="rows"),
            tfoot=self._resolve_section(config.footers, config, context, section="footers"),
            table_style_css=_css_to_str(self._style_dict(config.table_style, None, config)),
            css_class=config.css_class,
            table_id=table_id,
            base_css=base_css,
        )

    def _apply_group_header_permute(self, config: HtmlTableConfig) -> HtmlTableConfig:
        body_repeat = next((r for r in config.rows if r.repeat_for and _group_fields(r.group_by)), None)
        if body_repeat is None:
            return config
        fields = _group_fields(body_repeat.group_by)
        if len(config.headers) != 1:
            self.logger.debug("group_by: skip header permute (multi-row headers)")
            return config
        header = config.headers[0]
        if header.repeat_for or len(header.cells) != len(body_repeat.cells):
            self.logger.debug("group_by: skip header permute (header shape mismatch)")
            return config
        new_header_cells = _header_cells_for_group(header, body_repeat, config, fields)
        new_header = header.model_copy(update={"cells": new_header_cells})
        return config.model_copy(update={"headers": [new_header]})

    def _style_dict(
        self,
        style: dict[str, Any] | None,
        style_name: str | None,
        config: HtmlTableConfig,
    ) -> dict[str, str]:
        css: dict[str, str] = {}
        _apply_style_ref(css, style_name=style_name, style=style, registry=config.styles)
        return css

    def _resolve_section(
        self,
        rows: list[RowConfig],
        config: HtmlTableConfig,
        context: dict[str, Any],
        *,
        section: Literal["headers", "rows", "footers"],
    ) -> list[RenderedRow]:
        tag = "th" if section == "headers" else "td"
        apply_stripe = section == "rows" and config.striped
        rendered: list[RenderedRow] = []
        body_index = 0
        blocked: list[int] = []

        for row in rows:
            if row.repeat_for:
                expanded, blocked, body_index = self._expand_repeat(
                    row, config, context, tag=tag, apply_stripe=apply_stripe,
                    blocked=blocked, body_index=body_index,
                )
                rendered.extend(expanded)
            else:
                if row.hide_when and eval_condition(row.hide_when, context):
                    continue
                stripe = apply_stripe and (body_index % 2 == 0)
                resolved, blocked = self._resolve_row(
                    row, config, context, tag=tag, stripe=stripe, blocked=blocked
                )
                if resolved is not None:
                    rendered.append(resolved)
                    if section == "rows":
                        body_index += 1
        return rendered

    def _expand_repeat(
        self,
        row: RowConfig,
        config: HtmlTableConfig,
        context: dict[str, Any],
        *,
        tag: str,
        apply_stripe: bool,
        blocked: list[int],
        body_index: int,
    ) -> tuple[list[RenderedRow], list[int], int]:
        items = eval_path(row.repeat_for or "", context)
        if items is MISSING or items is None:
            fail_or_warn("repeat_for path %r resolved to nothing; emitting no rows", row.repeat_for)
            items = []
        if not isinstance(items, (list, tuple)):
            fail_or_warn(
                "repeat_for path %r did not resolve to a list; got %s",
                row.repeat_for,
                type(items).__name__,
            )
            items = []
        else:
            items = list(items)

        if row.filter_when:
            kept: list[Any] = []
            for element in items:
                child = dict(context)
                child[row.item_alias] = element
                if eval_condition(row.filter_when, child):
                    kept.append(element)
            items = kept

        fields = _group_fields(row.group_by)
        work_row, work_config = row, config
        if fields:
            sort_keys: list[str] = list(fields)
            extra = row.sort_by
            if extra:
                extra_list = [extra] if isinstance(extra, str) else list(extra)
                for raw in extra_list:
                    path = raw[1:] if raw.startswith("-") else raw
                    if path and path not in fields:
                        sort_keys.append(raw if raw.startswith("-") else path)
            items = self._sort_items(items, sort_keys, row.sort_desc)
            work_row, work_config, _inj = _reorder_for_group(row, config, fields)
        elif row.sort_by:
            items = self._sort_items(items, row.sort_by, row.sort_desc)

        if row.limit is not None:
            items = items[: max(0, row.limit)]

        spans = _group_runs(items, fields) if fields else [1] * len(items)
        group_cell_count = len(fields)
        self.logger.debug("Expanding %r into %d row(s)", row.repeat_for, len(items))

        rendered: list[RenderedRow] = []
        for idx, element in enumerate(items):
            child = dict(context)
            child[row.item_alias] = element
            child["index"] = idx
            child["index1"] = idx + 1
            if row.hide_when and eval_condition(row.hide_when, child):
                continue
            emit_row = work_row
            if fields and spans[idx] == 0:
                emit_row = work_row.model_copy(update={"cells": work_row.cells[group_cell_count:]})
            elif fields and spans[idx] > 1:
                new_cells = []
                for i, cell in enumerate(work_row.cells):
                    if i < group_cell_count:
                        new_cells.append(cell.model_copy(update={"rowspan": spans[idx]}))
                    else:
                        new_cells.append(cell)
                emit_row = work_row.model_copy(update={"cells": new_cells})

            stripe = apply_stripe and (body_index % 2 == 0)
            resolved, blocked = self._resolve_row(
                emit_row, work_config, child, tag=tag, stripe=stripe, blocked=blocked
            )
            if resolved is not None:
                rendered.append(resolved)
                body_index += 1
        return rendered, blocked, body_index

    def _sort_items(
        self,
        items: list[Any],
        sort_by: str | list[str],
        sort_desc: bool,
    ) -> list[Any]:
        if isinstance(sort_by, str):
            keys: list[tuple[str, bool]] = [(sort_by, sort_desc)]
        else:
            keys = []
            for raw in sort_by:
                desc = raw.startswith("-")
                path = raw[1:] if desc else raw
                if path:
                    keys.append((path, desc))
            if not keys:
                return items

        def multi_key(element: Any, *, as_str: bool) -> tuple[Any, ...]:
            parts: list[Any] = []
            for path, desc in keys:
                val = resolve_path(path, element)
                missing = val is MISSING or val is None
                if missing:
                    parts.append((1, ""))
                    continue
                if as_str:
                    val = str(val)
                if desc:
                    if isinstance(val, (int, float)) and not isinstance(val, bool):
                        parts.append((0, -val))
                    else:
                        parts.append((0, _Desc(val)))
                else:
                    parts.append((0, val))
            return tuple(parts)

        try:
            return sorted(items, key=lambda e: multi_key(e, as_str=False))
        except TypeError:
            return sorted(items, key=lambda e: multi_key(e, as_str=True))

    def _resolve_row(
        self,
        row: RowConfig,
        config: HtmlTableConfig,
        context: dict[str, Any],
        *,
        tag: str,
        stripe: bool,
        blocked: list[int],
    ) -> tuple[RenderedRow | None, list[int]]:
        cells: list[RenderedCell] = []
        occ = list(blocked)
        cursor = 0

        def _ensure(n: int) -> None:
            while len(occ) < n:
                occ.append(0)

        for cell in row.cells:
            span = max(1, cell.colspan)
            _ensure(cursor + 1)
            while cursor < len(occ) and occ[cursor] > 0:
                cursor += 1
            _ensure(cursor + span)
            resolved = self._resolve_cell(cell, config, context, tag=tag, col_index=cursor)
            if resolved is not None:
                cells.append(resolved)
            for i in range(span):
                occ[cursor + i] = max(occ[cursor + i], cell.rowspan)
            cursor += span

        next_blocked = [max(0, n - 1) for n in occ]
        if not cells and row.cells:
            return None, next_blocked

        css: dict[str, str] = {}
        if stripe:
            if config.stripe_style_name:
                css.update(_resolve_named_style(config.stripe_style_name, config.styles))
            else:
                css.update(style_to_css(_DEFAULT_STRIPE))

        _apply_style_ref(css, style_name=row.style_name, style=row.style, registry=config.styles)
        for rule in row.style_rules:
            if eval_condition(rule.when, context):
                _apply_style_ref(
                    css, style_name=rule.style_name, style=rule.style, registry=config.styles
                )

        return (
            RenderedRow(cells=cells, style_css=_css_to_str(css), css_class=row.css_class),
            next_blocked,
        )

    def _resolve_cell(
        self,
        cell: CellConfig,
        config: HtmlTableConfig,
        context: dict[str, Any],
        *,
        tag: str,
        col_index: int,
    ) -> RenderedCell | None:
        if cell.hide_when and eval_condition(cell.hide_when, context):
            return None

        column = config.columns[col_index] if col_index < len(config.columns) else None
        value_template = cell.value if cell.value != "" else (
            column.value if column and column.value is not None else ""
        )
        text = render_template(value_template, context, escape=not cell.raw)
        if not text.strip():
            empty = cell.empty_text if cell.empty_text is not None else (
                column.empty_text if column else None
            )
            if empty is not None:
                text = render_template(empty, context, escape=not cell.raw)

        href: str | None = None
        html_text = text
        if cell.link:
            href = render_template(cell.link, context, escape=True)
            if href:
                html_text = f'<a href="{href}">{text}</a>'

        css: dict[str, str] = {}
        _apply_style_ref(
            css,
            style_name=config.default_cell_style_name,
            style=config.default_cell_style,
            registry=config.styles,
        )
        if column:
            _apply_style_ref(
                css,
                style_name=column.style_name,
                style=column.style,
                registry=config.styles,
            )
        _apply_style_ref(
            css,
            style_name=cell.style_name,
            style=cell.style,
            registry=config.styles,
        )
        for rule in cell.style_rules:
            if eval_condition(rule.when, context):
                _apply_style_ref(
                    css,
                    style_name=rule.style_name,
                    style=rule.style,
                    registry=config.styles,
                )

        return RenderedCell(
            html=html_text,
            text=text,
            href=href,
            tag=tag,
            style_css=_css_to_str(css),
            css_class=_join_css_classes(
                column.css_class if column else None,
                cell.css_class,
            ),
            colspan=cell.colspan,
            rowspan=cell.rowspan,
        )


def _build_context(data: Any, table_id: str) -> dict[str, Any]:
    context: dict[str, Any] = {"report": data, "data": data, "table_id": table_id}
    if isinstance(data, dict):
        context.update(data)
        context["table_id"] = table_id
    return context


def _coerce_table(config: HtmlTableConfig | dict[str, Any] | str) -> HtmlTableConfig:
    if isinstance(config, HtmlTableConfig):
        return config
    if isinstance(config, str):
        return HtmlTableConfig.model_validate_json(config)
    return HtmlTableConfig.model_validate(config)


def _coerce_document(config: DocumentConfig | dict[str, Any] | str) -> DocumentConfig:
    if isinstance(config, DocumentConfig):
        return config
    if isinstance(config, str):
        return DocumentConfig.model_validate_json(config)
    return DocumentConfig.model_validate(config)


def _is_table_config(raw: dict[str, Any]) -> bool:
    if raw.get("type") == "table":
        return True
    return "headers" in raw or "rows" in raw or "columns" in raw


def _resolve_list_items(block: ListBlock, context: dict[str, Any]) -> list[Any]:
    if block.repeat_for:
        items = eval_path(block.repeat_for, context)
        if items is MISSING or not isinstance(items, (list, tuple)):
            return []
        out: list[Any] = []
        tmpl = block.item or "{{item}}"
        for element in items:
            child = dict(context)
            child[block.item_alias] = element
            out.append(render_template(tmpl, child))
        return out
    resolved: list[Any] = []
    for item in block.items:
        if isinstance(item, str):
            resolved.append(render_template(item, context))
        elif isinstance(item, dict):
            node = dict(item)
            if "title" in node:
                node["title"] = render_template(str(node["title"]), context)
            if "text" in node:
                node["text"] = render_template(str(node["text"]), context)
            resolved.append(node)
        else:
            resolved.append(item)
    return resolved


def _resolve_blocks(
    blocks: list[dict[str, Any]],
    context: dict[str, Any],
    data: Any,
    styles: dict[str, dict[str, Any]],
    *,
    strict: bool,
) -> list[dict[str, Any]]:
    out: list[dict[str, Any]] = []
    for raw in blocks:
        btype = raw.get("type") or ("table" if _is_table_config(raw) else None)
        hide = raw.get("hide_when")
        if hide and eval_condition(hide, context):
            continue
        if btype == "heading":
            block = HeadingBlock.model_validate(raw)
            out.append({
                "type": "heading",
                "level": max(1, min(6, block.level)),
                "text": render_template(block.value, context),
                "css_class": block.css_class,
            })
        elif btype == "paragraph":
            block = ParagraphBlock.model_validate(raw)
            out.append({
                "type": "paragraph",
                "text": render_template(block.value, context),
                "css_class": block.css_class,
            })
        elif btype == "list":
            block = ListBlock.model_validate(raw)
            out.append({
                "type": "list",
                "ordered": block.ordered,
                "items": _resolve_list_items(block, context),
                "css_class": block.css_class,
            })
        elif btype == "code":
            block = CodeBlock.model_validate(raw)
            src = block.code if block.code is not None else block.value
            out.append({
                "type": "code",
                "language": block.language,
                "code": render_template(src, context, escape=False),
                "css_class": block.css_class,
            })
        elif btype == "hr":
            block = HrBlock.model_validate(raw)
            out.append({"type": "hr", "css_class": block.css_class})
        elif btype == "html":
            block = HtmlBlock.model_validate(raw)
            out.append({
                "type": "html",
                "body": render_template(block.value, context, escape=not block.raw),
                "css_class": block.css_class,
            })
        elif btype == "section":
            block = SectionBlock.model_validate(raw)
            nested_ctx = context
            out.append({
                "type": "section",
                "title": render_template(block.title, context) if block.title else None,
                "css_class": block.css_class,
                "blocks": _resolve_blocks(block.blocks, nested_ctx, data, styles, strict=strict),
            })
        elif btype == "table" or _is_table_config(raw):
            table_raw = dict(raw)
            table_raw.pop("type", None)
            if styles and "styles" not in table_raw:
                table_raw["styles"] = styles
            table_raw.setdefault("strict", strict)
            table = _coerce_table(table_raw)
            rendered = TableResolver().resolve_table(table, data)
            out.append({"type": "table", "table": rendered})
        else:
            fail_or_warn("Unknown document block type %r", btype)
    return out


def resolve(
    config: HtmlTableConfig | DocumentConfig | dict[str, Any] | str,
    data: Any,
    *,
    strict: bool | None = None,
) -> ResolvedDocument:
    """Evaluate templates / repeat_for into a format-neutral document IR."""

    if isinstance(config, str):
        import json
        config = json.loads(config)

    if isinstance(config, HtmlTableConfig) or (
        isinstance(config, dict) and _is_table_config(config) and "blocks" not in config
    ):
        table = _coerce_table(config)
        is_strict = table.strict if strict is None else strict
        token = set_strict(is_strict)
        try:
            rendered = TableResolver().resolve_table(table, data)
            title = rendered.title
            rendered.title = None
            return ResolvedDocument(name=title, blocks=[{"type": "table", "table": rendered}])
        finally:
            reset_strict(token)

    doc = _coerce_document(config) if not isinstance(config, DocumentConfig) else config
    is_strict = doc.strict if strict is None else strict
    token = set_strict(is_strict)
    try:
        table_id = doc.id or _new_table_id()
        context = _build_context(data, table_id)
        return ResolvedDocument(
            name=render_template(doc.name or doc.title, context) or None,
            tagline=render_template(doc.tagline, context) or None,
            description=render_template(doc.description, context) or None,
            blocks=_resolve_blocks(doc.blocks, context, data, doc.styles, strict=is_strict),
        )
    finally:
        reset_strict(token)


def _html_emit():
    """Load sibling html.py without binding the name ``html`` (stdlib clash)."""

    name = "lyo_docs_html_emit"
    mod = sys.modules.get(name)
    if mod is not None:
        return mod
    import importlib.util

    path = Path(__file__).with_name("html.py")
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise ImportError(f"Cannot load {path}")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def build(
    config: HtmlTableConfig | DocumentConfig | dict[str, Any] | str,
    data: Any,
    *,
    format: str = "html",
    strict: bool | None = None,
) -> str:
    """Resolve config + data, then emit HTML or Markdown."""

    return _html_emit().build(config, data, format=format, strict=strict)


def build_table(
    config: HtmlTableConfig | dict[str, Any] | str,
    data: Any,
    *,
    strict: bool | None = None,
    format: str = "html",
) -> str:
    """Resolve a table config and emit HTML or Markdown."""

    return build(config, data, format=format, strict=strict)
