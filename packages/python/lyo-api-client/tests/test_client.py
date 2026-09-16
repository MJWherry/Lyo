from __future__ import annotations

import json

import pytest

from lyo_api_client import (
    ApiClient,
    ApiClientError,
    ApiRequest,
    ApiResponse,
    PatchRequest,
    build_url,
    entity_metadata_path,
    file_name_from_disposition,
    metadata_path,
    normalize_route_prefix,
    with_bearer_token,
    with_includes,
)

from conftest import StubTransport


class TestBuildUrl:
    def test_joins_base_and_path(self):
        assert build_url("http://localhost:5251", "/person") == "http://localhost:5251/person"

    def test_strips_trailing_slashes_and_adds_leading_slash(self):
        assert build_url("http://localhost:5251//", "person") == "http://localhost:5251/person"

    def test_appends_query_params(self):
        url = build_url("http://x", "/p", {"start": 0, "active": True, "name": "a b"})
        assert url == "http://x/p?start=0&active=true&name=a+b"

    def test_skips_none_query_values(self):
        assert build_url("http://x", "/p", {"a": None, "b": 1}) == "http://x/p?b=1"

    def test_empty_query_returns_bare_url(self):
        assert build_url("http://x", "/p", {}) == "http://x/p"


class TestWithBearerToken:
    def test_adds_authorization_header(self):
        assert with_bearer_token({"A": "1"}, "tok") == {"A": "1", "Authorization": "Bearer tok"}

    def test_no_token_returns_headers_unchanged(self):
        headers = {"A": "1"}
        assert with_bearer_token(headers, None) is headers


