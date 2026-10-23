"""Sync Automation API client built on ``lyo_api_client``."""

from __future__ import annotations

import time
from typing import Any

from lyo_api_client import ApiClient, ApiRequest

from . import routes
from .models import AutomationRunAccepted, AutomationRunReq, AutomationRunRes, is_settled


class AutomationApiClient:
    """Typed calls for automation runs. The caller stays on the API host."""

    def __init__(self, api_client: ApiClient) -> None:
        self._api = api_client

    def start(self, request: AutomationRunReq | dict[str, Any]) -> AutomationRunAccepted:
        """``POST Automation/Run``. Returns the accepted run id."""
        response = self._api.request(ApiRequest(method="POST", path=routes.RUNS, body=request))
        if not isinstance(response.data, dict):
            raise RuntimeError(f"Start failed: {response.status}")
        return AutomationRunAccepted.from_dict(response.data)

    def get(self, run_id: str) -> AutomationRunRes:
        """``GET Automation/Run/{id}``."""
        response = self._api.request(ApiRequest(method="GET", path=routes.run_path(run_id)))
        if not isinstance(response.data, dict):
            raise RuntimeError(f"Get failed: {response.status}")
        return AutomationRunRes.from_dict(response.data)

    def cancel(self, run_id: str) -> None:
        """``POST Automation/Run/{id}/Cancel``."""
        self._api.request(ApiRequest(method="POST", path=routes.run_cancel_path(run_id)))

    def log(self, run_id: str) -> str:
        """``GET Automation/Run/{id}/Log``."""
        response = self._api.request(ApiRequest(method="GET", path=routes.run_log_path(run_id)))
        return response.raw_body or ""

    def snapshots(self, run_id: str) -> list[str]:
        """``GET Automation/Run/{id}/Snapshots``."""
        response = self._api.request(ApiRequest(method="GET", path=routes.run_snapshots_path(run_id)))
        if not isinstance(response.data, list):
            raise RuntimeError(f"Snapshots failed: {response.status}")
        return [str(name) for name in response.data]

    def snapshot(self, run_id: str, name: str) -> bytes | None:
        """``GET Automation/Run/{id}/Snapshot/{name}`` as raw bytes."""
        response = self._api.request(ApiRequest(method="GET", path=routes.run_snapshot_path(run_id, name)))
        if response.body_bytes is not None:
            return response.body_bytes
        if response.raw_body is not None:
            return response.raw_body.encode()
        return None

    def step_screenshot(self, run_id: str, index: int) -> bytes | None:
        """``GET Automation/Run/{id}/Steps/{index}/Screenshot`` as raw bytes."""
        response = self._api.request(ApiRequest(method="GET", path=routes.run_step_screenshot_path(run_id, index)))
        if response.body_bytes is not None:
            return response.body_bytes
        if response.raw_body is not None:
            return response.raw_body.encode()
        return None

    def wait_until_settled(self, run_id: str, pause_ms: int = 0) -> AutomationRunRes:
        """Poll ``get`` until the run is completed, failed, or cancelled."""
        for _attempt in range(120):
            run = self.get(run_id)
            if is_settled(run.status):
                return run
            if pause_ms > 0:
                time.sleep(pause_ms / 1000)
        raise RuntimeError("The run did not finish.")
