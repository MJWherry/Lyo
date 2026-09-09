"""Job request models. Snake_case attributes; camelCase ``to_dict()`` wire JSON."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is None:
        return
    if hasattr(value, "to_dict"):
        out[key] = value.to_dict()
    elif isinstance(value, list):
        out[key] = [v.to_dict() if hasattr(v, "to_dict") else v for v in value]
    else:
        out[key] = value


@dataclass
class JobRunParameterReq:
    key: str
    type: str | None = None
    value: str | None = None
    description: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"key": self.key}
        _put(out, "type", self.type)
        _put(out, "value", self.value)
        _put(out, "description", self.description)
        return out


@dataclass
class JobRunReq:
    job_definition_id: str
    created_by: str
    allow_triggers: bool = False
    job_schedule_id: str | None = None
    job_trigger_id: str | None = None
    dry_run: bool = False
    suppress_dispatch: bool = False
    job_run_parameters: list[JobRunParameterReq] = field(default_factory=list)
    idempotency_key: str | None = None
    priority: int | None = None
    trace_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "jobDefinitionId": self.job_definition_id,
            "createdBy": self.created_by,
            "allowTriggers": self.allow_triggers,
        }
        _put(out, "jobScheduleId", self.job_schedule_id)
        _put(out, "jobTriggerId", self.job_trigger_id)
        if self.dry_run:
            out["dryRun"] = True
        if self.suppress_dispatch:
            out["suppressDispatch"] = True
        if self.job_run_parameters:
            out["jobRunParameters"] = [p.to_dict() for p in self.job_run_parameters]
        _put(out, "idempotencyKey", self.idempotency_key)
        _put(out, "priority", self.priority)
        _put(out, "traceId", self.trace_id)
        return out


@dataclass
class JobRunStartedReq:
    worker_instance_id: str | None = None
    machine_name: str | None = None
    process_id: int | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "workerInstanceId", self.worker_instance_id)
        _put(out, "machineName", self.machine_name)
        _put(out, "processId", self.process_id)
        return out


@dataclass
class JobRunHeartbeatReq:
    progress_percent: int | None = None
    progress_message: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "progressPercent", self.progress_percent)
        _put(out, "progressMessage", self.progress_message)
        return out


@dataclass
class JobRunLogReq:
    level: str
    message: str
    context: str | None = None
    stack_trace: str | None = None
    timestamp: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"level": self.level, "message": self.message}
        _put(out, "context", self.context)
        _put(out, "stackTrace", self.stack_trace)
        _put(out, "timestamp", self.timestamp)
        return out


@dataclass
class JobRunResultReq:
    key: str
    type: str = ""
    value: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"key": self.key, "type": self.type}
        _put(out, "value", self.value)
        return out


@dataclass
class JobWorkerInstanceReq:
    worker_type: str
    machine_name: str
    process_id: int
    started_timestamp: str
    last_heartbeat_utc: str
    state: str = "running"
    in_flight_count: int = 0
    metadata: dict[str, str | None] | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "workerType": self.worker_type,
            "machineName": self.machine_name,
            "processId": self.process_id,
            "startedTimestamp": self.started_timestamp,
            "lastHeartbeatUtc": self.last_heartbeat_utc,
            "state": self.state,
            "inFlightCount": self.in_flight_count,
        }
        _put(out, "metadata", self.metadata)
        return out


@dataclass
class JobDefinitionReq:
    name: str
    type: str
    worker_type: str
    description: str | None = None
    enabled: bool = True

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "name": self.name,
            "type": self.type,
            "workerType": self.worker_type,
            "enabled": self.enabled,
        }
        _put(out, "description", self.description)
        return out


JOB_DEFINITION_EDITOR_INCLUDES = [
    "JobParameters",
    "JobSchedules.JobScheduleParameters",
    "JobSchedules.JobBlackoutCalendar.JobBlackoutWindows",
    "JobTriggerTriggersJobDefinitions",
]

JOB_RUN_DETAIL_INCLUDES = [
    "JobDefinition",
    "JobRunLogs",
    "JobRunResults",
    "JobRunParameters",
    "JobSchedule",
]
