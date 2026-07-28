"""Transport abstraction plus the default stdlib implementation.

The client works out of the box via UrllibTransport (zero dependencies). To
use httpx/requests instead, pass any callable matching the Transport protocol
to ApiClient(transport=...).
"""

from __future__ import annotations

import json
import urllib.error
import urllib.request
from typing import Protocol

from .models import ApiResponse, TransportRequest


class Transport(Protocol):
    """Executes a resolved HTTP request and returns a normalized response.

    Implementations must not raise on non-2xx statuses; they report them via
    ``ApiResponse.ok`` so the client can normalize the error.
    """

    def __call__(self, request: TransportRequest) -> ApiResponse: ...


def _parse_body(raw: str) -> object:
    if not raw:
        return None
    try:
        return json.loads(raw)
    except ValueError:
        return None


class UrllibTransport:
    """Default synchronous transport built on urllib.request (stdlib only)."""

    def __init__(self, timeout: float = 30.0) -> None:
        self.timeout = timeout

    def __call__(self, request: TransportRequest) -> ApiResponse:
        req = urllib.request.Request(
            request.url,
            data=request.body.encode("utf-8") if request.body is not None else None,
            headers=request.headers,
            method=request.method,
        )
        try:
            with urllib.request.urlopen(req, timeout=self.timeout) as res:
                raw = res.read().decode("utf-8", errors="replace")
                return ApiResponse(
                    status=res.status,
                    ok=200 <= res.status < 300,
                    headers=dict(res.headers.items()),
                    data=_parse_body(raw),
                    raw_body=raw,
                )
        except urllib.error.HTTPError as err:
            raw = err.read().decode("utf-8", errors="replace")
            return ApiResponse(
                status=err.code,
                ok=False,
                headers=dict(err.headers.items()) if err.headers else {},
                data=_parse_body(raw),
                raw_body=raw,
            )
