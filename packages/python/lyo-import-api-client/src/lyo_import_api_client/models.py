"""Import request models. Snake_case attributes; camelCase ``to_dict()``."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from .preview import ImportFieldMap


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is not None:
        out[key] = value


@dataclass
class ImportRequest:
    """Body of ``POST /Import``."""

    format: str
    json_mode: str | None = None
    rows_path: str | None = None
    sheet_name: str | None = None
    content: str | None = None
    content_bytes: str | None = None
    fields: list[ImportFieldMap] | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict = {"format": self.format}
        _put(out, "jsonMode", self.json_mode)
        _put(out, "rowsPath", self.rows_path)
        _put(out, "sheetName", self.sheet_name)
        _put(out, "content", self.content)
        _put(out, "contentBytes", self.content_bytes)
        if self.fields:
            out["fields"] = [field.to_dict() for field in self.fields]
        return out
