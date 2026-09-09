"""Generic request/response types for the Lyo API client."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Literal, Mapping

HttpMethod = Literal["GET", "POST", "PUT", "PATCH", "DELETE"]


@dataclass
class ApiRequest:
    """A single API call description, independent of transport.

    ``body`` may be any JSON-serializable value, or an object exposing a
    ``to_dict()`` method (all lyo_person_api_client request models do).
    """

    method: HttpMethod
    path: str
    body: Any = None
    headers: dict[str, str] | None = None
    query: Mapping[str, str | int | float | bool | None] | None = None


@dataclass
class TransportRequest:
    """The fully resolved HTTP request handed to a Transport."""

    method: HttpMethod
    url: str
    headers: dict[str, str]
    body: str | None = None


@dataclass
class ApiResponse:
    """Normalized API response.

    ``data`` holds the parsed JSON payload when the response body was JSON;
    ``raw_body`` always holds the raw response text (when available).
    ``body_bytes`` holds the raw bytes when the transport returns binary (downloads).
    """

    status: int
    ok: bool
    headers: dict[str, str] = field(default_factory=dict)
    data: Any = None
    raw_body: str | None = None
    body_bytes: bytes | None = None


@dataclass
class PatchRequest:
    """HTTP PATCH body (``Lyo.Api.Models.Common.Request.PatchRequest``). CamelCase wire JSON."""

    properties: dict[str, Any] = field(default_factory=dict)
    keys: list[list[Any]] | None = None
    query: Any = None
    allow_multiple: bool = False

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"properties": self.properties}
        if self.keys is not None:
            out["keys"] = self.keys
        if self.query is not None:
            out["query"] = self.query.to_dict() if hasattr(self.query, "to_dict") else self.query
        if self.allow_multiple:
            out["allowMultiple"] = True
        return out


@dataclass
class ParameterOptionsItem:
    key: str = ""
    label: str = ""

    def to_dict(self) -> dict[str, Any]:
        return {"key": self.key, "label": self.label}


@dataclass
class ParameterOptionsResolveReq:
    options_json: str | None = None
    sibling_values: dict[str, str | None] | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        if self.options_json is not None:
            out["optionsJson"] = self.options_json
        if self.sibling_values is not None:
            out["siblingValues"] = self.sibling_values
        return out
