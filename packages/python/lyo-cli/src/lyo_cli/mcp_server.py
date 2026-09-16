"""stdio MCP server. Reads the same profile as ``lyo``. Write tools register only when ``LYO_MCP_WRITES=1``."""

from __future__ import annotations

from typing import Any

from lyo_api_client import ApiClient
from lyo_api_client.transport import UrllibTransport

from .mcp_tools import (
    QUERY_PROJECT_DESCRIPTION,
    create_entity,
    delete_entity,
    get_entity_tool,
    get_metadata_tool,
    patch_entity,
    query_project_tool,
    tool_names,
    update_entity,
    writes_enabled,
)
from .profile import MissingProfileError, default_profile_path, resolve_profile
from .session import connect


def main() -> None:
    """Serve Lyo query tools over stdio. Install ``lyo-cli[mcp]`` first."""
    try:
        from mcp.server.mcpserver import MCPServer
    except ImportError as exc:
        raise SystemExit("Install the mcp extra: pip install 'lyo-cli[mcp]'") from exc
    path = default_profile_path()
    try:
        profile = resolve_profile(path=path)
    except MissingProfileError as exc:
        raise SystemExit(str(exc)) from exc
    client = connect(profile, transport=UrllibTransport(), profile_path=path)
    server = MCPServer("lyo")
    register(server, client, writes_enabled())
    server.run(transport="stdio")


def register(server: Any, client: ApiClient, enabled: bool) -> None:
    """Register the tools named by ``tool_names(enabled)`` on ``server``."""
    names = set(tool_names(enabled))

    def get_metadata(route: str, entity_type: str | None = None) -> Any:
        """GET {route}/Metadata, or GET {route}/{entity_type}/Metadata when entity_type is set."""
        return get_metadata_tool(client, route, entity_type)

    def query_project(route: str, request: dict[str, Any]) -> Any:
        return query_project_tool(client, route, request)

    def get_entity(route: str, entity_id: str, include: list[str] | None = None) -> Any:
        """GET {route}/{entity_id}. include is a list of navigation paths."""
        return get_entity_tool(client, route, entity_id, include)

    server.tool()(get_metadata)
    server.tool(description=QUERY_PROJECT_DESCRIPTION)(query_project)
    server.tool()(get_entity)
    if "create_entity" not in names:
        return

    def create_entity_tool(route: str, body: dict[str, Any]) -> Any:
        """POST {route} with the JSON body."""
        return create_entity(client, route, body)

    def patch_entity_tool(route: str, body: dict[str, Any]) -> Any:
        """PATCH {route} with a PatchRequest body."""
        return patch_entity(client, route, body)

    def update_entity_tool(route: str, entity_id: str, body: dict[str, Any]) -> Any:
        """POST {route}/Update for one id."""
        return update_entity(client, route, entity_id, body)

    def delete_entity_tool(route: str, entity_id: str) -> Any:
        """DELETE {route}/{entity_id}."""
        return delete_entity(client, route, entity_id)

    server.tool(name="create_entity")(create_entity_tool)
    server.tool(name="patch_entity")(patch_entity_tool)
    server.tool(name="update_entity")(update_entity_tool)
    server.tool(name="delete_entity")(delete_entity_tool)
