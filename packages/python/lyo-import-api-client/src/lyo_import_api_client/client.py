"""Sync Import API client built on ``lyo_api_client``."""

from __future__ import annotations

from typing import Any

from lyo_api_client import ApiClient, ApiRequest, ApiResponse

from .models import ImportRequest


class ImportApiClient:
    """Posts an import document. ``path`` defaults to ``/Import``."""

    def __init__(self, api_client: ApiClient, *, path: str = "/Import") -> None:
        self._api = api_client
        self.path = path

    def post_import(self, body: ImportRequest | dict[str, Any]) -> ApiResponse:
        """``POST`` the import body to ``path``."""
        return self._api.request(ApiRequest(method="POST", path=self.path, body=body))
