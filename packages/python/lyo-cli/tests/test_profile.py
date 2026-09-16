from __future__ import annotations

import json
from datetime import datetime, timezone
from pathlib import Path

import pytest
from lyo_api_client import ApiResponse, TransportRequest

from lyo_cli.profile import MissingProfileError, Profile, resolve_profile, save_profile
from lyo_cli.session import connect


def test_env_token_overrides_file(tmp_path: Path, monkeypatch: pytest.MonkeyPatch):
    path = tmp_path / "profile.json"
    save_profile(Profile(base_url="http://file", access_token="file-token", refresh_token="file-refresh"), path)
    monkeypatch.setenv("LYO_API_TOKEN", "env-token")
    resolved = resolve_profile(environ={"LYO_API_TOKEN": "env-token"}, path=path)
    assert resolved.access_token == "env-token"
    assert resolved.base_url == "http://file"
    assert resolved.refresh_token is None


def test_missing_profile_raises(tmp_path: Path):
    with pytest.raises(MissingProfileError):
        resolve_profile(environ={}, path=tmp_path / "missing.json")


def test_save_uses_mode_0600(tmp_path: Path):
    path = tmp_path / "dir" / "profile.json"
    save_profile(Profile(base_url="http://api", access_token="tok"), path)
    assert path.stat().st_mode & 0o777 == 0o600
    assert json.loads(path.read_text())["access_token"] == "tok"


def test_expired_access_refreshes_once_then_queries(tmp_path: Path):
    path = tmp_path / "profile.json"
    save_profile(
        Profile(
            base_url="http://x",
            access_token="old",
            refresh_token="ref",
            access_expires_at="2000-01-01T00:00:00Z",
        ),
        path,
    )
    seen: list[TransportRequest] = []

    def transport(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        if request.url.endswith("/auth/refresh"):
            return ApiResponse(
                status=200,
                ok=True,
                data={"access_token": "new", "refresh_token": "ref2", "expires_in": 3600, "token_type": "Bearer"},
            )
        return ApiResponse(status=200, ok=True, data={"items": [{"Id": "1"}]})

    client = connect(
        resolve_profile(environ={}, path=path),
        transport=transport,
        profile_path=path,
        now=datetime(2026, 1, 1, tzinfo=timezone.utc),
    )
    client.query_project("/api/Job/Definition", {"Select": ["Id"], "Amount": 20})
    assert len(seen) == 2
    assert seen[0].url == "http://x/auth/refresh"
    assert json.loads(seen[0].body) == {"refresh_token": "ref"}
    assert seen[1].url == "http://x/api/Job/Definition/QueryProject"
    assert seen[1].headers["Authorization"] == "Bearer new"
    saved = json.loads(path.read_text())
    assert saved["access_token"] == "new"
    assert saved["refresh_token"] == "ref2"


def test_opaque_token_does_not_refresh(tmp_path: Path):
    path = tmp_path / "profile.json"
    save_profile(
        Profile(
            base_url="http://x",
            access_token="lyo_pat_live_x",
            refresh_token="ref",
            access_expires_at="2000-01-01T00:00:00Z",
        ),
        path,
    )
    seen: list[str] = []

    def transport(request: TransportRequest) -> ApiResponse:
        seen.append(request.url)
        return ApiResponse(status=200, ok=True, data={"items": []})

    client = connect(
        resolve_profile(environ={}, path=path),
        transport=transport,
        profile_path=path,
        now=datetime(2026, 1, 1, tzinfo=timezone.utc),
    )
    client.query_project("/api/Job/Definition", {"Select": ["Id"]})
    assert seen == ["http://x/api/Job/Definition/QueryProject"]
    assert client.token == "lyo_pat_live_x"
