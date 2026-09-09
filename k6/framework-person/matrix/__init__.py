"""OOP orchestration for the framework-person intensity × cache k6 matrix."""

from .axes import MatrixAxes
from .cell import MatrixCell
from .k6_compat import rewrite_bare_imports_for_k6
from .planner import MatrixPlanner
from .runner import K6ProcessRunner

__all__ = [
    "MatrixAxes",
    "MatrixCell",
    "MatrixPlanner",
    "K6ProcessRunner",
    "rewrite_bare_imports_for_k6",
]