class TestApiClientRequest:
    def test_sends_json_content_type_and_bearer_token(self, stub_transport):
        client = ApiClient("http://x", token="tok", transport=stub_transport)
        client.request(ApiRequest(method="GET", path="/health"))
        sent = stub_transport.last
        assert sent.method == "GET"
        assert sent.url == "http://x/health"
        assert sent.headers["Content-Type"] == "application/json"
        assert sent.headers["Authorization"] == "Bearer tok"
        assert sent.body is None

    def test_request_headers_override_defaults(self, stub_transport):
        client = ApiClient("http://x", default_headers={"X-A": "d", "X-B": "d"}, transport=stub_transport)
        client.request(ApiRequest(method="GET", path="/p", headers={"X-B": "r"}))
        assert stub_transport.last.headers["X-A"] == "d"
        assert stub_transport.last.headers["X-B"] == "r"

    def test_serializes_plain_body_as_json(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.request(ApiRequest(method="POST", path="/p", body={"A": 1}))
        assert json.loads(stub_transport.last.body) == {"A": 1}

    def test_serializes_to_dict_body(self, stub_transport):
        class Model:
            def to_dict(self):
                return {"Name": "n"}

        client = ApiClient("http://x", transport=stub_transport)
        client.request(ApiRequest(method="POST", path="/p", body=Model()))
        assert json.loads(stub_transport.last.body) == {"Name": "n"}

    def test_bytes_body_is_not_json(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        raw = b"--bound\r\nscan-bytes\r\n"
        content_type = "multipart/form-data; boundary=bound"
        client.request(ApiRequest(method="POST", path="/upload", body=raw, headers={"Content-Type": content_type}))
        assert stub_transport.last.body == raw
        assert stub_transport.last.headers["Content-Type"] == content_type

    def test_multipart_form_builds_bytes_payload(self):
        from lyo_api_client import multipart_form

        body, content_type = multipart_form(
            [("entityType", "Recipe"), ("file", b"scan-bytes", "scan.bin")]
        )
        assert content_type.startswith("multipart/form-data; boundary=")
        assert b"scan-bytes" in body
        assert b'name="entityType"' in body
        assert b"scan.bin" in body
        assert b"application/json" not in body

    def test_returns_response_on_success(self, stub_transport):
        stub_transport.response = ApiResponse(status=200, ok=True, data={"ok": 1})
        client = ApiClient("http://x", transport=stub_transport)
        res = client.request(ApiRequest(method="GET", path="/p"))
        assert res.data == {"ok": 1}


class TestMetadataPaths:
    def test_normalize_route_prefix(self):
        assert normalize_route_prefix("person") == "/person"
        assert normalize_route_prefix("/person/") == "/person"
        assert normalize_route_prefix("") == ""

    def test_metadata_path(self):
        assert metadata_path("person") == "/person/Metadata"
        assert metadata_path("") == "/Metadata"

    def test_entity_metadata_path(self):
        assert entity_metadata_path("Twilio", "SmsLog") == "/Twilio/SmsLog/Metadata"


class TestMetadataClient:
    def test_get_metadata(self, stub_transport):
        stub_transport.response = ApiResponse(
            status=200, ok=True, data={"keyPropertyName": "Id", "keyType": "Guid"}
        )
        client = ApiClient("http://x", transport=stub_transport)
        res = client.get_metadata("person")
        assert stub_transport.last.url == "http://x/person/Metadata"
        assert res.data["keyPropertyName"] == "Id"

    def test_get_entity_metadata(self, stub_transport):
        stub_transport.response = ApiResponse(status=200, ok=True, data={"entityType": "JobDefinition"})
        client = ApiClient("http://x", transport=stub_transport)
        client.get_entity_metadata("api/Job", "JobDefinition")
        assert stub_transport.last.url == "http://x/api/Job/JobDefinition/Metadata"


class TestErrorNormalization:
    def test_problem_details_title_in_message(self):
        transport = StubTransport(ApiResponse(status=400, ok=False, data={"title": "Bad Input", "status": 400}))
        client = ApiClient("http://x", transport=transport)
        with pytest.raises(ApiClientError) as exc:
            client.request(ApiRequest(method="POST", path="/p"))
        assert str(exc.value) == "400 Bad Input"
        assert exc.value.status == 400
        assert exc.value.details == {"title": "Bad Input", "status": 400}

    def test_generic_message_without_title(self):
        transport = StubTransport(ApiResponse(status=500, ok=False, data=None, raw_body="boom"))
        client = ApiClient("http://x", transport=transport)
        with pytest.raises(ApiClientError) as exc:
            client.request(ApiRequest(method="GET", path="/p"))
        assert str(exc.value) == "Request failed with status 500"
        assert exc.value.details == "boom"


class TestWithIncludes:
    def test_omits_empty(self):
        assert with_includes("/Job/Definition/abc") == "/Job/Definition/abc"
        assert with_includes("/Job/Definition/abc", []) == "/Job/Definition/abc"
        assert with_includes("/Job/Definition/abc", ["  "]) == "/Job/Definition/abc"

    def test_appends_repeated_include(self):
        path = with_includes("/Job/Run/abc", ["JobDefinition", "JobRunLogs"])
        assert path == "/Job/Run/abc?include=JobDefinition&include=JobRunLogs"

    def test_appends_to_existing_query(self):
        path = with_includes("/Job/Run/abc?x=1", ["JobDefinition"])
        assert path == "/Job/Run/abc?x=1&include=JobDefinition"


class TestFileNameFromDisposition:
    def test_quoted(self):
        assert file_name_from_disposition('attachment; filename="report.pdf"') == "report.pdf"

    def test_star_utf8(self):
        assert file_name_from_disposition("attachment; filename*=UTF-8''caf%C3%A9.pdf") == "café.pdf"

    def test_missing(self):
        assert file_name_from_disposition(None) is None


class TestGetByIdAndPatch:
    def test_get_by_id_with_includes(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.get_by_id("Job/Definition", "abc", ["JobParameters"])
        assert stub_transport.last.method == "GET"
        assert stub_transport.last.url == "http://x/Job/Definition/abc?include=JobParameters"

    def test_patch_serializes_body(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.patch("Job/Definition", PatchRequest(keys=[["abc"]], properties={"enabled": True}))
        assert stub_transport.last.method == "PATCH"
        assert stub_transport.last.url == "http://x/Job/Definition"
        assert json.loads(stub_transport.last.body) == {
            "properties": {"enabled": True},
            "keys": [["abc"]],
        }


class TestGenericCrud:
    def test_query_project_posts_body_unchanged(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        body = {
            "Select": ["Id", "Name"],
            "whereClause": {
                "$type": "group",
                "Operator": "And",
                "Children": [{"$type": "condition", "Field": "Name", "Comparison": "Equals", "Value": "a"}],
            },
            "Amount": 20,
        }
        client.query_project("api/Job/Definition", body)
        assert stub_transport.last.method == "POST"
        assert stub_transport.last.url == "http://x/api/Job/Definition/QueryProject"
        assert json.loads(stub_transport.last.body) == body

    def test_create_posts_body_unchanged(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.create("/api/Job/Definition", {"name": "nightly"})
        assert stub_transport.last.method == "POST"
        assert stub_transport.last.url == "http://x/api/Job/Definition"
        assert json.loads(stub_transport.last.body) == {"name": "nightly"}

    def test_update_posts_keys_and_data(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.update("api/Job/Definition", "abc", {"name": "nightly"})
        assert stub_transport.last.method == "POST"
        assert stub_transport.last.url == "http://x/api/Job/Definition/Update"
        assert json.loads(stub_transport.last.body) == {"keys": ["abc"], "data": {"name": "nightly"}}

    def test_delete_by_id_encodes_id(self, stub_transport):
        client = ApiClient("http://x", transport=stub_transport)
        client.delete_by_id("api/Job/Definition", "a/b")
        assert stub_transport.last.method == "DELETE"
        assert stub_transport.last.url == "http://x/api/Job/Definition/a%2Fb"
        assert stub_transport.last.body is None
