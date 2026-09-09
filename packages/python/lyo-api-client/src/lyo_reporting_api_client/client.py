"""Reporting API client built on top of lyo_api_client."""

from __future__ import annotations

from typing import Any

from lyo_api_client import (
    ApiClient,
    ApiRequest,
    ApiResponse,
    ParameterOptionsResolveReq,
    PatchRequest,
    file_name_from_disposition,
)
from lyo_person_api_client.models import QueryConcreteReq

from . import routes
from .models import GenerateReportReq, ReportDefinitionParameterReq, ReportDefinitionReq
from .routes import join_route_prefix


class _Crud:
    def __init__(self, api: ApiClient, route: str) -> None:
        self._api = api
        self.route = route

    def query_concrete(self, request: QueryConcreteReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryConcrete", body=request))

    def query_project(self, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryProject", body=request))

    def get(self, entity_id: str, include: list[str] | None = None) -> ApiResponse:
        return self._api.get_by_id(self.route, entity_id, include)

    def create(self, body: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self.route, body=body))

    def update(self, entity_id: str, body: Any) -> ApiResponse:
        return self._api.request(
            ApiRequest(method="POST", path=f"{self.route}/Update", body={"keys": [entity_id], "data": body})
        )

    def patch(self, entity_id: str, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        if isinstance(body, PatchRequest) and body.keys is None:
            body.keys = [[entity_id]]
        return self._api.patch(self.route, body)

    def delete_by_id(self, entity_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{entity_id}"))


class _Generations:
    def __init__(self, api: ApiClient, prefix: str | None) -> None:
        self._api = api
        self._p = lambda rel: join_route_prefix(prefix, rel)
        self.route = self._p(routes.GENERATIONS)

    def query_concrete(self, request: QueryConcreteReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryConcrete", body=request))

    def query_project(self, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryProject", body=request))

    def get(self, generation_id: str, include: list[str] | None = None) -> ApiResponse:
        return self._api.get_by_id(self.route, generation_id, include)

    def delete_by_id(self, generation_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{generation_id}"))

    def generate(self, request: GenerateReportReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.GENERATIONS_GENERATE), body=request))

    def rerun(self, generation_id: str, include_report_data: bool = False) -> ApiResponse:
        query = {"includeReportData": True} if include_report_data else None
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.generation_rerun(generation_id)), query=query))

    def download(self, generation_id: str) -> tuple[bytes | None, str | None]:
        res = self._api.request(ApiRequest(method="GET", path=self._p(routes.generation_download(generation_id))))
        name = file_name_from_disposition(res.headers.get("content-disposition") or res.headers.get("Content-Disposition"))
        payload = res.body_bytes
        if payload is None and res.raw_body is not None:
            payload = res.raw_body.encode()
        return payload, name


class ReportingApiClient:
    """Typed endpoints for the Lyo Reporting API."""

    def __init__(self, api_client: ApiClient, *, route_prefix: str | None = None) -> None:
        self.api_client = api_client
        self._prefix = route_prefix
        p = lambda rel: join_route_prefix(route_prefix, rel)
        self.definitions = _Crud(api_client, p(routes.DEFINITIONS))
        self.definition_parameters = _Crud(api_client, p(routes.DEFINITION_PARAMETERS))
        self.generations = _Generations(api_client, route_prefix)

    def resolve_parameter_options(self, body: ParameterOptionsResolveReq | dict[str, Any]) -> ApiResponse:
        return self.api_client.request(
            ApiRequest(
                method="POST",
                path=join_route_prefix(self._prefix, routes.RESOLVE_PARAMETER_OPTIONS),
                body=body,
            )
        )
