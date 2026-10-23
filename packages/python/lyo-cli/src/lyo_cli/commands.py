"""CLI operations. Each function returns the text the command prints, or writes the profile."""

from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from lyo_api_client import ApiClient
from lyo_api_client.transport import Transport
from lyo_auth_api_client import AuthApiClient

from .profile import Profile, save_profile
from .session import expires_at_from


def query_command(
    client: ApiClient,
    route: str,
    *,
    select: list[str] | None,
    amount: int | None,
    body: dict[str, Any] | None,
) -> str:
    """``POST {route}/QueryProject``. ``select`` and ``amount`` fill the body only when those keys are absent.

    Amount defaults to 20 when both the flag and the body omit it.
    """
    payload = dict(body or {})
    if select and "Select" not in payload:
        payload["Select"] = list(select)
    if "Amount" not in payload:
        payload["Amount"] = 20 if amount is None else amount
    return json.dumps(client.query_project(route, payload).data)


def get_command(client: ApiClient, route: str, entity_id: str, include: list[str] | None = None) -> str:
    """``GET {route}/{id}``."""
    return json.dumps(client.get_by_id(route, entity_id, include).data)


def create_command(client: ApiClient, route: str, body: Any) -> str:
    """``POST {route}``."""
    return json.dumps(client.create(route, body).data)


def patch_command(client: ApiClient, route: str, body: Any) -> str:
    """``PATCH {route}``."""
    return json.dumps(client.patch(route, body).data)


def update_command(client: ApiClient, route: str, entity_id: str, body: Any) -> str:
    """``POST {route}/Update``."""
    return json.dumps(client.update(route, entity_id, body).data)


def delete_command(client: ApiClient, route: str, entity_id: str) -> str:
    """``DELETE {route}/{id}``."""
    return json.dumps(client.delete_by_id(route, entity_id).data)


def metadata_command(client: ApiClient, route: str, entity_type: str | None = None) -> str:
    """``GET {route}/Metadata``, or ``GET {route}/{entity}/Metadata`` when ``entity_type`` is set."""
    if entity_type:
        return json.dumps(client.get_entity_metadata(route, entity_type).data)
    return json.dumps(client.get_metadata(route).data)


def status_text(payload: Any, access_token: str | None) -> str:
    """JSON for the signed-in user. The access token is not included."""
    user = payload.get("user") if isinstance(payload, dict) and "user" in payload else payload
    text = json.dumps(user)
    if access_token and access_token in text:
        text = text.replace(access_token, "")
    return text


def status_command(client: ApiClient) -> str:
    """``GET /auth/me``. Prints the user, not the bearer token."""
    payload = AuthApiClient(client).me().data
    return status_text(payload, client.token)


def save_token(base_url: str, token: str, path: Path) -> Profile:
    """Store a pasted PAT or CLI token. Opaque ``lyo_`` tokens do not refresh."""
    profile = Profile(base_url=base_url, access_token=token)
    save_profile(profile, path)
    return profile


def finish_login(
    base_url: str,
    code: str,
    origin: str,
    path: Path,
    transport: Transport,
    now: datetime | None = None,
) -> Profile:
    """Exchange a handoff code and save the access and refresh tokens."""
    current = now or datetime.now(timezone.utc)
    data = AuthApiClient(ApiClient(base_url, transport=transport)).exchange_handoff(code, origin).data or {}
    profile = Profile(
        base_url=base_url,
        access_token=data.get("access_token"),
        refresh_token=data.get("refresh_token"),
        access_expires_at=expires_at_from(data.get("expires_in"), current),
    )
    save_profile(profile, path)
    return profile


def refresh_profile(profile: Profile, path: Path, transport: Transport, now: datetime | None = None) -> Profile:
    """``POST /auth/refresh`` and save the rotated tokens."""
    if not profile.refresh_token:
        raise SystemExit("No refresh token in the profile.")
    if profile.access_token and profile.access_token.startswith("lyo_"):
        raise SystemExit("This token does not refresh. Paste a new one with lyo auth token.")
    current = now or datetime.now(timezone.utc)
    data = (
        AuthApiClient(ApiClient(profile.base_url, token=profile.access_token, transport=transport))
        .refresh({"refresh_token": profile.refresh_token})
        .data
        or {}
    )
    updated = Profile(
        base_url=profile.base_url,
        access_token=data.get("access_token"),
        refresh_token=data.get("refresh_token") or profile.refresh_token,
        access_expires_at=expires_at_from(data.get("expires_in"), current),
    )
    save_profile(updated, path)
    return updated


def logout_profile(profile: Profile, path: Path, transport: Transport) -> None:
    """``POST /auth/logout`` with the refresh token, then clear stored credentials."""
    body = {"refresh_token": profile.refresh_token} if profile.refresh_token else None
    AuthApiClient(ApiClient(profile.base_url, token=profile.access_token, transport=transport)).logout(body)
    save_profile(Profile(base_url=profile.base_url), path)
