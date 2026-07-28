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
    """

    status: int
    ok: bool
    headers: dict[str, str] = field(default_factory=dict)
    data: Any = None
    raw_body: str | None = None
