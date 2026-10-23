"""Auth request helpers. Snake_case attributes; camelCase ``to_dict()``."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

_SECRET_KEYS = frozenset({"secret", "secretHash", "plaintext"})


def token_for_list(token: dict[str, Any]) -> dict[str, Any]:
    """Drop secret material so a list or log line cannot reprint a token."""
    return {key: value for key, value in token.items() if key not in _SECRET_KEYS}


@dataclass
class CreateTokenRequest:
    """Body of ``POST /tokens``."""

    display_name: str
    scopes: list[str] = field(default_factory=list)
    lifetime_seconds: int | None = None
    metadata: dict[str, Any] | None = None
    kind: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"displayName": self.display_name}
        if self.scopes:
            out["scopes"] = list(self.scopes)
        if self.lifetime_seconds is not None:
            out["lifetimeSeconds"] = self.lifetime_seconds
        if self.metadata is not None:
            out["metadata"] = self.metadata
        if self.kind is not None:
            out["kind"] = self.kind
        return out
