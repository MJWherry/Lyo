"""Automation run request and response models.

Snake_case attributes. ``to_dict()`` emits the camelCase wire JSON the API expects.
Response helpers read that same camelCase payload.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Literal

RunStatus = Literal["queued", "running", "completed", "failed", "cancelled"]
_SETTLED = frozenset({"completed", "failed", "cancelled"})


def is_settled(status: str) -> bool:
    """True when a run will not change again."""
    return status in _SETTLED


@dataclass
class AutomationRunReq:
    """Body of ``POST Automation/Run``. ``plan`` is the automation document."""

    plan: Any
    browser_kind: str | None = None
    headless: bool | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"plan": self.plan}
        if self.browser_kind is not None:
            out["browserKind"] = self.browser_kind
        if self.headless is not None:
            out["headless"] = self.headless
        return out


@dataclass
class AutomationRunAccepted:
    """``202`` body from starting a run."""

    run_id: str
    status: str

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationRunAccepted:
        return cls(run_id=str(data["runId"]), status=str(data.get("status", "queued")))


@dataclass
class AutomationPlanContextSnapshot:
    """Variables and context bag after one step."""

    strings: dict[str, str] | None = None
    string_lists: dict[str, list[str]] | None = None
    context_items: dict[str, str | None] | None = None
    element_refs: list[str] | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any] | None) -> AutomationPlanContextSnapshot | None:
        if data is None:
            return None
        return cls(
            strings=data.get("strings"),
            string_lists=data.get("stringLists"),
            context_items=data.get("contextItems"),
            element_refs=data.get("elementRefs"),
        )


@dataclass
class AutomationRunStepRes:
    """One finished step on a run."""

    index: int
    type: str
    outcome: str
    duration_ms: int = 0
    name: str | None = None
    error: str | None = None
    screenshot_file_id: str | None = None
    context: AutomationPlanContextSnapshot | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationRunStepRes:
        return cls(
            index=int(data["index"]),
            type=str(data.get("type", "")),
            outcome=str(data.get("outcome", "")),
            duration_ms=int(data.get("durationMs", 0)),
            name=data.get("name"),
            error=data.get("error"),
            screenshot_file_id=data.get("screenshotFileId"),
            context=AutomationPlanContextSnapshot.from_dict(data.get("context")),
        )


@dataclass
class AutomationRunRes:
    """``GET Automation/Run/{id}`` body."""

    run_id: str
    status: str
    error: str | None = None
    strings: dict[str, str] | None = None
    string_lists: dict[str, list[str]] | None = None
    steps: list[AutomationRunStepRes] | None = None

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> AutomationRunRes:
        raw_steps = data.get("steps") or []
        return cls(
            run_id=str(data["runId"]),
            status=str(data["status"]),
            error=data.get("error"),
            strings=data.get("strings"),
            string_lists=data.get("stringLists"),
            steps=[AutomationRunStepRes.from_dict(step) for step in raw_steps],
        )
