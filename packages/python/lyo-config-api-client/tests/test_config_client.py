from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse
from lyo_config_api_client import ConfigApiClient, ConfigBindingRecord, ConfigDefinitionRecord, ConfigValue

from conftest import StubTransport


def test_resolve_not_modified_sends_etag_and_api_key(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=304, ok=False)
    config = ConfigApiClient(ApiClient("http://x", transport=stub_transport), api_key="secret")
    result = config.resolve("App", "gateway:local", if_none_match='"abc"')
    assert result.outcome == 304
    assert result.record is None
    assert stub_transport.last.method == "GET"
    assert stub_transport.last.url == "http://x/api/config/App/gateway%3Alocal"
    assert stub_transport.last.headers["If-None-Match"] == '"abc"'
    assert stub_transport.last.headers["X-Api-Key"] == "secret"


def test_put_definition_sends_plaintext_and_encrypted_flag(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={})
    config = ConfigApiClient(ApiClient("http://x", transport=stub_transport))
    config.put_definition(
        ConfigDefinitionRecord(
            subject_entity_type="App",
            key="Theme",
            for_value_type="System.String",
            is_encrypted=True,
            default_value=ConfigValue(type_name="System.String", json='"light"'),
        )
    )
    assert stub_transport.last.method == "PUT"
    assert stub_transport.last.url == "http://x/api/config/manage/definitions"
    body = json.loads(stub_transport.last.body)
    assert body["isEncrypted"] is True
    assert body["defaultValue"] == {"typeName": "System.String", "json": '"light"'}
    assert "cipher" not in json.dumps(body).lower()


def test_resolve_head_not_modified_when_etag_matches(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=304, ok=False)
    config = ConfigApiClient(ApiClient("http://x", transport=stub_transport))
    result = config.resolve("App", "local", method="HEAD", version='"abc"')
    assert result.outcome == 304
    assert result.record is None
    assert stub_transport.last.method == "HEAD"
    assert stub_transport.last.url == "http://x/api/config/App/local?version=%22abc%22"


def test_manage_routes(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={})
    config = ConfigApiClient(ApiClient("http://x", transport=stub_transport))
    config.list_definitions("App")
    config.get_definition_by_key("App", "Theme")
    config.get_definition("def-1")
    config.definition_revisions("def-1")
    config.get_definition_revision("def-1", 2)
    config.revert_definition("def-1", 2)
    config.delete_definition("def-1")
    config.list_bindings("App", "local")
    config.get_binding_by_key("App", "local", "Theme")
    config.get_binding("bind-1")
    config.put_binding(
        ConfigBindingRecord(
            definition_id="def-1",
            key="Theme",
            subject_entity_type="App",
            subject_entity_id="local",
            value=ConfigValue(type_name="System.String", json='"light"'),
        )
    )
    config.delete_bindings("App", "local")
    config.delete_binding("bind-1")
    config.binding_revisions("bind-1")
    config.get_binding_revision("bind-1", 3)
    config.revert_binding("bind-1", 3)
    config.resolved("App", "local")
    urls = [(request.method, request.url) for request in stub_transport.requests]
    assert urls[0] == ("GET", "http://x/api/config/manage/definitions?subjectEntityType=App")
    assert urls[1][0] == "GET" and "definitions/by-key" in urls[1][1] and "key=Theme" in urls[1][1]
    assert urls[2] == ("GET", "http://x/api/config/manage/definitions/def-1")
    assert urls[3] == ("GET", "http://x/api/config/manage/definitions/def-1/revisions")
    assert urls[4] == ("GET", "http://x/api/config/manage/definitions/def-1/revisions/2")
    assert urls[5] == ("POST", "http://x/api/config/manage/definitions/def-1/revert")
    assert json.loads(stub_transport.requests[5].body) == {"revision": 2}
    assert urls[6] == ("DELETE", "http://x/api/config/manage/definitions/def-1")
    assert urls[7][0] == "GET" and urls[7][1].startswith("http://x/api/config/manage/bindings?")
    assert "by-key" in urls[8][1]
    assert urls[9] == ("GET", "http://x/api/config/manage/bindings/bind-1")
    assert urls[10] == ("PUT", "http://x/api/config/manage/bindings")
    assert urls[11][0] == "DELETE" and urls[11][1].startswith("http://x/api/config/manage/bindings?")
    assert urls[12] == ("DELETE", "http://x/api/config/manage/bindings/bind-1")
    assert urls[13] == ("GET", "http://x/api/config/manage/bindings/bind-1/revisions")
    assert urls[14] == ("GET", "http://x/api/config/manage/bindings/bind-1/revisions/3")
    assert urls[15] == ("POST", "http://x/api/config/manage/bindings/bind-1/revert")
    assert urls[16][0] == "GET" and urls[16][1].startswith("http://x/api/config/manage/resolved?")
