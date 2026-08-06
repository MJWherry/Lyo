"""Matrix cell identity — mirrors k6/framework-person/lib/matrixCell.js."""

from __future__ import annotations

from dataclasses import dataclass

from .axes import MatrixAxes


@dataclass(frozen=True, slots=True)
class MatrixCell:
    endpoint: str
    profile: str
    intensity: str
    cache_mode: str
    seed: int = MatrixAxes.MATRIX_SEED

    def __post_init__(self) -> None:
        if self.endpoint not in MatrixAxes.ENDPOINTS:
            raise ValueError(f"Unknown endpoint '{self.endpoint}'")
        if self.profile not in MatrixAxes.PROFILES:
            raise ValueError(f"Unknown profile '{self.profile}'")
        if self.intensity not in MatrixAxes.INTENSITIES:
            raise ValueError(f"Unknown intensity '{self.intensity}'")
        if self.cache_mode not in MatrixAxes.CACHE_MODES:
            raise ValueError(f"Unknown cache_mode '{self.cache_mode}'")

    @property
    def cell_id(self) -> str:
        return f"{self.endpoint}_{self.profile}_{self.intensity}_{self.cache_mode}"

    @property
    def scenario_file(self) -> str:
        return f"{self.endpoint}_{self.profile}.js"

    @property
    def is_cached(self) -> bool:
        return self.cache_mode == "cached"

    def to_env(self) -> dict[str, str]:
        return {
            "INTENSITY": self.intensity,
            "CACHE_MODE": self.cache_mode,
            "CACHE_HIT_MODE": "true" if self.is_cached else "false",
            "RANDOM_SEED": str(self.seed),
        }
