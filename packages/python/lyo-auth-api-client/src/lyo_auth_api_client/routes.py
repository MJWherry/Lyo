"""REST paths for session auth and the admin Auth resources."""

from urllib.parse import quote

USER = "Auth/User"
TOKEN = "Auth/Token"
CLAIM = "Auth/Claim"
SCOPE = "Auth/Scope"
LINKED_IDENTITY = "Auth/LinkedIdentity"
EVENT = "Auth/Event"


def login_url(provider: str, return_url: str | None = None, prefix: str = "/auth") -> str:
    """Browser redirect for ``GET /auth/login/{provider}``."""
    path = f"{prefix.rstrip('/')}/login/{quote(provider, safe='')}"
    if not return_url:
        return path
    return f"{path}?returnUrl={quote(return_url, safe='')}"
