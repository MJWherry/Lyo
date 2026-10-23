"""Config resolve prefix and query-host routes."""

DEFINITION = "Config/Definition"
BINDING = "Config/Binding"


def config_prefix(prefix: str = "/api/config") -> str:
    """Normalize a config prefix so it has one leading slash and no trailing slash."""
    trimmed = (prefix or "").rstrip("/")
    return trimmed if trimmed.startswith("/") else f"/{trimmed}"
