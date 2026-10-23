"""REST paths from Lyo.Reporting.Models.Constants.Rest.Reporting."""

ROUTE = "Reporting"
DEFINITIONS = "Reporting/Definition"
DEFINITION_PARAMETERS = "Reporting/Definition/Parameter"
GENERATIONS = "Reporting/Generation"
GENERATIONS_GENERATE = "Reporting/Generation/Generate"
RESOLVE_PARAMETER_OPTIONS = "Reporting/ResolveParameterOptions"


def generation_download(generation_id: str) -> str:
    return f"{GENERATIONS}/{generation_id}/Download"


def generation_rerun(generation_id: str) -> str:
    return f"{GENERATIONS}/{generation_id}/Rerun"


def join_route_prefix(route_prefix: str | None, relative: str) -> str:
    relative = relative.lstrip("/")
    prefix = (route_prefix or "").strip().strip("/")
    return f"/{prefix}/{relative}" if prefix else f"/{relative}"
