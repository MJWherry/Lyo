""".NET repo parsing helpers shared by the tooling scripts.

Covers the bits both gen_graph.py and build_manifests.py need: NuGet version
range normalization, Central Package Management (Directory.Packages.props)
lookup, MSBuild TargetFramework condition handling, and csproj discovery.
"""

from __future__ import annotations

import functools
import re
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

# Central Package Management: csproj PackageReference entries are version-less; versions live
# in this props file as ranges (e.g. `[1.2.3,)`) and are resolved from here.
CENTRAL_PACKAGES_PROPS = REPO_ROOT / "Lyo.Net" / "Directory.Packages.props"

NS_RE = re.compile(r"\sxmlns=\"[^\"]+\"")

# Match `'$(TargetFramework)' == 'netstandard2.0'` and friends.
# The closing `'` after `)` is optional to stay tolerant of variants.
_TFM_EQ_RE = re.compile(
    r"\$\(\s*TargetFramework\s*\)\s*'?\s*==\s*'?([\w.+-]+)'?"
)
_TFM_NEQ_RE = re.compile(
    r"\$\(\s*TargetFramework\s*\)\s*'?\s*!=\s*'?([\w.+-]+)'?"
)


def parse_msbuild_xml(path: Path) -> ET.Element:
    """Parse an MSBuild XML file with any default xmlns stripped, so element
    lookups work without namespace prefixes. Raises ET.ParseError on bad XML."""
    text = NS_RE.sub("", path.read_text(encoding="utf-8"), count=1)
    return ET.fromstring(text)


def normalize_nuget_version(version: str) -> str:
    """Reduce a NuGet range to its declared minimum ('[1.2.3,)' / '[1.2.3,2.0.0)' -> '1.2.3').

    Exact pins ('5.0.0') and floating versions ('2.*') pass through unchanged.
    """
    v = version.strip()
    if v.startswith(("[", "(")):
        lower = v[1:].split(",", 1)[0].strip().rstrip("])")
        return lower or v
    return v


@functools.lru_cache(maxsize=1)
def load_central_package_versions() -> dict[str, str]:
    """Package -> declared minimum version from Directory.Packages.props."""
    if not CENTRAL_PACKAGES_PROPS.is_file():
        print(f"warning: {CENTRAL_PACKAGES_PROPS} not found; package versions unavailable.")
        return {}
    try:
        root = parse_msbuild_xml(CENTRAL_PACKAGES_PROPS)
    except ET.ParseError as exc:
        print(f"warning: could not parse {CENTRAL_PACKAGES_PROPS}: {exc}")
        return {}
    versions: dict[str, str] = {}
    for node in root.iter("PackageVersion"):
        name = node.attrib.get("Include")
        version = node.attrib.get("Version")
        if name and version:
            versions[name] = normalize_nuget_version(version)
    return versions


def framework_label(condition: str) -> str:
    """Reduce an MSBuild Condition string to a short framework label."""
    if not condition:
        return "all"
    m = _TFM_EQ_RE.search(condition)
    if m:
        return m.group(1)
    m = _TFM_NEQ_RE.search(condition)
    if m:
        return f"!{m.group(1)}"
    short = condition.strip().strip("'\"")
    return short[:40] + ("..." if len(short) > 40 else "")


def tfm_condition_applies(condition: str | None, tfm: str) -> bool:
    """Whether an ItemGroup Condition applies when building for `tfm`.

    Conditions not mentioning TargetFramework are treated as applying (we don't
    evaluate arbitrary MSBuild expressions).
    """
    if not condition:
        return True
    if "TargetFramework" in condition:
        return tfm in condition
    return True


def read_target_frameworks(csproj: Path) -> list[str]:
    """Return TargetFramework / TargetFrameworks from a csproj (stable order)."""
    if not csproj.is_file():
        return []
    try:
        root = parse_msbuild_xml(csproj)
    except ET.ParseError:
        return []
    for tag in ("TargetFrameworks", "TargetFramework"):
        for el in root.iter(tag):
            text = (el.text or "").strip()
            if not text:
                continue
            # Prefer first non-empty PropertyGroup declaration.
            frameworks = [t.strip() for t in text.split(";") if t.strip()]
            if frameworks:
                # De-dupe preserving order
                seen: set[str] = set()
                out: list[str] = []
                for fw in frameworks:
                    key = fw.casefold()
                    if key in seen:
                        continue
                    seen.add(key)
                    out.append(fw)
                return out
    return []


def find_project_csproj(project_dir: Path) -> Path | None:
    """Locate a project's csproj, preferring `<dirname>.csproj`."""
    preferred = project_dir / f"{project_dir.name}.csproj"
    if preferred.is_file():
        return preferred
    matches = sorted(project_dir.glob("*.csproj"))
    return matches[0] if matches else None
