from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse, ParameterOptionsResolveReq
from lyo_reporting_api_client import GenerateReportReq, ReportingApiClient
from lyo_person_api_client import QueryConcreteReq

from conftest import StubTransport


def test_generate_and_query(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"id": "g1", "status": "pending"})
    reports = ReportingApiClient(ApiClient("http://x", transport=stub_transport))
    reports.generations.generate(GenerateReportReq(report_definition_id="d1", created_by="ui"))
    assert stub_transport.last.url == "http://x/Reporting/Generation/Generate"
    assert json.loads(stub_transport.last.body)["reportDefinitionId"] == "d1"
    reports.definitions.query_concrete(QueryConcreteReq(amount=5))
    assert stub_transport.last.url == "http://x/Reporting/Definition/QueryConcrete"


def test_resolve_parameter_options(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"items": []})
    reports = ReportingApiClient(ApiClient("http://x", transport=stub_transport))
    reports.resolve_parameter_options(ParameterOptionsResolveReq(options_json="{}"))
    assert stub_transport.last.url == "http://x/Reporting/ResolveParameterOptions"
