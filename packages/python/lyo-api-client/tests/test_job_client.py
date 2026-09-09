from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse
from lyo_job_api_client import JobApiClient, JobRunReq, join_route_prefix
from lyo_person_api_client import QueryConcreteReq

from conftest import StubTransport


def test_join_route_prefix():
    assert join_route_prefix(None, "Job/Definition") == "/Job/Definition"
    assert join_route_prefix("api", "Job/Run") == "/api/Job/Run"


def test_run_create_and_cancel(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"isSuccess": True})
    jobs = JobApiClient(ApiClient("http://x", transport=stub_transport))
    jobs.runs.create(JobRunReq(job_definition_id="d1", created_by="tester"))
    assert stub_transport.last.method == "POST"
    assert stub_transport.last.url == "http://x/Job/Run/Create"
    assert json.loads(stub_transport.last.body)["jobDefinitionId"] == "d1"
    jobs.runs.cancel("run-1")
    assert stub_transport.last.url == "http://x/Job/Run/run-1/Cancel"


def test_definition_query_and_stats(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"isSuccess": True, "items": []})
    jobs = JobApiClient(ApiClient("http://x", transport=stub_transport))
    jobs.definitions.query_concrete(QueryConcreteReq(amount=10))
    assert stub_transport.last.url == "http://x/Job/Definition/QueryConcrete"
    jobs.definitions.stats("def-1", days=7)
    assert stub_transport.last.url == "http://x/Job/Definition/def-1/Stats?days=7"
