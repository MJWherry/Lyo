"""Import preview helpers. Field maps stay in memory until the host posts them."""

from __future__ import annotations

from collections.abc import Callable, Sequence
from dataclasses import dataclass
from typing import Any

PREVIEW_CAP = 50


@dataclass
class ImportFieldMap:
    source: str
    target: str
    required: bool | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"source": self.source, "target": self.target}
        if self.required is not None:
            out["required"] = self.required
        return out


@dataclass
class ImportIssue:
    index: int
    message: str


@dataclass
class ImportPreviewState:
    fields: list[ImportFieldMap]
    blocked_reason: str | None
    preview: list[str]
    issues: list[ImportIssue]


@dataclass
class ImportPreviewShown:
    preview: list[str]
    issues: list[ImportIssue]


def empty_preview() -> ImportPreviewState:
    """A preview with no fields, rows, or issues."""
    return ImportPreviewState(fields=[], blocked_reason=None, preview=[], issues=[])


def try_add_field(state: ImportPreviewState, field: ImportFieldMap) -> ImportPreviewState:
    """A second source aimed at the same target is not stored."""
    if any(existing.target == field.target for existing in state.fields):
        return ImportPreviewState(
            fields=list(state.fields),
            blocked_reason=f"Two sources map to '{field.target}'.",
            preview=list(state.preview),
            issues=list(state.issues),
        )
    return ImportPreviewState(
        fields=[*state.fields, field],
        blocked_reason=None,
        preview=list(state.preview),
        issues=list(state.issues),
    )


def show_preview(
    rows: Sequence[Any],
    issues: Sequence[ImportIssue],
    label: Callable[[Any], str],
) -> ImportPreviewShown:
    """Preview shows at most 50 good rows. Every issue is kept."""
    return ImportPreviewShown(
        preview=[label(row) for row in list(rows)[:PREVIEW_CAP]],
        issues=list(issues),
    )
