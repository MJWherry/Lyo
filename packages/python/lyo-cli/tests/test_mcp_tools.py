from __future__ import annotations

import json

import pytest
from lyo_api_client import ApiClient, ApiResponse, TransportRequest

from lyo_cli.mcp_tools import QueryRejected, delete_entity, query_project_tool, tool_names, writes_enabled


def test_query_project_rejects_missing_select_and_amount_over_100():
    client = ApiClient("http://x", transport=lambda request: ApiResponse(status=200, ok=True, data={}))
    with pytest.raises(QueryRejected):
        query_project_tool(client, "/api/Job/Definition", {"Amount": 20})
    with pytest.raises(QueryRejected):
        query_project_tool(client, "/api/Job/Definition", {"Select": ["Id"], "Amount": 101})


def test_write_tools_absent_unless_enabled():
    assert "delete_entity" not in tool_names(False)
    assert "create_entity" not in tool_names(False)
    assert "query_project" in tool_names(False)
    assert "delete_entity" in tool_names(True)
    assert writes_enabled({}) is False
    assert writes_enabled({"LYO_MCP_WRITES": "1"}) is True


def test_delete_entity_calls_delete_by_id():
    seen: list[TransportRequest] = []

    def transport(request: TransportRequest) -> ApiResponse:
        seen.append(request)
        return ApiResponse(status=200, ok=True, data={"deleted": True})

    client = ApiClient("http://x", transport=transport)
    assert delete_entity(client, "/api/Job/Definition", "a/b") == {"deleted": True}
    assert seen[0].method == "DELETE"
    assert seen[0].url == "http://x/api/Job/Definition/a%2Fb"
    query_project_tool(client, "/api/Job/Definition", {"Select": ["Id"], "Amount": 100})
    assert json.loads(seen[1].body) == {"Select": ["Id"], "Amount": 100}
