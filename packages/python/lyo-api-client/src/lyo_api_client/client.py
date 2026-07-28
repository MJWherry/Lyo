"""Core, reusable API client foundation for Lyo Python consumers.

This module intentionally has no domain-specific endpoints; see
lyo_person_api_client for the Person API surface built on top of it.
"""

from __future__ import annotations

import json
from typing import Any, Mapping
from urllib.parse import urlencode

from .errors import to_api_client_error
from .models import ApiRequest, ApiResponse, TransportRequest
from .transport import Transport, UrllibTransport


def build_url(
    base_url: str,
    path: str,
    query: Mapping[str, str | int | float | bool | None] | None = None,
) -> str:
    """Join base URL and path, appending non-None query parameters."""
    normalized_base = base_url.rstrip("/")
    normalized_path = path if path.startswith("/") else f"/{path}"
    url = f"{normalized_base}{normalized_path}"

    if not query:
        return url

    params = [(key, _query_str(value)) for key, value in query.items() if value is not None]
    qs = urlencode(params)
    return f"{url}?{qs}" if qs else url


def _query_str(value: str | int | float | bool) -> str:
    # Match the TS client's String(value): booleans as "true"/"false".
    if isinstance(value, bool):
        return "true" if value else "false"
    return str(value)


def with_bearer_token(headers: dict[str, str], token: str | None = None) -> dict[str, str]:
    """Return a copy of ``headers`` with an Authorization bearer header when a token is given."""
    if not token:
        return headers
    return {**headers, "Authorization": f"Bearer {token}"}


def _serialize_body(body: Any) -> str:
    if hasattr(body, "to_dict"):
        body = body.to_dict()
    return json.dumps(body)


class ApiClient:
    """Transport-agnostic Lyo API client.

    Works out of the box against a running API:

        client = ApiClient("http://localhost:5251", token="optional-token")
        response = client.request(ApiRequest(method="GET", path="/health"))

    Raises ApiClientError for non-2xx responses.
    """

    def __init__(
        self,
        base_url: str,
        *,
        token: str | None = None,
        default_headers: Mapping[str, str] | None = None,
        transport: Transport | None = None,
    ) -> None:
        self.base_url = base_url
        self.token = token
        self.default_headers = dict(default_headers or {})
        self.transport: Transport = transport if transport is not None else UrllibTransport()

    def request(self, request: ApiRequest) -> ApiResponse:
        """Execute an API request, returning the normalized response or raising ApiClientError."""
        url = build_url(self.base_url, request.path, request.query)
        headers = with_bearer_token(
            {
                "Content-Type": "application/json",
                **self.default_headers,
                **(request.headers or {}),
            },
            self.token,
        )

        body = None if request.body is None else _serialize_body(request.body)

        response = self.transport(TransportRequest(method=request.method, url=url, headers=headers, body=body))

        if not response.ok:
            raise to_api_client_error(response.status, response.data if response.data is not None else response.raw_body)

        return response
