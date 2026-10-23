"""Reporting request models. Snake_case attributes; camelCase ``to_dict()``."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is None:
        return
    out[key] = value.to_dict() if hasattr(value, "to_dict") else value


@dataclass
class ReportGenerationParameterReq:
    key: str
    type: str | None = None
    value: str | None = None
    description: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"key": self.key}
        _put(out, "type", self.type)
        _put(out, "value", self.value)
        _put(out, "description", self.description)
        return out


@dataclass
class GenerateReportReq:
    report_definition_id: str | None = None
    report_data_json: str | None = None
    override_report_data_json: str | None = None
    format: str | None = None
    parameters: list[ReportGenerationParameterReq] = field(default_factory=list)
    file_name: str | None = None
    path_prefix: str | None = None
    created_by: str | None = None
    include_report_data: bool = False

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "reportDefinitionId", self.report_definition_id)
        _put(out, "reportDataJson", self.report_data_json)
        _put(out, "overrideReportDataJson", self.override_report_data_json)
        _put(out, "format", self.format)
        if self.parameters:
            out["parameters"] = [p.to_dict() for p in self.parameters]
        _put(out, "fileName", self.file_name)
        _put(out, "pathPrefix", self.path_prefix)
        _put(out, "createdBy", self.created_by)
        if self.include_report_data:
            out["includeReportData"] = True
        return out


@dataclass
class ReportDefinitionReq:
    name: str
    report_data_json: str
    description: str | None = None
    tags: str | None = None
    is_active: bool = True
    default_format: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "name": self.name,
            "reportDataJson": self.report_data_json,
            "isActive": self.is_active,
        }
        _put(out, "description", self.description)
        _put(out, "tags", self.tags)
        _put(out, "defaultFormat", self.default_format)
        return out


@dataclass
class ReportDefinitionParameterReq:
    report_definition_id: str
    key: str
    type: str | None = None
    value: str | None = None
    required: bool = False

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "reportDefinitionId": self.report_definition_id,
            "key": self.key,
            "required": self.required,
        }
        _put(out, "type", self.type)
        _put(out, "value", self.value)
        return out
