from __future__ import annotations

import json

from lyo_api_client import ApiClient
from lyo_person_api_client import (
    FromClause,
    PersonApiClient,
    QueryReq,
    baseline_query,
    build_options,
    select_projection_query,
)


def make_client(stub_transport) -> PersonApiClient:
    return PersonApiClient(ApiClient("http://x", transport=stub_transport))


def test_query_person_posts_to_query_concrete(stub_transport):
    make_client(stub_transport).query_person(baseline_query())
    sent = stub_transport.last
    assert sent.method == "POST"
    assert sent.url == "http://x/person/QueryConcrete"
    assert json.loads(sent.body) == baseline_query().to_dict()


def test_query_person_projected_posts_to_query_project(stub_transport):
    make_client(stub_transport).query_person_projected(select_projection_query())
    sent = stub_transport.last
    assert sent.url == "http://x/person/QueryProject"
    assert json.loads(sent.body) == select_projection_query().to_dict()


def test_query_root_posts_to_root_query(stub_transport):
    request = QueryReq(
        options=build_options(),
        from_clause=FromClause(alias="p", entity_type="Person"),
        select=["p.Id"],
    )
    make_client(stub_transport).query_root(request)
    sent = stub_transport.last
    assert sent.url == "http://x/Query"
    payload = json.loads(sent.body)
    assert payload["From"] == {"Alias": "p", "EntityType": "Person"}
    assert payload["Select"] == ["p.Id"]
    assert "Joins" not in payload


def test_get_person_gets_by_id_with_includes(stub_transport):
    make_client(stub_transport).get_person(
        "abc-123",
        include=["contactaddresses.address", "contactemailaddresses.emailaddress"],
    )
    sent = stub_transport.last
    assert sent.method == "GET"
    assert sent.url.startswith("http://x/person/abc-123?")
    assert "include=contactaddresses.address" in sent.url
    assert "include=contactemailaddresses.emailaddress" in sent.url
