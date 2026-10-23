"""Sync Config API client built on ``lyo_api_client``."""

from __future__ import annotations

from typing import Any, Literal
from urllib.parse import quote

from lyo_api_client import ApiClient, ApiClientError, ApiRequest, ApiResponse, HttpMethod

from .models import ConfigBindingRecord, ConfigDefinitionRecord, ConfigResolveResult
from .routes import config_prefix

ResolveMethod = Literal["GET", "HEAD", "POST"]


def _etag(headers: dict[str, str]) -> str | None:
    for key, value in headers.items():
        if key.lower() == "etag":
            return value
    return None


class ConfigApiClient:
    """Resolve and manage config definitions and bindings.

    ``api_key`` is sent as ``X-Api-Key`` when the host requires it.
    ``prefix`` defaults to ``/api/config``.
    """

    def __init__(self, api_client: ApiClient, *, api_key: str | None = None, prefix: str = "/api/config") -> None:
        self._api = api_client
        self._api_key = api_key
        self.prefix = config_prefix(prefix)
        self._manage = f"{self.prefix}/manage"

    def resolve(
        self,
        app_kind: str,
        app_id: str,
        *,
        if_none_match: str | None = None,
        version: str | None = None,
        method: ResolveMethod = "GET",
    ) -> ConfigResolveResult:
        """``GET``, ``HEAD``, or ``POST {prefix}/{appKind}/{appId}``. ``304`` is not an error."""
        extra: dict[str, str] = {}
        if if_none_match:
            extra["If-None-Match"] = if_none_match
        path = f"{self.prefix}/{quote(app_kind, safe='')}/{quote(app_id, safe='')}"
        query = {"version": version} if version else None
        body: dict[str, Any] | None = {} if method == "POST" else None
        try:
            response = self._call(method, path, body=body, query=query, extra=extra)
        except ApiClientError as err:
            if err.status == 304:
                return ConfigResolveResult(outcome=304)
            raise
        if response.status == 304:
            return ConfigResolveResult(outcome=304)
        return ConfigResolveResult(outcome=200, record=response.data, etag=_etag(response.headers))

    def list_definitions(self, subject_entity_type: str | None = None) -> ApiResponse:
        return self._call("GET", f"{self._manage}/definitions", query={"subjectEntityType": subject_entity_type})

    def get_definition_by_key(self, subject_entity_type: str, key: str) -> ApiResponse:
        return self._call(
            "GET",
            f"{self._manage}/definitions/by-key",
            query={"subjectEntityType": subject_entity_type, "key": key},
        )

    def get_definition(self, definition_id: str) -> ApiResponse:
        return self._call("GET", f"{self._manage}/definitions/{quote(definition_id, safe='')}")

    def put_definition(self, body: ConfigDefinitionRecord | dict[str, Any]) -> ApiResponse:
        return self._call("PUT", f"{self._manage}/definitions", body=body)

    def definition_revisions(self, definition_id: str) -> ApiResponse:
        return self._call("GET", f"{self._manage}/definitions/{quote(definition_id, safe='')}/revisions")

    def get_definition_revision(self, definition_id: str, revision: int) -> ApiResponse:
        encoded = quote(definition_id, safe="")
        return self._call("GET", f"{self._manage}/definitions/{encoded}/revisions/{revision}")

    def revert_definition(self, definition_id: str, revision: int) -> ApiResponse:
        encoded = quote(definition_id, safe="")
        return self._call("POST", f"{self._manage}/definitions/{encoded}/revert", body={"revision": revision})

    def delete_definition(self, definition_id: str) -> ApiResponse:
        return self._call("DELETE", f"{self._manage}/definitions/{quote(definition_id, safe='')}")

    def list_bindings(self, subject_entity_type: str, subject_entity_id: str) -> ApiResponse:
        return self._call(
            "GET",
            f"{self._manage}/bindings",
            query={"subjectEntityType": subject_entity_type, "subjectEntityId": subject_entity_id},
        )

    def get_binding_by_key(self, subject_entity_type: str, subject_entity_id: str, key: str) -> ApiResponse:
        return self._call(
            "GET",
            f"{self._manage}/bindings/by-key",
            query={"subjectEntityType": subject_entity_type, "subjectEntityId": subject_entity_id, "key": key},
        )

    def get_binding(self, binding_id: str) -> ApiResponse:
        return self._call("GET", f"{self._manage}/bindings/{quote(binding_id, safe='')}")

    def put_binding(self, body: ConfigBindingRecord | dict[str, Any]) -> ApiResponse:
        return self._call("PUT", f"{self._manage}/bindings", body=body)

    def delete_bindings(self, subject_entity_type: str, subject_entity_id: str) -> ApiResponse:
        return self._call(
            "DELETE",
            f"{self._manage}/bindings",
            query={"subjectEntityType": subject_entity_type, "subjectEntityId": subject_entity_id},
        )

    def delete_binding(self, binding_id: str) -> ApiResponse:
        return self._call("DELETE", f"{self._manage}/bindings/{quote(binding_id, safe='')}")

    def binding_revisions(self, binding_id: str) -> ApiResponse:
        return self._call("GET", f"{self._manage}/bindings/{quote(binding_id, safe='')}/revisions")

    def get_binding_revision(self, binding_id: str, revision: int) -> ApiResponse:
        encoded = quote(binding_id, safe="")
        return self._call("GET", f"{self._manage}/bindings/{encoded}/revisions/{revision}")

    def revert_binding(self, binding_id: str, revision: int) -> ApiResponse:
        encoded = quote(binding_id, safe="")
        return self._call("POST", f"{self._manage}/bindings/{encoded}/revert", body={"revision": revision})

    def resolved(self, subject_entity_type: str, subject_entity_id: str) -> ApiResponse:
        return self._call(
            "GET",
            f"{self._manage}/resolved",
            query={"subjectEntityType": subject_entity_type, "subjectEntityId": subject_entity_id},
        )

    def _call(
        self,
        method: HttpMethod,
        path: str,
        *,
        body: Any = None,
        query: dict[str, str | int | float | bool | None] | None = None,
        extra: dict[str, str] | None = None,
    ) -> ApiResponse:
        headers = {"X-Api-Key": self._api_key} if self._api_key else None
        if extra:
            headers = {**(headers or {}), **extra}
        return self._api.request(ApiRequest(method=method, path=path, body=body, headers=headers, query=query))
