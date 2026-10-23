from __future__ import annotations

import json
from pathlib import Path

from lyo_api_client import ApiClient, ApiResponse, TransportRequest

from lyo_cli.commands import (
    create_command,
    finish_login,
    get_command,
    query_command,
    save_token,
    status_text,
)
from lyo_cli.profile import load_profile


def _transport(data):
    seen: list[TransportRequest] = []

    def transport(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        return ApiResponse(status=200, ok=True, data=data)

    return seen, transport


def test_query_merges_select_and_amount_and_prints_json():
    seen, transport = _transport({"items": [{"Id": "1", "Name": "nightly"}]})
    client = ApiClient("http://x", transport=transport)
    text = query_command(
        client,
        "/api/Job/Definition",
        select=["Id", "Name"],
        amount=5,
        body={"whereClause": {"$type": "condition", "Field": "Name", "Comparison": "Equals", "Value": "nightly"}},
    )
    body = json.loads(seen[0].body)
    assert body["Select"] == ["Id", "Name"]
    assert body["Amount"] == 5
    assert body["whereClause"]["Field"] == "Name"
    assert json.loads(text) == {"items": [{"Id": "1", "Name": "nightly"}]}


def test_query_defaults_amount_when_body_omits_it():
    seen, transport = _transport({"items": []})
    client = ApiClient("http://x", transport=transport)
    query_command(client, "/api/Job/Definition", select=["Id"], amount=None, body=None)
    assert json.loads(seen[0].body)["Amount"] == 20


def test_get_and_create():
    seen, transport = _transport({"id": "abc"})
    client = ApiClient("http://x", transport=transport)
    assert json.loads(get_command(client, "/api/Job/Definition", "abc")) == {"id": "abc"}
    assert seen[0].method == "GET"
    assert seen[0].url == "http://x/api/Job/Definition/abc"
    assert json.loads(create_command(client, "/api/Job/Definition", {"name": "nightly"})) == {"id": "abc"}
    assert seen[1].method == "POST"
    assert seen[1].url == "http://x/api/Job/Definition"
    assert json.loads(seen[1].body) == {"name": "nightly"}


def test_status_prints_user_not_token():
    text = status_text({"user": {"id": "u1", "displayName": "Ada"}, "scopes": []}, access_token="secret-token")
    assert "Ada" in text
    assert "secret-token" not in text


def test_save_token_writes_profile(tmp_path: Path):
    path = tmp_path / "profile.json"
    save_token("http://api", "lyo_pat_live_x", path)
    loaded = load_profile(path)
    assert loaded.base_url == "http://api"
    assert loaded.access_token == "lyo_pat_live_x"
    assert path.stat().st_mode & 0o777 == 0o600


def test_finish_login_exchanges_handoff_and_saves(tmp_path: Path):
    path = tmp_path / "profile.json"
    seen: list[TransportRequest] = []

    def transport(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        return ApiResponse(
            status=200,
            ok=True,
            data={"access_token": "jwt", "refresh_token": "ref", "expires_in": 60, "token_type": "Bearer"},
        )

    finish_login("http://x", "lyoh_abc", "http://127.0.0.1:8734", path, transport)
    assert seen[0].url == "http://x/auth/handoff/exchange"
    assert seen[0].headers["Origin"] == "http://127.0.0.1:8734"
    assert json.loads(seen[0].body) == {"code": "lyoh_abc"}
    loaded = load_profile(path)
    assert loaded.access_token == "jwt"
    assert loaded.refresh_token == "ref"
    assert loaded.access_expires_at
