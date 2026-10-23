"""Profile file at ``~/.config/lyo/profile.json`` (mode ``0600``).

Resolution order for the base URL and token is ``--base-url`` / ``--token``, then
``LYO_API_BASE_URL`` / ``LYO_API_TOKEN``, then the file. An explicit token does not
keep the file's refresh token.
"""

from __future__ import annotations

import json
import os
from dataclasses import dataclass
from pathlib import Path
from typing import Mapping


class MissingProfileError(Exception):
    """No base URL was given by a flag, ``LYO_API_BASE_URL``, or the profile file."""


@dataclass
class Profile:
    """Saved API location and credentials. ``access_expires_at`` is UTC ``YYYY-MM-DDTHH:MM:SSZ``."""

    base_url: str
    access_token: str | None = None
    refresh_token: str | None = None
    access_expires_at: str | None = None


def default_profile_path() -> Path:
    """``~/.config/lyo/profile.json``."""
    return Path.home() / ".config" / "lyo" / "profile.json"


def save_profile(profile: Profile, path: Path) -> None:
    """Write ``profile`` as JSON. The file mode is ``0600``."""
    path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "base_url": profile.base_url,
        "access_token": profile.access_token,
        "refresh_token": profile.refresh_token,
        "access_expires_at": profile.access_expires_at,
    }
    encoded = json.dumps(payload, indent=2) + "\n"
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    try:
        os.write(fd, encoded.encode())
    finally:
        os.close(fd)
    os.chmod(path, 0o600)


def load_profile(path: Path) -> Profile:
    """Read a profile file. Raises ``MissingProfileError`` when ``base_url`` is absent."""
    data = json.loads(path.read_text())
    base_url = data.get("base_url")
    if not base_url:
        raise MissingProfileError(f"Profile {path} has no base_url.")
    return Profile(
        base_url=base_url,
        access_token=data.get("access_token"),
        refresh_token=data.get("refresh_token"),
        access_expires_at=data.get("access_expires_at"),
    )


def resolve_profile(
    *,
    base_url: str | None = None,
    token: str | None = None,
    environ: Mapping[str, str] | None = None,
    path: Path | None = None,
) -> Profile:
    """Resolve the base URL and token. Raises ``MissingProfileError`` when no base URL is available."""
    env = os.environ if environ is None else environ
    profile_path = default_profile_path() if path is None else path
    stored = load_profile(profile_path) if profile_path.is_file() else None
    resolved_base = _blank(base_url) or _blank(env.get("LYO_API_BASE_URL")) or (stored.base_url if stored else None)
    if not resolved_base:
        raise MissingProfileError("No base URL. Pass --base-url, set LYO_API_BASE_URL, or run lyo auth login.")
    explicit_token = _blank(token) or _blank(env.get("LYO_API_TOKEN"))
    if explicit_token:
        return Profile(base_url=resolved_base, access_token=explicit_token)
    if stored is None:
        return Profile(base_url=resolved_base)
    return Profile(
        base_url=resolved_base,
        access_token=stored.access_token,
        refresh_token=stored.refresh_token,
        access_expires_at=stored.access_expires_at,
    )


def _blank(value: str | None) -> str | None:
    if value is None:
        return None
    trimmed = value.strip()
    return trimmed or None
