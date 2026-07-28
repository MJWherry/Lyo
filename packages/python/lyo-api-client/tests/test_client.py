from __future__ import annotations

import json

import pytest

from lyo_api_client import ApiClient, ApiClientError, ApiRequest, ApiResponse, build_url, with_bearer_token

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

    def test_returns_response_on_success(self, stub_transport):
        stub_transport.response = ApiResponse(status=200, ok=True, data={"ok": 1})
        client = ApiClient("http://x", transport=stub_transport)
        res = client.request(ApiRequest(method="GET", path="/p"))
        assert res.data == {"ok": 1}


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
