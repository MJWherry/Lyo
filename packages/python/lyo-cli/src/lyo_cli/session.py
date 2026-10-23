"""Open an ``ApiClient`` from a profile, refreshing an expired access token once."""

from __future__ import annotations

from datetime import datetime, timedelta, timezone
from pathlib import Path

from lyo_api_client import ApiClient
from lyo_api_client.transport import Transport
from lyo_auth_api_client import AuthApiClient

from .profile import Profile, save_profile


def connect(profile: Profile, *, transport: Transport, profile_path: Path, now: datetime | None = None) -> ApiClient:
    """Return a client for ``profile``.

    A ``lyo_`` token is sent as-is. Any other access token is refreshed once when
    ``access_expires_at`` is in the past and ``refresh_token`` is set. The new pair
    is written back to ``profile_path``.
    """
    current = now or datetime.now(timezone.utc)
    token = profile.access_token
    if token and token.startswith("lyo_"):
        return ApiClient(profile.base_url, token=token, transport=transport)
    if profile.refresh_token and _expired(profile.access_expires_at, current):
        auth = AuthApiClient(ApiClient(profile.base_url, token=token, transport=transport))
        data = auth.refresh({"refresh_token": profile.refresh_token}).data or {}
        new_token = data["access_token"]
        new_refresh = data.get("refresh_token") or profile.refresh_token
        save_profile(
            Profile(
                base_url=profile.base_url,
                access_token=new_token,
                refresh_token=new_refresh,
                access_expires_at=_expires_at(data.get("expires_in"), current),
            ),
            profile_path,
        )
        return ApiClient(profile.base_url, token=new_token, transport=transport)
    return ApiClient(profile.base_url, token=token, transport=transport)


def expires_at_from(expires_in: int | None, now: datetime) -> str | None:
    """UTC timestamp ``expires_in`` seconds after ``now``, or ``None`` when the lifetime is missing."""
    return _expires_at(expires_in, now)


def _expires_at(expires_in: object, now: datetime) -> str | None:
    if not isinstance(expires_in, int):
        return None
    moment = now if now.tzinfo else now.replace(tzinfo=timezone.utc)
    return (moment + timedelta(seconds=expires_in)).astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _expired(access_expires_at: str | None, now: datetime) -> bool:
    if not access_expires_at:
        return False
    parsed = datetime.fromisoformat(access_expires_at.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    current = now if now.tzinfo else now.replace(tzinfo=timezone.utc)
    return current >= parsed
