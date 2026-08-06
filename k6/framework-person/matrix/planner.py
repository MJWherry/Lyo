"""Keyword filter → cartesian product of MatrixCell."""

from __future__ import annotations

import os

from .axes import MatrixAxes
from .cell import MatrixCell


def _normalize(keyword: str) -> str:
    return "".join(keyword.split()).lower()


def _normalize_cache_keyword(keyword: str) -> str | None:
    if keyword in ("cached", "cache-hit", "cachehit"):
        return "cached"
    if keyword in ("uncached", "miss", "cache-miss", "cachemiss"):
        return "uncached"
    return None


class MatrixPlanner:
    """Plan matrix cells from CLI/env keywords.

    Axis groups are AND-ed (intensity, cache, endpoint, profile). Within a group,
    keywords are OR-ed. Missing a group expands the full axis.
    """

    def __init__(self, seed: int = MatrixAxes.MATRIX_SEED) -> None:
        self.seed = seed

    def collect_keywords(self, cli: list[str], env_filter: str = "") -> list[str]:
        out: list[str] = []
        if env_filter.strip():
            for part in env_filter.split(","):
                n = _normalize(part)
                if n:
                    out.append(n)
        for arg in cli:
            for part in arg.split(","):
                n = _normalize(part)
                if n:
                    out.append(n)
        return out

    def plan(self, keywords: list[str]) -> list[MatrixCell]:
        intensities = self._resolve_intensities(keywords)
        cache_modes = self._resolve_cache_modes(keywords)
        scenario_files = self._select_scenario_files(keywords)

        cells: list[MatrixCell] = []
        for scenario_file in scenario_files:
            endpoint, profile = self._parse_scenario_file(scenario_file)
            for intensity in intensities:
                for cache_mode in cache_modes:
                    cells.append(
                        MatrixCell(
                            endpoint=endpoint,
                            profile=profile,
                            intensity=intensity,
                            cache_mode=cache_mode,
                            seed=self.seed,
                        )
                    )
        return cells

    def _resolve_intensities(self, keywords: list[str]) -> tuple[str, ...]:
        selected = tuple(k for k in keywords if k in MatrixAxes.INTENSITY_KEYWORDS)
        return selected or MatrixAxes.INTENSITIES

    def _resolve_cache_modes(self, keywords: list[str]) -> tuple[str, ...]:
        selected: list[str] = []
        for k in keywords:
            mode = _normalize_cache_keyword(k)
            if mode and mode not in selected:
                selected.append(mode)
        if selected:
            return tuple(selected)

        # Backward compat: explicit CACHE_HIT_MODE without cache keywords → single mode.
        raw = os.environ.get("CACHE_HIT_MODE", "").strip().lower()
        if raw in ("1", "true", "yes", "y"):
            return ("cached",)
        if raw in ("0", "false", "no", "n"):
            return ("uncached",)

        raw_mode = os.environ.get("CACHE_MODE", "").strip().lower()
        if raw_mode in MatrixAxes.CACHE_MODES:
            return (raw_mode,)

        return MatrixAxes.CACHE_MODES

    def _select_scenario_files(self, keywords: list[str]) -> list[str]:
        endpoints = {
            MatrixAxes.ENDPOINT_ALIASES[k] for k in keywords if k in MatrixAxes.ENDPOINT_ALIASES
        }
        profiles = {k for k in keywords if k in MatrixAxes.PROFILE_KEYWORDS}
        exclude_soak = any(k in ("nonsoak", "no-soak", "nosoak") for k in keywords)
        substrings = [
            k
            for k in keywords
            if k not in MatrixAxes.INTENSITY_KEYWORDS
            and _normalize_cache_keyword(k) is None
            and k not in MatrixAxes.PROFILE_KEYWORDS
            and k not in MatrixAxes.ENDPOINT_ALIASES
            and k not in ("nonsoak", "no-soak", "nosoak", "all", "matrix")
        ]

        files: list[str] = []
        for test_file in MatrixAxes.SCENARIO_FILES:
            test_name = test_file.removesuffix(".js")
            endpoint, profile = self._parse_scenario_file(test_file)
            if endpoints and endpoint not in endpoints:
                continue
            if profiles and profile not in profiles:
                continue
            if exclude_soak and profile == "soak":
                continue
            if substrings and not any(s in test_name or s in test_file for s in substrings):
                continue
            files.append(test_file)
        return files

    @staticmethod
    def _parse_scenario_file(scenario_file: str) -> tuple[str, str]:
        name = scenario_file.removesuffix(".js")
        for endpoint in sorted(MatrixAxes.ENDPOINTS, key=len, reverse=True):
            prefix = f"{endpoint}_"
            if name.startswith(prefix):
                return endpoint, name[len(prefix) :]
        raise ValueError(f"Cannot parse scenario file '{scenario_file}'")
