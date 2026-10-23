from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse
from lyo_import_api_client import (
    ImportApiClient,
    ImportFieldMap,
    ImportIssue,
    ImportRequest,
    empty_preview,
    show_preview,
    try_add_field,
)

from conftest import StubTransport


def test_post_import_posts_import_path(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"isSuccess": True})
    client = ImportApiClient(ApiClient("http://x", transport=stub_transport))
    client.post_import(ImportRequest(format="Csv", content="a,b", fields=[ImportFieldMap("given", "Name")]))
    assert stub_transport.last.method == "POST"
    assert stub_transport.last.url == "http://x/Import"
    body = json.loads(stub_transport.last.body)
    assert body["format"] == "Csv"
    assert body["content"] == "a,b"
    assert body["fields"] == [{"source": "given", "target": "Name"}]


def test_post_import_path_is_overridable(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"isSuccess": True})
    client = ImportApiClient(ApiClient("http://x", transport=stub_transport), path="/custom/Import")
    client.post_import({"format": "Json"})
    assert stub_transport.last.url == "http://x/custom/Import"


def test_try_add_field_rejects_duplicate_target():
    once = try_add_field(empty_preview(), ImportFieldMap("given", "Name"))
    twice = try_add_field(once, ImportFieldMap("family", "Name"))
    assert [(field.source, field.target) for field in twice.fields] == [("given", "Name")]
    assert twice.blocked_reason == "Two sources map to 'Name'."


def test_show_preview_caps_at_50_and_keeps_every_issue():
    rows = [{"name": f"r{index}"} for index in range(60)]
    issues = [ImportIssue(1, "a"), ImportIssue(2, "b"), ImportIssue(3, "c")]
    shown = show_preview(rows, issues, lambda row: row["name"])
    assert len(shown.preview) == 50
    assert shown.preview[0] == "r0"
    assert shown.issues == issues
