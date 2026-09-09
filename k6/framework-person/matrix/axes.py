"""Canonical matrix axes — keep in sync with k6/framework-person/lib/matrixAxes.js."""

from __future__ import annotations


class MatrixAxes:
    MATRIX_SEED = 20260623

    ENDPOINTS = ("query", "queryproject", "queryroot")
    PROFILES = ("load", "stress", "spike", "soak", "ceiling")
    INTENSITIES = ("low", "med", "high")
    CACHE_MODES = ("uncached", "cached")

    INTENSITY_KEYWORDS = frozenset(("low", "med", "high"))
    CACHE_KEYWORDS = frozenset(
        ("uncached", "cached", "cache-hit", "cachehit", "miss", "cache-miss", "cachemiss")
    )
    PROFILE_KEYWORDS = frozenset(("load", "stress", "spike", "soak", "ceiling"))
    ENDPOINT_ALIASES = {
        "query": "query",
        "queryproject": "queryproject",
        "projected": "queryproject",
        "projection": "queryproject",
        "queryroot": "queryroot",
        "rootquery": "queryroot",
        "root": "queryroot",
    }

    SCENARIO_FILES: tuple[str, ...] = ()


MatrixAxes.SCENARIO_FILES = tuple(
    f"{endpoint}_{profile}.js"
    for endpoint in MatrixAxes.ENDPOINTS
    for profile in MatrixAxes.PROFILES
)
