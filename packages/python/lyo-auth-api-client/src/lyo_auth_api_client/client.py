"""Sync Auth API client built on ``lyo_api_client``."""

from __future__ import annotations

from typing import Any
from urllib.parse import quote

from lyo_api_client import ApiClient, ApiRequest, ApiResponse, PatchRequest

from . import routes
from .models import CreateTokenRequest, token_for_list


def _join(prefix: str, path: str) -> str:
    return f"{prefix.rstrip('/')}/{path.lstrip('/')}"


class _Read:
    def __init__(self, api: ApiClient, route: str) -> None:
        self._api = api
        self.route = route

    def query_project(self, body: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryProject", body=body))

    def get_by_id(self, entity_id: str) -> ApiResponse:
        return self._api.get_by_id(self.route, entity_id)


class _Users(_Read):
    def patch(self, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        return self._api.patch(self.route, body)


class _AdminTokens(_Read):
    def patch(self, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        return self._api.patch(self.route, body)

    def delete(self, entity_id: str) -> ApiResponse:
        encoded = quote(entity_id, safe="")
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{encoded}"))


class _Crud(_Read):
    def create(self, body: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self.route, body=body))

    def patch(self, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        return self._api.patch(self.route, body)

    def delete(self, entity_id: str) -> ApiResponse:
        encoded = quote(entity_id, safe="")
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{encoded}"))


class _Tokens:
    def __init__(self, api: ApiClient, prefix: str) -> None:
        self._api = api
        self._prefix = prefix.rstrip("/")

    def list(self, include_revoked: bool = False) -> ApiResponse:
        query = {"includeRevoked": True} if include_revoked else None
        response = self._api.request(ApiRequest(method="GET", path=self._prefix, query=query))
        rows = response.data if isinstance(response.data, list) else []
        response.data = [token_for_list(row) if isinstance(row, dict) else row for row in rows]
        return response

    def create(self, body: CreateTokenRequest | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._prefix, body=body))

    def revoke(self, token_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self._prefix}/{quote(token_id, safe='')}"))

    def kinds(self) -> ApiResponse:
        return self._api.request(ApiRequest(method="GET", path=f"{self._prefix}/kinds"))


class _Admin:
    def __init__(self, api: ApiClient) -> None:
        self.users = _Users(api, routes.USER)
        self.tokens = _AdminTokens(api, routes.TOKEN)
        self.claims = _Crud(api, routes.CLAIM)
        self.scopes = _Crud(api, routes.SCOPE)
        self.identities = _Read(api, routes.LINKED_IDENTITY)
        self.events = _Read(api, routes.EVENT)


class AuthApiClient:
    """Session, personal tokens, and admin Auth resources.

    ``session_prefix`` defaults to ``/auth``. ``token_prefix`` defaults to ``/tokens``.
    """

    def __init__(
        self,
        api_client: ApiClient,
        *,
        session_prefix: str = "/auth",
        token_prefix: str = "/tokens",
    ) -> None:
        self._api = api_client
        self._session = session_prefix
        self.tokens = _Tokens(api_client, token_prefix)
        self.admin = _Admin(api_client)

    def me(self) -> ApiResponse:
        """``GET /auth/me``."""
        return self._api.request(ApiRequest(method="GET", path=_join(self._session, "me")))

    def user(self, user_id: str) -> ApiResponse:
        """``GET /auth/users/{id}``."""
        return self._api.request(ApiRequest(method="GET", path=_join(self._session, f"users/{quote(user_id, safe='')}")))

    def exchange_handoff(self, code: str, origin: str) -> ApiResponse:
        """``POST /auth/handoff/exchange``. ``origin`` must match the allow-listed return origin, including port."""
        return self._api.request(
            ApiRequest(
                method="POST",
                path=_join(self._session, "handoff/exchange"),
                body={"code": code},
                headers={"Origin": origin},
            )
        )

    def logout(self, body: Any = None) -> ApiResponse:
        """``POST /auth/logout``. Pass ``{"refresh_token": ...}`` to revoke that refresh token."""
        return self._api.request(ApiRequest(method="POST", path=_join(self._session, "logout"), body=body))

    def refresh(self, body: Any = None) -> ApiResponse:
        """``POST /auth/refresh``. Pass ``{"refresh_token": ...}`` to rotate the session."""
        return self._api.request(ApiRequest(method="POST", path=_join(self._session, "refresh"), body=body))

    def session(self) -> ApiResponse:
        """``GET /auth/me``."""
        return self.me()
