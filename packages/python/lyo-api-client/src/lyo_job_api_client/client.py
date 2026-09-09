"""Job API client built on top of lyo_api_client."""

from __future__ import annotations

from datetime import datetime, timezone
from typing import Any

from lyo_api_client import ApiClient, ApiRequest, ApiResponse, PatchRequest
from lyo_person_api_client.models import QueryConcreteReq

from . import routes
from .models import (
    JobDefinitionReq,
    JobRunHeartbeatReq,
    JobRunLogReq,
    JobRunReq,
    JobRunStartedReq,
    JobWorkerInstanceReq,
)
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

    def patch(self, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        return self._api.patch(self.route, body)

    def delete_by_id(self, entity_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{entity_id}"))


class _Definitions(_Crud):
    def stats(self, definition_id: str, days: int | None = None) -> ApiResponse:
        query = {"days": days} if days is not None else None
        return self._api.request(ApiRequest(method="GET", path=f"{self.route}/{definition_id}/Stats", query=query))

    def next_runs(self, definition_id: str, count: int | None = None) -> ApiResponse:
        query = {"count": count} if count is not None else None
        return self._api.request(
            ApiRequest(method="GET", path=f"{self.route}/{definition_id}/NextRuns", query=query)
        )

    def latest_runs(self, definition_ids: list[str]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/LatestRuns", body=definition_ids))

    def get_distinct_worker_types(self) -> list[str]:
        res = self.query_concrete(QueryConcreteReq(amount=500))
        items = (res.data or {}).get("items") or []
        seen: set[str] = set()
        out: list[str] = []
        for item in items:
            worker_type = (item.get("workerType") or "").strip()
            if worker_type and worker_type.lower() not in seen:
                seen.add(worker_type.lower())
                out.append(worker_type)
        return out


class _Runs:
    def __init__(self, api: ApiClient, prefix: str) -> None:
        self._api = api
        self._p = lambda rel: join_route_prefix(prefix, rel)
        self.route = self._p(routes.RUNS)

    def query_concrete(self, request: QueryConcreteReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryConcrete", body=request))

    def query_project(self, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryProject", body=request))

    def get(self, run_id: str, include: list[str] | None = None) -> ApiResponse:
        return self._api.get_by_id(self.route, run_id, include)

    def delete_by_id(self, run_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{run_id}"))

    def create(self, request: JobRunReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.RUNS_CREATE), body=request))

    def start(self, run_id: str, request: JobRunStartedReq | dict[str, Any] | None = None) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_started(run_id)), body=request))

    def finish(self, run_id: str, results: list[Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_finished(run_id)), body=results))

    def cancel(self, run_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_cancel(run_id))))

    def requeue(self, run_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_requeue(run_id))))

    def rerun(self, run_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_rerun(run_id))))

    def resync_queued(self, definition_id: str | None = None) -> ApiResponse:
        query = {"definitionId": definition_id} if definition_id else None
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.RUNS_RESYNC), query=query))

    def log(self, run_id: str, request: JobRunLogReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_log(run_id)), body=request))

    def heartbeat(self, run_id: str, request: JobRunHeartbeatReq | dict[str, Any] | None = None) -> ApiResponse:
        return self._api.request(ApiRequest(method="PATCH", path=self._p(routes.run_heartbeat(run_id)), body=request))

    def create_children(self, parent_run_id: str, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self._p(routes.run_children(parent_run_id)), body=request))


class _Workers(_Crud):
    def register(self, request: JobWorkerInstanceReq | dict[str, Any]) -> ApiResponse:
        return self.create(request)

    def heartbeat(self, instance_id: str, in_flight_count: int, metadata: dict[str, str | None] | None = None) -> ApiResponse:
        properties: {
            "LastHeartbeatUtc": datetime.now(timezone.utc).isoformat(),
            "InFlightCount": in_flight_count,
        }
        if metadata:
            import json

            properties["MetadataJson"] = json.dumps(metadata)
        return self.patch(PatchRequest(keys=[[instance_id]], properties=properties))

    def stop(self, instance_id: str) -> ApiResponse:
        return self.patch(
            PatchRequest(
                keys=[[instance_id]],
                properties={
                    "State": "stopped",
                    "InFlightCount": 0,
                    "LastHeartbeatUtc": datetime.now(timezone.utc).isoformat(),
                },
            )
        )


class JobApiClient:
    """Typed endpoints for the Lyo Job API."""

    def __init__(self, api_client: ApiClient, *, route_prefix: str | None = None) -> None:
        self.api_client = api_client
        prefix = route_prefix
        p = lambda rel: join_route_prefix(prefix, rel)
        self.definitions = _Definitions(api_client, p(routes.DEFINITIONS))
        self.definition_parameters = _Crud(api_client, p(routes.DEFINITION_PARAMETERS))
        self.runs = _Runs(api_client, prefix or "")
        self.run_parameters = _Crud(api_client, p(routes.RUN_PARAMETERS))
        self.run_results = _Crud(api_client, p(routes.RUN_RESULTS))
        self.run_logs = _Crud(api_client, p(routes.RUN_LOGS))
        self.schedules = _Crud(api_client, p(routes.SCHEDULES))
        self.schedule_parameters = _Crud(api_client, p(routes.SCHEDULE_PARAMETERS))
        self.triggers = _Crud(api_client, p(routes.TRIGGERS))
        self.worker_instances = _Workers(api_client, p(routes.WORKER_INSTANCES))
        self.blackout_calendars = _Crud(api_client, p(routes.BLACKOUT_CALENDARS))
        self.blackout_windows = _Crud(api_client, p(routes.BLACKOUT_WINDOWS))
        self.workflows = _Crud(api_client, p(routes.WORKFLOWS))
        self.workflow_steps = _Crud(api_client, p(routes.WORKFLOW_STEPS))
        self.workflow_runs = _Crud(api_client, p(routes.WORKFLOW_RUNS))
        self.workflow_run_steps = _Crud(api_client, p(routes.WORKFLOW_RUN_STEPS))
