from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse, PatchRequest, TransportRequest
from lyo_auth_api_client import AuthApiClient, CreateTokenRequest, login_url


class _ScriptedTransport:
    def __init__(self, handler) -> None:
        self.handler = handler
        self.requests: list[TransportRequest] = []

    def __call__(self, request: TransportRequest) -> ApiResponse:
        self.requests.append(request)
        return self.handler(request)


def test_exchange_handoff_posts_code_and_origin():
    seen: list[TransportRequest] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        return ApiResponse(
            status=200,
            ok=True,
            data={"access_token": "jwt", "refresh_token": "ref", "expires_in": 60, "token_type": "Bearer"},
        )

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    result = auth.exchange_handoff("lyoh_abc", "http://127.0.0.1:8734")
    assert result.data["access_token"] == "jwt"
    assert len(seen) == 1
    assert seen[0].method == "POST"
    assert seen[0].url == "http://x/auth/handoff/exchange"
    assert seen[0].headers["Origin"] == "http://127.0.0.1:8734"
    assert json.loads(seen[0].body) == {"code": "lyoh_abc"}


def test_logout_posts_refresh_token_when_given():
    seen: list[TransportRequest] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        return ApiResponse(status=204, ok=True, data=None)

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    auth.logout({"refresh_token": "ref"})
    assert seen[0].method == "POST"
    assert seen[0].url == "http://x/auth/logout"
    assert json.loads(seen[0].body) == {"refresh_token": "ref"}


def test_me_gets_auth_me():
    seen: list[tuple[str, str]] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append((request.method, request.url))
        return ApiResponse(status=200, ok=True, data={"id": "u1", "displayName": "Ada"})

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    auth.me()
    assert seen == [("GET", "http://x/auth/me")]


def test_login_url_is_provider_redirect():
    assert login_url("google", "/home") == "/auth/login/google?returnUrl=%2Fhome"


def test_create_token_returns_secret_once():
    def handler(request: TransportRequest) -> ApiResponse:
        if request.method == "POST":
            assert json.loads(request.body)["displayName"] == "cli"
            return ApiResponse(
                status=200,
                ok=True,
                data={"plaintext": "s3cret", "record": {"id": "t1", "displayName": "cli"}},
            )
        return ApiResponse(
            status=200,
            ok=True,
            data=[{"id": "t1", "displayName": "cli", "secret": "s3cret", "secretHash": "hash", "plaintext": "s3cret"}],
        )

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    created = auth.tokens.create(CreateTokenRequest(display_name="cli", scopes=["auth.tokens.read"]))
    assert created.data["plaintext"] == "s3cret"
    listed = auth.tokens.list()
    assert listed.data[0] == {"id": "t1", "displayName": "cli"}


def test_admin_user_query_uses_auth_user_route():
    seen: list[tuple[str, str]] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append((request.method, request.url))
        return ApiResponse(status=200, ok=True, data={"items": [], "isSuccess": True})

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    auth.admin.users.query_project({"Amount": 10})
    assert seen == [("POST", "http://x/Auth/User/QueryProject")]


def test_session_token_and_admin_routes():
    seen: list[tuple[str, str]] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append((request.method, request.url))
        return ApiResponse(status=200, ok=True, data={})

    auth = AuthApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    auth.user("ada/1")
    auth.logout()
    auth.refresh()
    auth.session()
    auth.tokens.revoke("t 1")
    auth.tokens.kinds()
    auth.admin.users.patch(PatchRequest(properties={"displayName": "Ada"}))
    auth.admin.tokens.delete("tok-1")
    auth.admin.claims.create({"type": "role"})
    auth.admin.scopes.get_by_id("scope-1")
    auth.admin.identities.query_project({})
    auth.admin.events.get_by_id("evt-1")
    assert seen == [
        ("GET", "http://x/auth/users/ada%2F1"),
        ("POST", "http://x/auth/logout"),
        ("POST", "http://x/auth/refresh"),
        ("GET", "http://x/auth/me"),
        ("DELETE", "http://x/tokens/t%201"),
        ("GET", "http://x/tokens/kinds"),
        ("PATCH", "http://x/Auth/User"),
        ("DELETE", "http://x/Auth/Token/tok-1"),
        ("POST", "http://x/Auth/Claim"),
        ("GET", "http://x/Auth/Scope/scope-1"),
        ("POST", "http://x/Auth/LinkedIdentity/QueryProject"),
        ("GET", "http://x/Auth/Event/evt-1"),
    ]
