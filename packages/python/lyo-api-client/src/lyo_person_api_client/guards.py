"""Runtime response guards for lightweight contract validation.

The API returns plain JSON; these guards check that a parsed response payload
has the expected QueryRes / ProjectedQueryRes shape before you rely on it.
"""

from __future__ import annotations

from typing import Any


def is_query_res(value: Any) -> bool:
    """Whether ``value`` looks like a QueryRes payload (entity query response)."""
    if not isinstance(value, dict):
        return False
    if not isinstance(value.get("isSuccess"), bool) or "queryRequest" not in value:
        return False
    if "items" not in value:
        return True
    return value["items"] is None or isinstance(value["items"], list)


def is_projected_query_res(value: Any) -> bool:
    """Whether ``value`` looks like a ProjectedQueryRes payload (projected rows)."""
    if not isinstance(value, dict):
        return False
    if not isinstance(value.get("isSuccess"), bool) or "queryRequest" not in value:
        return False
    if "items" not in value or value["items"] is None:
        return True
    items = value["items"]
    return isinstance(items, list) and all(row is None or isinstance(row, dict) for row in items)
