#!/usr/bin/env python3
"""Fluent builder for document/table configs. Dict inline styles stay dicts."""

from __future__ import annotations

import sys
from pathlib import Path
from typing import Any

_DOCS = Path(__file__).resolve().parent
_SCRIPTS = _DOCS.parent
for _p in (str(_SCRIPTS), str(_DOCS)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from document import (  # noqa: E402
    ColumnConfig,
    DocumentConfig,
    HtmlTableConfig,
    RowConfig,
    StyleRule,
)


def _coerce_style_rule(rule: Any) -> dict[str, Any]:
    if isinstance(rule, StyleRule):
        return rule.model_dump()
    if isinstance(rule, dict):
        return rule
    if isinstance(rule, (tuple, list)) and len(rule) >= 2:
        when, rest = rule[0], rule[1]
        if isinstance(rest, str):
            return {"when": when, "style_name": rest}
        return {"when": when, "style": rest}
    raise TypeError(f"Cannot coerce style rule from {type(rule).__name__}")


class TableBuilder:
    """Table-scoped fluent builder. ``build(data, format=...)`` renders."""

    def __init__(self, parent: HtmlBuilder | None = None, **table_kw: Any) -> None:
        self._parent = parent
        self._kw = dict(table_kw)
        styles = self._kw.pop("styles", None)
        self._styles: dict[str, dict[str, Any]] = dict(styles or {})
        if parent is not None:
            for name, css in parent._styles.items():
                self._styles.setdefault(name, css)
        self._headers: list[dict[str, Any]] = []
        self._rows: list[dict[str, Any]] = []
        self._footers: list[dict[str, Any]] = []
        self._columns: list[dict[str, Any]] = []
        self._current: dict[str, Any] | None = None
        self._current_target = self._rows

    def style(self, name: str, style: dict[str, Any] | None = None, **css: Any) -> TableBuilder:
        merged = dict(style or {})
        merged.update(css)
        self._styles[name] = merged
        if self._parent is not None:
            self._parent._styles[name] = merged
        return self

    def column(self, **kw: Any) -> TableBuilder:
        self._columns.append(kw)
        return self

    def header_row(self, **kw: Any) -> TableBuilder:
        self._flush_row()
        self._current = {"cells": [], **kw}
        self._current_target = self._headers
        return self

    def body_row(self, **kw: Any) -> TableBuilder:
        self._flush_row()
        self._current = {"cells": [], **kw}
        self._current_target = self._rows
        return self

    def footer_row(self, **kw: Any) -> TableBuilder:
        self._flush_row()
        self._current = {"cells": [], **kw}
        self._current_target = self._footers
        return self

    def cell(self, value: str = "", **kw: Any) -> TableBuilder:
        if self._current is None:
            self.body_row()
        if "style_rules" in kw and kw["style_rules"] is not None:
            kw["style_rules"] = [_coerce_style_rule(r) for r in kw["style_rules"]]
        self._current["cells"].append({"value": value, **kw})
        return self

    def end_row(self) -> TableBuilder:
        self._flush_row()
        return self

    def end_table(self) -> HtmlBuilder:
        self._flush_row()
        if self._parent is None:
            raise RuntimeError("end_table() requires HtmlBuilder.table()")
        self._parent._adopt_table(self)
        return self._parent

    def _flush_row(self) -> None:
        if self._current is not None:
            self._current_target.append(self._current)
            self._current = None

    def to_config(self) -> HtmlTableConfig:
        self._flush_row()
        payload = dict(self._kw)
        payload["styles"] = self._styles
        if self._columns:
            payload["columns"] = [ColumnConfig.model_validate(c) for c in self._columns]
        payload["headers"] = [RowConfig.model_validate(r) for r in self._headers]
        payload["rows"] = [RowConfig.model_validate(r) for r in self._rows]
        payload["footers"] = [RowConfig.model_validate(r) for r in self._footers]
        return HtmlTableConfig.model_validate(payload)

    def build(self, data: Any, format: str = "html", *, strict: bool | None = None) -> str:
        if self._parent is not None and (self._parent._blocks or self._parent._name or self._parent._tagline):
            self._parent._adopt_table(self)
            return self._parent.build(data, format=format, strict=strict)
        from html import build as emit

        return emit(self.to_config(), data, format=format, strict=strict)


class HtmlBuilder:
    """Document (and table) fluent builder."""

    def __init__(self) -> None:
        self._styles: dict[str, dict[str, Any]] = {}
        self._blocks: list[dict[str, Any]] = []
        self._name: str | None = None
        self._tagline: str | None = None
        self._description: str | None = None
        self._id: str | None = None
        self._strict: bool = False
        self._section_stack: list[list[dict[str, Any]]] = []
        self._table: TableBuilder | None = None

    def _target(self) -> list[dict[str, Any]]:
        return self._section_stack[-1] if self._section_stack else self._blocks

    def _adopt_table(self, table: TableBuilder) -> None:
        cfg = table.to_config()
        raw = cfg.model_dump(exclude_none=True)
        raw["type"] = "table"
        self._target().append(raw)
        if self._table is table:
            self._table = None

    def name(self, value: str) -> HtmlBuilder:
        self._name = value
        return self

    def title(self, value: str) -> HtmlBuilder:
        return self.name(value)

    def tagline(self, value: str) -> HtmlBuilder:
        self._tagline = value
        return self

    def description(self, value: str) -> HtmlBuilder:
        self._description = value
        return self

    def doc_id(self, value: str) -> HtmlBuilder:
        self._id = value
        return self

    def strict(self, value: bool = True) -> HtmlBuilder:
        self._strict = value
        return self

    def style(self, name: str, style: dict[str, Any] | None = None, **css: Any) -> HtmlBuilder:
        merged = dict(style or {})
        merged.update(css)
        self._styles[name] = merged
        return self

    def table(self, **kw: Any) -> TableBuilder:
        if self._table is not None:
            self._adopt_table(self._table)
        self._table = TableBuilder(self, **kw)
        return self._table

    def section(self, title: str | None = None, **kw: Any) -> HtmlBuilder:
        block: dict[str, Any] = {"type": "section", "title": title, "blocks": []}
        block.update(kw)
        self._target().append(block)
        self._section_stack.append(block["blocks"])
        return self

    def end_section(self) -> HtmlBuilder:
        if self._section_stack:
            self._section_stack.pop()
        return self

    def heading(self, value: str, level: int = 1, **kw: Any) -> HtmlBuilder:
        self._target().append({"type": "heading", "level": level, "value": value, **kw})
        return self

    def paragraph(self, value: str, **kw: Any) -> HtmlBuilder:
        self._target().append({"type": "paragraph", "value": value, **kw})
        return self

    def hr(self, **kw: Any) -> HtmlBuilder:
        self._target().append({"type": "hr", **kw})
        return self

    def code(self, value: str, language: str = "text", **kw: Any) -> HtmlBuilder:
        self._target().append({"type": "code", "value": value, "language": language, **kw})
        return self

    def html(self, value: str, raw: bool = True, **kw: Any) -> HtmlBuilder:
        self._target().append({"type": "html", "value": value, "raw": raw, **kw})
        return self

    def ul(
        self,
        items: list[Any] | None = None,
        *,
        repeat_for: str | None = None,
        item: str | None = None,
        **kw: Any,
    ) -> HtmlBuilder:
        block: dict[str, Any] = {"type": "list", "ordered": False, "items": items or []}
        if repeat_for:
            block["repeat_for"] = repeat_for
        if item is not None:
            block["item"] = item
        block.update(kw)
        self._target().append(block)
        return self

    def ol(
        self,
        items: list[Any] | None = None,
        *,
        repeat_for: str | None = None,
        item: str | None = None,
        **kw: Any,
    ) -> HtmlBuilder:
        block: dict[str, Any] = {"type": "list", "ordered": True, "items": items or []}
        if repeat_for:
            block["repeat_for"] = repeat_for
        if item is not None:
            block["item"] = item
        block.update(kw)
        self._target().append(block)
        return self

    def to_config(self) -> DocumentConfig | HtmlTableConfig:
        if self._table is not None and not self._blocks and not self._name and not self._tagline and not self._description:
            cfg = self._table.to_config()
            if self._styles:
                merged = dict(self._styles)
                merged.update(cfg.styles)
                cfg = cfg.model_copy(update={"styles": merged})
            return cfg
        if self._table is not None:
            self._adopt_table(self._table)
        return DocumentConfig(
            id=self._id,
            name=self._name,
            tagline=self._tagline,
            description=self._description,
            styles=self._styles,
            blocks=list(self._blocks),
            strict=self._strict,
        )

    def build(self, data: Any, format: str = "html", *, strict: bool | None = None) -> str:
        from html import build as emit

        return emit(self.to_config(), data, format=format, strict=strict)


__all__ = ["HtmlBuilder", "TableBuilder"]
