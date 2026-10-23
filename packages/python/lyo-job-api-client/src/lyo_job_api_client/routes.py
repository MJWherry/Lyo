"""REST paths from Lyo.Job.Models.Constants.Rest.Job."""

ROUTE = "Job"
DEFINITIONS = "Job/Definition"
DEFINITIONS_QUERY = "Job/Definition/QueryConcrete"
DEFINITIONS_LATEST_RUNS = "Job/Definition/LatestRuns"
DEFINITION_PARAMETERS = "Job/Definition/Parameter"
SCHEDULES = "Job/Schedule"
SCHEDULE_PARAMETERS = "Job/ScheduleParameters"
TRIGGERS = "Job/Triggers"
RUNS = "Job/Run"
RUNS_CREATE = "Job/Run/Create"
RUNS_RESYNC = "Job/Run/Resync"
RUNS_QUERY = "Job/Run/QueryConcrete"
RUN_LOGS = "Job/Run/Log"
RUN_PARAMETERS = "Job/Run/Parameter"
RUN_RESULTS = "Job/Run/Result"
WORKER_INSTANCES = "Job/WorkerInstance"
BLACKOUT_CALENDARS = "Job/BlackoutCalendar"
BLACKOUT_WINDOWS = "Job/BlackoutCalendar/Window"
WORKFLOWS = "Job/Workflow"
WORKFLOW_STEPS = "Job/Workflow/Step"
WORKFLOW_RUNS = "Job/Workflow/Run"
WORKFLOW_RUN_STEPS = "Job/Workflow/Run/Step"


def run_started(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Started"


def run_finished(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Finished"


def run_requeue(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Requeue"


def run_cancel(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Cancel"


def run_rerun(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Rerun"


def run_log(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Log"


def run_children(parent_run_id: str) -> str:
    return f"{RUNS}/{parent_run_id}/Children"


def run_heartbeat(run_id: str) -> str:
    return f"{RUNS}/{run_id}/Heartbeat"


def definition_stats(definition_id: str) -> str:
    return f"{DEFINITIONS}/{definition_id}/Stats"


def definition_next_runs(definition_id: str) -> str:
    return f"{DEFINITIONS}/{definition_id}/NextRuns"


def join_route_prefix(route_prefix: str | None, relative: str) -> str:
    relative = relative.lstrip("/")
    prefix = (route_prefix or "").strip().strip("/")
    return f"/{prefix}/{relative}" if prefix else f"/{relative}"
