from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse, TransportRequest
from lyo_automation_api_client import AutomationApiClient, AutomationRunReq


class _ScriptedTransport:
    def __init__(self, handler) -> None:
        self.handler = handler
        self.requests: list[TransportRequest] = []

    def __call__(self, request: TransportRequest) -> ApiResponse:
        self.requests.append(request)
        return self.handler(request)


def test_start_posts_automation_run_and_returns_run_id():
    def handler(request: TransportRequest) -> ApiResponse:
        assert request.method == "POST"
        assert request.url == "http://x/Automation/Run"
        assert json.loads(request.body)["plan"]["name"] == "demo"
        return ApiResponse(status=202, ok=True, data={"runId": "run-1", "status": "queued"})

    client = AutomationApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    accepted = client.start(AutomationRunReq(plan={"name": "demo", "steps": []}))
    assert accepted.run_id == "run-1"


def test_get_cancel_log_snapshots_and_snapshot_use_run_paths():
    seen: list[tuple[str, str]] = []

    def handler(request: TransportRequest) -> ApiResponse:
        seen.append((request.method, request.url))
        if request.url.endswith("/Snapshot/shot%201.png"):
            return ApiResponse(status=200, ok=True, body_bytes=b"png")
        if request.url.endswith("/Log"):
            return ApiResponse(status=200, ok=True, raw_body="line")
        if request.url.endswith("/Snapshots"):
            return ApiResponse(status=200, ok=True, data=["shot 1.png"])
        return ApiResponse(status=200, ok=True, data={"runId": "run-1", "status": "running"})

    client = AutomationApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    assert client.get("run-1").status == "running"
    client.cancel("run-1")
    assert client.log("run-1") == "line"
    assert client.snapshots("run-1") == ["shot 1.png"]
    assert client.snapshot("run-1", "shot 1.png") == b"png"
    assert seen == [
        ("GET", "http://x/Automation/Run/run-1"),
        ("POST", "http://x/Automation/Run/run-1/Cancel"),
        ("GET", "http://x/Automation/Run/run-1/Log"),
        ("GET", "http://x/Automation/Run/run-1/Snapshots"),
        ("GET", "http://x/Automation/Run/run-1/Snapshot/shot%201.png"),
    ]


def test_wait_until_settled_polls_until_completed():
    gets = 0

    def handler(request: TransportRequest) -> ApiResponse:
        nonlocal gets
        if request.method == "POST":
            return ApiResponse(status=202, ok=True, data={"runId": "run-1", "status": "queued"})
        gets += 1
        status = "queued" if gets < 2 else "completed"
        return ApiResponse(status=200, ok=True, data={"runId": "run-1", "status": status})

    client = AutomationApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    accepted = client.start(AutomationRunReq(plan={"name": "demo"}))
    settled = client.wait_until_settled(accepted.run_id)
    assert settled.status == "completed"
    assert gets == 2
