"""Normalized API errors."""

from __future__ import annotations

from typing import Any


class ApiClientError(Exception):
    """Raised when the API returns a non-success status.

    ``status`` is the HTTP status code (when known) and ``details`` the parsed
    error payload (typically an RFC 7807 problem details object).
    """

    def __init__(self, message: str, status: int | None = None, details: Any = None) -> None:
        super().__init__(message)
        self.status = status
        self.details = details


def to_api_client_error(status: int, payload: Any) -> ApiClientError:
    """Build an ApiClientError, surfacing a problem-details ``title`` when present."""
    if isinstance(payload, dict) and isinstance(payload.get("title"), str):
        return ApiClientError(f"{status} {payload['title']}", status, payload)
    return ApiClientError(f"Request failed with status {status}", status, payload)
