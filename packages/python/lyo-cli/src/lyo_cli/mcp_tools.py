"""MCP tool functions. The stdio server registers these. Write tools stay off unless ``LYO_MCP_WRITES=1``."""

from __future__ import annotations

import os
from typing import Any, Mapping

from lyo_api_client import ApiClient

READ_TOOLS = ("get_metadata", "query_project", "get_entity")
WRITE_TOOLS = ("create_entity", "patch_entity", "update_entity", "delete_entity")
MAX_AMOUNT = 100

QUERY_PROJECT_DESCRIPTION = """POST {route}/QueryProject. Select is required. Amount above 100 is rejected before the call.

whereClause example:
{"$type": "group", "Operator": "And", "Children": [{"$type": "condition", "Field": "Name", "Comparison": "Equals", "Value": "a"}]}
"""


class QueryRejected(ValueError):
    """The projection request is missing Select or asks for more than 100 rows."""


def writes_enabled(environ: Mapping[str, str] | None = None) -> bool:
    """True only when ``LYO_MCP_WRITES`` is ``1``."""
    env = os.environ if environ is None else environ
    return env.get("LYO_MCP_WRITES") == "1"


def tool_names(enabled: bool) -> tuple[str, ...]:
    """Tool names to register. Write tools are included only when ``enabled`` is true."""
    if enabled:
        return READ_TOOLS + WRITE_TOOLS
    return READ_TOOLS


def query_project_tool(client: ApiClient, route: str, request: dict[str, Any]) -> Any:
    """Run QueryProject after rejecting a missing Select or an Amount above 100."""
    select = request.get("Select") if isinstance(request, dict) else None
    if not isinstance(select, list) or not select:
        raise QueryRejected("Select is required.")
    amount = request.get("Amount")
    if isinstance(amount, (int, float)) and not isinstance(amount, bool) and amount > MAX_AMOUNT:
        raise QueryRejected("Amount must be 100 or less.")
    return client.query_project(route, request).data


def get_metadata_tool(client: ApiClient, route: str, entity_type: str | None = None) -> Any:
    """``GET {route}/Metadata``, or one entity when ``entity_type`` is set."""
    if entity_type:
        return client.get_entity_metadata(route, entity_type).data
    return client.get_metadata(route).data


def get_entity_tool(client: ApiClient, route: str, entity_id: str, include: list[str] | None = None) -> Any:
    """``GET {route}/{id}`` with optional include paths."""
    return client.get_by_id(route, entity_id, include).data


def create_entity(client: ApiClient, route: str, body: dict[str, Any]) -> Any:
    """``POST {route}``."""
    return client.create(route, body).data


def patch_entity(client: ApiClient, route: str, body: dict[str, Any]) -> Any:
    """``PATCH {route}``."""
    return client.patch(route, body).data


def update_entity(client: ApiClient, route: str, entity_id: str, body: dict[str, Any]) -> Any:
    """``POST {route}/Update``."""
    return client.update(route, entity_id, body).data


def delete_entity(client: ApiClient, route: str, entity_id: str) -> Any:
    """``DELETE {route}/{id}``."""
    return client.delete_by_id(route, entity_id).data
