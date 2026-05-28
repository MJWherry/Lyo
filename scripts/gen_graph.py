"""Generate an interactive HTML view of project references in Lyo.slnx.

Output:
  Lyo.ProjectGraph.html  - self-contained interactive viewer (Cytoscape + dagre)

Tests/benchmarks are excluded by default. Pass --include-tests to keep them.
"""

from __future__ import annotations

import argparse
import json
import re
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
SLNX = REPO_ROOT / "Lyo.Net" / "Lyo.slnx"
OUT_HTML = REPO_ROOT / "Lyo.ProjectGraph.html"
NS_RE = re.compile(r"\sxmlns=\"[^\"]+\"")

# Match `'$(TargetFramework)' == 'netstandard2.0'` and friends.
# The closing `'` after `)` is optional to stay tolerant of variants.
_TFM_EQ_RE = re.compile(
    r"\$\(\s*TargetFramework\s*\)\s*'?\s*==\s*'?([\w.+-]+)'?"
)
_TFM_NEQ_RE = re.compile(
    r"\$\(\s*TargetFramework\s*\)\s*'?\s*!=\s*'?([\w.+-]+)'?"
)


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


# ---------------------------------------------------------------------------
# Data model
# ---------------------------------------------------------------------------


@dataclass
class Project:
    name: str
    folder: str
    rel_path: str
    abs_path: Path
    refs: list[str] = field(default_factory=list)
    # (package, version, framework_label)
    package_refs: list[tuple[str, str, str]] = field(default_factory=list)


def load_slnx_projects() -> list[Project]:
    text = NS_RE.sub("", SLNX.read_text(encoding="utf-8"), count=1)
    root = ET.fromstring(text)
    base = SLNX.parent
    projects: list[Project] = []
    for folder in root.iter("Folder"):
        folder_name = folder.attrib.get("Name", "/")
        for proj in folder.findall("Project"):
            rel = proj.attrib["Path"].replace("\\", "/")
            abs_path = (base / rel).resolve()
            projects.append(
                Project(
                    name=abs_path.stem,
                    folder=folder_name,
                    rel_path=rel,
                    abs_path=abs_path,
                )
            )
    return projects


def parse_refs(p: Project) -> None:
    if not p.abs_path.exists():
        return
    text = NS_RE.sub("", p.abs_path.read_text(encoding="utf-8"), count=1)
    try:
        root = ET.fromstring(text)
    except ET.ParseError:
        return
    seen: set[str] = set()
    for ref in root.iter("ProjectReference"):
        include = ref.attrib.get("Include")
        if not include:
            continue
        target_name = Path(include.replace("\\", "/")).stem
        if target_name == p.name or target_name in seen:
            continue
        seen.add(target_name)
        p.refs.append(target_name)

    # NuGet / external packages. We iterate ItemGroups (not just the inner
    # PackageReference nodes) so we can capture an enclosing
    # `Condition="'$(TargetFramework)' == 'netstandard2.0'"` and bucket packages
    # per target framework. Version may be missing under Central Package
    # Management; store empty string in that case.
    pkg_seen: set[tuple[str, str, str]] = set()
    for ig in root.iter("ItemGroup"):
        ig_cond = ig.attrib.get("Condition", "") or ""
        for pkg in ig.findall("PackageReference"):
            include = pkg.attrib.get("Include")
            if not include:
                continue
            version = pkg.attrib.get("Version", "")
            if not version:
                v_el = pkg.find("Version")
                if v_el is not None and v_el.text:
                    version = v_el.text.strip()
            pkg_cond = pkg.attrib.get("Condition", "") or ""
            combined = " ".join(c for c in (ig_cond, pkg_cond) if c)
            fw = framework_label(combined)
            key = (include, version, fw)
            if key in pkg_seen:
                continue
            pkg_seen.add(key)
            p.package_refs.append(key)


def is_test_like(name: str) -> bool:
    lower = name.lower()
    return lower.endswith(".tests") or lower.endswith(".benchmarks")


def top_area(folder: str) -> str:
    parts = [p for p in folder.split("/") if p]
    return parts[0] if parts else "Other"


AREA_ORDER = [
    "Core",
    "Data",
    "Security",
    "Communication",
    "Integration",
    "Features",
    "Apps",
    "Tools",
]
AREA_FILL = {
    "Core": "#DAE8FC",
    "Data": "#D5E8D4",
    "Communication": "#FFE6CC",
    "Security": "#F8CECC",
    "Integration": "#E1D5E7",
    "Apps": "#FFF2CC",
    "Features": "#D4E1F5",
    "Tools": "#F5F5F5",
}
AREA_STROKE = {
    "Core": "#6C8EBF",
    "Data": "#82B366",
    "Communication": "#D79B00",
    "Security": "#B85450",
    "Integration": "#9673A6",
    "Apps": "#D6B656",
    "Features": "#7A95C8",
    "Tools": "#666666",
}


# ---------------------------------------------------------------------------
# Build serializable graph data
# ---------------------------------------------------------------------------


def _transitive_projects(p: Project, name_to_proj: dict[str, Project]) -> list[str]:
    """All Lyo projects reachable from `p` via ProjectReference (excluding `p`)."""
    visited: set[str] = set()
    stack: list[str] = [r for r in p.refs if r in name_to_proj and r != p.name]
    while stack:
        cur = stack.pop()
        if cur in visited:
            continue
        visited.add(cur)
        nxt = name_to_proj.get(cur)
        if nxt is None:
            continue
        for r in nxt.refs:
            if r in name_to_proj and r != p.name and r not in visited:
                stack.append(r)
    return sorted(visited)


def _framework_sort_key(fw: str) -> tuple:
    if fw == "all":
        return (0, "")
    if fw.startswith("!"):
        return (2, fw.lower())
    return (1, fw.lower())


def _aggregate_packages(
    names: list[str], name_to_proj: dict[str, Project]
) -> list[dict]:
    """Group package refs across `names` by framework label, then by package."""
    # framework -> package -> set(versions)
    by_fw: dict[str, dict[str, set[str]]] = defaultdict(lambda: defaultdict(set))
    for n in names:
        proj = name_to_proj.get(n)
        if proj is None:
            continue
        for pkg, ver, fw in proj.package_refs:
            by_fw[fw][pkg].add(ver)

    out: list[dict] = []
    for fw in sorted(by_fw, key=_framework_sort_key):
        pkgs: list[dict] = []
        for pkg in sorted(by_fw[fw]):
            versions = sorted(v for v in by_fw[fw][pkg] if v)
            if not versions:
                versions = [""]
            pkgs.append({"name": pkg, "versions": versions})
        out.append({"framework": fw, "packages": pkgs})
    return out


def build_graph_data(projects: list[Project]) -> dict:
    name_to_proj = {p.name: p for p in projects}
    by_area: dict[str, int] = defaultdict(int)
    for p in projects:
        by_area[top_area(p.folder)] += 1

    # Per-project transitive Lyo closure (sorted by area then name) and
    # transitive package union.
    area_of = {p.name: top_area(p.folder) for p in projects}
    transitive_lyo: dict[str, list[str]] = {}
    transitive_pkgs: dict[str, list[dict]] = {}
    for p in projects:
        names = _transitive_projects(p, name_to_proj)
        names.sort(key=lambda n: (
            AREA_ORDER.index(area_of[n]) if area_of.get(n) in AREA_ORDER else 99,
            n,
        ))
        transitive_lyo[p.name] = names
        transitive_pkgs[p.name] = _aggregate_packages([p.name] + names, name_to_proj)

    serial_projects = [
        {
            "name": p.name,
            "area": top_area(p.folder),
            "folder": p.folder.strip("/") or "(root)",
            "refs": [r for r in p.refs if r in name_to_proj],
            "directPackages": _aggregate_packages([p.name], name_to_proj),
            "transitiveLyo": transitive_lyo[p.name],
            "transitivePackages": transitive_pkgs[p.name],
        }
        for p in projects
    ]

    area_edges_w: dict[tuple[str, str], int] = defaultdict(int)
    for p in projects:
        sa = top_area(p.folder)
        for r in p.refs:
            tp = name_to_proj.get(r)
            if tp is None:
                continue
            ta = top_area(tp.folder)
            if sa != ta:
                area_edges_w[(sa, ta)] += 1

    areas_used = [a for a in AREA_ORDER if a in by_area]
    return {
        "projects": serial_projects,
        "areas": areas_used,
        "byArea": {a: by_area[a] for a in areas_used},
        "areaEdges": [[s, t, w] for (s, t), w in sorted(area_edges_w.items())],
        "colors": {a: AREA_STROKE[a] for a in areas_used},
        "fills": {a: AREA_FILL[a] for a in areas_used},
    }


# ---------------------------------------------------------------------------
# Interactive HTML
# ---------------------------------------------------------------------------


HTML_TEMPLATE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8"/>
<title>Lyo Project Graph</title>
<script src="https://unpkg.com/cytoscape@3.30.4/dist/cytoscape.min.js"></script>
<script src="https://unpkg.com/dagre@0.8.5/dist/dagre.min.js"></script>
<script src="https://unpkg.com/cytoscape-dagre@2.5.0/cytoscape-dagre.js"></script>
<style>
  :root {
    --bg: #1b1d22;
    --bg-elev: #24272e;
    --border: #3a3f48;
    --text: #e6e8ec;
    --text-dim: #9aa0a8;
    --accent: #7aa2f7;
    --sidebar-w: 260px;
    --sidebar-collapsed-w: 32px;
  }
  * { box-sizing: border-box; }
  html, body { margin: 0; padding: 0; height: 100%; }
  body {
    font-family: ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
    background: var(--bg); color: var(--text);
    display: flex; flex-direction: column;
  }
  header {
    padding: 10px 16px;
    background: var(--bg-elev);
    border-bottom: 1px solid var(--border);
    display: flex; gap: 12px; align-items: center;
    flex-wrap: wrap;
  }
  h1 { font-size: 14px; font-weight: 600; margin: 0; letter-spacing: 0.02em; }
  .crumb { color: var(--text-dim); font-size: 13px; }
  .crumb b { color: var(--text); }
  button, select, input {
    background: #2c3038; color: var(--text);
    border: 1px solid var(--border); border-radius: 6px;
    padding: 5px 10px; font-size: 13px; font-family: inherit;
  }
  button { cursor: pointer; }
  button:hover { border-color: var(--accent); }
  input { width: 220px; }
  #status { color: var(--text-dim); font-size: 12px; margin-left: auto; }
  #workspace {
    display: flex; flex: 1; min-height: 0; min-width: 0;
  }
  #graph-col {
    flex: 1; min-width: 0; display: flex; flex-direction: column;
  }
  #cy { flex: 1; min-height: 0; background: #15171b; }
  #legend {
    display: flex; gap: 10px; flex-wrap: wrap;
    padding: 6px 16px; background: var(--bg-elev);
    border-top: 1px solid var(--border);
    font-size: 12px;
    flex-shrink: 0;
  }
  .swatch {
    display: inline-block; width: 10px; height: 10px;
    border-radius: 2px; margin-right: 6px; vertical-align: middle;
  }
  #sidebar {
    flex-shrink: 0;
    width: var(--sidebar-w);
    display: flex; flex-direction: row;
    background: var(--bg-elev);
    border-left: 1px solid var(--border);
    transition: width 0.2s ease;
    overflow: hidden;
  }
  #sidebar.collapsed { width: var(--sidebar-collapsed-w); }
  #sidebar.resizing { transition: none; }
  #sidebar-resize {
    flex-shrink: 0;
    width: 6px;
    cursor: col-resize;
    touch-action: none;
    background: transparent;
    position: relative;
  }
  #sidebar-resize::before {
    content: '';
    position: absolute;
    left: 2px;
    top: 0;
    bottom: 0;
    width: 2px;
    background: var(--border);
    opacity: 0.6;
    transition: opacity 0.15s, background 0.15s;
  }
  #sidebar-resize:hover::before,
  #sidebar.resizing #sidebar-resize::before {
    opacity: 1;
    background: var(--accent);
  }
  #sidebar:not(.resizable) #sidebar-resize,
  #sidebar.collapsed #sidebar-resize { display: none; }
  #sidebar-toggle {
    flex-shrink: 0;
    width: var(--sidebar-collapsed-w);
    border: none;
    border-right: 1px solid var(--border);
    background: #2c3038;
    color: var(--text-dim);
    cursor: pointer;
    font-size: 16px;
    line-height: 1;
    padding: 0;
    border-radius: 0;
  }
  #sidebar-toggle:hover { color: var(--accent); background: #343840; }
  #info {
    flex: 1; min-width: 0;
    display: flex; flex-direction: column;
    font-size: 12px; line-height: 1.5;
    padding: 10px 10px;
    overflow: hidden;
  }
  #info h3 {
    margin: 0; font-size: 13px; color: var(--accent);
    word-break: break-word;
    line-height: 1.3;
  }
  #sidebar.collapsed #info { display: none; }
  #info .sidebar-empty {
    color: var(--text-dim); font-style: italic;
    font-size: 12px; padding: 8px 4px;
    line-height: 1.5;
  }
  #info .meta { color: var(--text-dim); font-size: 11px; margin-bottom: 8px; }
  #info .header-row {
    display: flex; align-items: center; gap: 8px;
    margin: 0 0 4px 0;
  }
  #info .back {
    background: transparent; color: var(--text-dim);
    border: 1px solid var(--border); border-radius: 6px;
    padding: 2px 8px; cursor: pointer; font-size: 12px;
    font-family: inherit; line-height: 1.2;
  }
  #info .back:hover { color: var(--accent); border-color: var(--accent); }
  #info .back[hidden] { display: none; }
  #info ul { margin: 4px 0 0 16px; padding: 0; }
  #info li { margin: 1px 0; }
  #info .empty { color: var(--text-dim); font-style: italic; }
  #info .tabs {
    display: flex; gap: 4px; margin-bottom: 8px; flex-wrap: wrap;
    border-bottom: 1px solid var(--border); padding-bottom: 6px;
  }
  #info .tab {
    background: transparent; color: var(--text-dim);
    border: 1px solid transparent; border-radius: 6px;
    padding: 3px 8px; font-size: 12px; cursor: pointer;
    font-family: inherit;
  }
  #info .tab:hover { color: var(--text); }
  #info .tab.active {
    color: var(--text); background: #2c3038;
    border-color: var(--accent);
  }
  #info .tabpanel {
    overflow-y: auto; flex: 1 1 auto; min-height: 0;
    padding-right: 2px;
  }
  #info .pkg-row {
    display: flex; gap: 6px; align-items: baseline;
    flex-wrap: wrap; margin: 2px 0;
  }
  #info .pkg-name { color: var(--text); }
  #info .proj-link {
    cursor: pointer; color: var(--accent);
    text-decoration: none; border-bottom: 1px dotted transparent;
  }
  #info .proj-link:hover { border-bottom-color: var(--accent); }
  #info .proj-link:focus { outline: 1px dashed var(--accent); outline-offset: 2px; }
  #info .ver {
    font-family: ui-monospace, monospace; font-size: 11px;
    color: #cfd3da; background: #2c3038;
    border: 1px solid var(--border); border-radius: 4px;
    padding: 0 5px;
  }
  #info .ver.multi { border-color: #d79b00; }
  #info .dep-overview {
    display: flex; flex-wrap: wrap; gap: 6px 12px;
    font-size: 11px; color: var(--text-dim);
    padding: 6px 8px; margin-bottom: 6px;
    background: #1b1d22; border: 1px solid var(--border);
    border-radius: 6px;
  }
  #info .dep-overview b { color: var(--text); font-weight: 600; }
  #info .dep-overview .ov-tp b { color: #d79b00; }
  #info .dep-filters {
    display: flex; flex-wrap: wrap; gap: 6px 10px;
    margin-bottom: 8px; padding-bottom: 6px;
    border-bottom: 1px solid var(--border);
  }
  #info .dep-filters label {
    display: inline-flex; align-items: center; gap: 4px;
    font-size: 11px; color: var(--text-dim); cursor: pointer;
    user-select: none; white-space: nowrap;
  }
  #info .dep-filters input { margin: 0; width: 12px; height: 12px; cursor: pointer; }
  #info .dep-filters label:hover { color: var(--text); }
  #info .dep-list { margin: 0; }
  #info .dep-row {
    display: flex; flex-wrap: wrap; align-items: baseline; gap: 4px 6px;
    padding: 3px 0; border-bottom: 1px solid #2a2e36;
  }
  #info .dep-row.dep-hidden { display: none; }
  #info .dep-row .dep-name {
    color: var(--text);
    word-break: break-word;
    flex: 1 1 100%;
    min-width: 0;
  }
  #info .tag {
    font-family: ui-monospace, monospace; font-size: 9px;
    line-height: 1.4; padding: 0 5px; border-radius: 3px;
    border: 1px solid var(--border); text-transform: uppercase;
    letter-spacing: 0.04em;
  }
  #info .tag-direct   { color: #c5d6f0; border-color: #6c8ebf; }
  #info .tag-transitive { color: #f5d99e; border-color: #d79b00; }
  #info .tag-lyo      { color: #c4dfb1; border-color: #82b366; }
  #info .tag-ms       { color: #9aa0a8; border-color: #4a5160; }
  #info .tag-tp       { color: #fff; border-color: #d79b00; background: #6b5018; }
  #info .fw-block { margin-top: 6px; }
  #info .fw-block + .fw-block { margin-top: 10px; }
  #info .fw-block.fw-hidden { display: none; }
  #info .fw-group + .fw-group { margin-top: 8px; }
  #info .fw-label {
    display: inline-block;
    font-family: ui-monospace, monospace;
    font-size: 11px; color: #cfd3da;
    background: #2c3038; border: 1px solid var(--border);
    border-radius: 4px; padding: 1px 8px;
    margin: 4px 0 4px 0;
  }
  #info .fw-label.fw-all          { border-color: #6c8ebf; color: #c5d6f0; }
  #info .fw-label.fw-netstandard2_0 { border-color: #d79b00; color: #f5d99e; }
  #info .fw-label.fw-net10_0       { border-color: #82b366; color: #c4dfb1; }
  #info .section + .section { margin-top: 10px; }
  #info .section h4 {
    margin: 0 0 4px 0; font-size: 12px; color: var(--text);
    font-weight: 600;
  }
  .kbd { font-family: ui-monospace, monospace; background: #2c3038;
         border: 1px solid var(--border); border-radius: 4px;
         padding: 1px 5px; font-size: 11px; }
</style>
</head>
<body>
<header>
  <h1>Lyo Project Graph</h1>
  <span class="crumb" id="crumb">Areas overview</span>
  <button id="overview-btn">&larr; Overview</button>
  <select id="area-select"><option value="">Jump to area...</option></select>
  <input id="search" placeholder="filter projects..."/>
  <button id="reset-btn">Reset highlight</button>
  <span id="status"></span>
</header>
<div id="workspace">
  <div id="graph-col">
    <div id="cy"></div>
    <div id="legend"></div>
  </div>
  <aside id="sidebar" class="collapsed">
    <button type="button" id="sidebar-toggle" title="Toggle details panel" aria-expanded="false">&lsaquo;</button>
    <div id="sidebar-resize" role="separator" aria-orientation="vertical"
         aria-label="Resize details panel" title="Drag to resize"></div>
    <div id="info">
      <div class="sidebar-empty">Click a project to inspect dependencies.</div>
    </div>
  </aside>
</div>
<script>
const DATA = __DATA_JSON__;
const COLORS = DATA.colors;
const FILLS = DATA.fills;

const projectByName = {};
for (const p of DATA.projects) projectByName[p.name] = p;

/** Allow Cytoscape to wrap long dotted names inside the node bubble. */
function wrapGraphLabel(name) {
  return name ? name.replace(/\./g, '.\u200b') : name;
}

function labelMaxWidthForCount(n) {
  if (n <= 12) return 220;
  if (n <= 30) return 180;
  if (n <= 60) return 150;
  return 120;
}

function applyGraphLabelSizing(nodes) {
  const n = nodes.length;
  const tmax = labelMaxWidthForCount(n);
  const fs = n > 60 ? 9 : 10;
  const pad = n > 60 ? 8 : 10;
  nodes.style({
    'text-max-width': tmax,
    'font-size': fs,
    'padding': pad,
  });
}

// ----- Element builders ----------------------------------------------------
function buildOverviewElements() {
  const nodes = DATA.areas.map(a => ({
    data: { id: 'A:' + a, label: a + '\n' + DATA.byArea[a] + ' projects',
            area: a, kind: 'area' }
  }));
  const edges = DATA.areaEdges.map(([s, t, w], i) => ({
    data: { id: 'AE:' + i, source: 'A:' + s, target: 'A:' + t,
            weight: w, label: String(w), kind: 'aedge' }
  }));
  return nodes.concat(edges);
}

function buildAreaElements(area) {
  const projects = DATA.projects.filter(p => p.area === area);
  const internal = new Set(projects.map(p => p.name));
  const externals = new Set();
  for (const p of projects) {
    for (const r of p.refs) {
      if (!internal.has(r) && projectByName[r]) externals.add(r);
    }
  }
  const nodes = [];
  for (const p of projects) {
    nodes.push({ data: {
      id: p.name, label: wrapGraphLabel(p.name),
      area: p.area, folder: p.folder, kind: 'project'
    }});
  }
  for (const n of externals) {
    const p = projectByName[n];
    nodes.push({ data: {
      id: n, label: wrapGraphLabel(n), area: p.area, folder: p.folder, kind: 'external'
    }});
  }
  const edges = [];
  for (const p of projects) {
    for (const r of p.refs) {
      if (internal.has(r) || externals.has(r)) {
        edges.push({ data: {
          id: p.name + '->' + r,
          source: p.name, target: r,
          external: !internal.has(r), kind: 'pedge'
        }});
      }
    }
  }
  return nodes.concat(edges);
}

// ----- Cytoscape -----------------------------------------------------------
const cy = cytoscape({
  container: document.getElementById('cy'),
  wheelSensitivity: 0.2,
  style: [
    { selector: 'node', style: {
        'label': 'data(label)',
        'text-wrap': 'wrap',
        'text-overflow-wrap': 'anywhere',
        'color': '#1a1d22',
        'font-size': 12,
        'font-family': 'inherit',
        'text-valign': 'center',
        'text-halign': 'center',
        'border-width': 2,
        'shape': 'round-rectangle',
    }},
    { selector: 'node[kind="area"]', style: {
        'background-color': function(e) { return FILLS[e.data('area')] || '#888'; },
        'border-color':     function(e) { return COLORS[e.data('area')] || '#444'; },
        'width': 'label', 'height': 'label',
        'padding': 12,
        'text-max-width': 200,
        'font-size': 12, 'font-weight': 'bold',
    }},
    { selector: 'node[kind="project"]', style: {
        'background-color': function(e) { return FILLS[e.data('area')] || '#fff'; },
        'border-color':     function(e) { return COLORS[e.data('area')] || '#444'; },
        'width': 'label', 'height': 'label',
        'padding': 10,
        'text-max-width': 200,
        'font-size': 10,
    }},
    { selector: 'node[kind="external"]', style: {
        'background-color': '#2c3038',
        'color': '#cfd3da',
        'border-color': function(e) { return COLORS[e.data('area')] || '#666'; },
        'border-style': 'dashed',
        'width': 'label', 'height': 'label',
        'padding': 10,
        'text-max-width': 200,
        'font-size': 10,
    }},
    { selector: 'edge', style: {
        'curve-style': 'bezier',
        'target-arrow-shape': 'triangle',
        'arrow-scale': 0.9,
        'width': 1.4,
        'line-color': '#5a606a',
        'target-arrow-color': '#5a606a',
        'opacity': 0.85,
    }},
    { selector: 'edge[kind="aedge"]', style: {
        'curve-style': 'bezier',
        'width': function(e) { return Math.min(8, 1 + Math.log2(e.data('weight') + 1)); },
        'label': 'data(label)',
        'font-size': 11, 'color': '#cfd3da',
        'text-background-color': '#1b1d22',
        'text-background-opacity': 0.9,
        'text-background-padding': 2,
        'line-color': '#7a8290',
        'target-arrow-color': '#7a8290',
    }},
    { selector: 'edge[external]', style: {
        'line-style': 'dashed',
        'opacity': 0.7,
    }},
    { selector: '.dim', style: { 'opacity': 0.08 } },
    { selector: 'edge.hover-edge', style: {
        'opacity': 0.9,
        'line-color': '#9aa8c4',
        'target-arrow-color': '#9aa8c4',
        'width': 2,
        'z-index': 9997,
    }},
    { selector: 'node.hi', style: {
        'opacity': 1.0,
        'border-color': '#f7c948', 'border-width': 3,
    }},
    { selector: 'edge.hi', style: {
        'opacity': 1.0,
        'line-color': '#f7c948', 'target-arrow-color': '#f7c948',
        'width': 2.5,
    }},
    { selector: 'node.hi-down', style: {
        'border-color': '#7aa2f7', 'border-width': 3,
    }},
    { selector: 'edge.hi-down', style: {
        'line-color': '#7aa2f7', 'target-arrow-color': '#7aa2f7',
        'width': 2.0,
    }},
    { selector: 'node.hi-up', style: {
        'border-color': '#79c98e', 'border-width': 3,
    }},
    { selector: 'edge.hi-up', style: {
        'line-color': '#79c98e', 'target-arrow-color': '#79c98e',
        'width': 2.0,
    }},
  ],
  elements: [],
});

// Compact wrapped layout: dependency layers flow top-to-bottom, each layer
// wraps into multiple rows. Node width follows label (width:label); rows are
// spaced from measured bounding boxes so labels never overlap neighbors.
const LAYOUT_NODE_GAP = 28;
const LAYOUT_ROW_GAP = 72;
const LAYOUT_MEASURE_PAD = 8;

function maxColsForCount(n) {
  if (n <= 8) return Math.min(4, n);
  if (n <= 20) return 5;
  if (n <= 50) return 6;
  return 8;
}

function computeDependencyDepth(ids, edges) {
  const outgoing = {};
  ids.forEach(id => { outgoing[id] = []; });
  edges.forEach(e => {
    const s = e.source().id(), t = e.target().id();
    if (outgoing[s]) outgoing[s].push(t);
  });
  const memo = {};
  function depth(id, visiting) {
    if (memo[id] !== undefined) return memo[id];
    visiting = visiting || new Set();
    if (visiting.has(id)) return 0;
    visiting.add(id);
    const outs = outgoing[id] || [];
    let d = 0;
    for (const t of outs) d = Math.max(d, depth(t, visiting) + 1);
    visiting.delete(id);
    memo[id] = d;
    return d;
  }
  ids.forEach(id => depth(id));
  return memo;
}

function measureRow(nodes, slice) {
  slice.forEach((id, i) => {
    nodes.getElementById(id).position({ x: i * 900, y: -8000 });
  });
  return slice.map(id => {
    const n = nodes.getElementById(id);
    const dim = n.layoutDimensions({ nodeDimensionsIncludeLabels: true });
    return {
      id: id,
      w: dim.w + LAYOUT_MEASURE_PAD,
      h: dim.h + LAYOUT_MEASURE_PAD,
    };
  });
}

function placeRow(nodes, slice, sizes, y, gap) {
  const totalW = sizes.reduce((s, m) => s + m.w, 0) + gap * (sizes.length - 1);
  let x = -totalW / 2;
  sizes.forEach(m => {
    nodes.getElementById(m.id).position({ x: x + m.w / 2, y: y });
    x += m.w + gap;
  });
  return Math.max(...sizes.map(m => m.h), 32);
}

function runWrappedLayerLayout(nodes, edges, opts) {
  const ids = nodes.map(n => n.id());
  if (!ids.length) return;
  const depthMap = computeDependencyDepth(ids, edges);
  const maxD = Math.max(0, ...ids.map(id => depthMap[id]));
  const byLayer = {};
  ids.forEach(id => {
    const layer = maxD - depthMap[id];
    if (!byLayer[layer]) byLayer[layer] = [];
    byLayer[layer].push(id);
  });
  const maxCols = opts.maxCols || maxColsForCount(ids.length);
  const gap = opts.gap || LAYOUT_NODE_GAP;
  const rowGap = opts.rowGap || LAYOUT_ROW_GAP;
  let y = 0;
  Object.keys(byLayer).map(Number).sort((a, b) => a - b).forEach(layer => {
    const row = byLayer[layer].sort((a, b) => a.localeCompare(b));
    for (let i = 0; i < row.length; i += maxCols) {
      const slice = row.slice(i, i + maxCols);
      const sizes = measureRow(nodes, slice);
      const rowH = placeRow(nodes, slice, sizes, y, gap);
      y += rowH + rowGap;
    }
  });
}

const NODE_BASE = {
  area:     { pad: 12, tmax: 200, fs: 12 },
  project:  { pad: 10, tmax: 200, fs: 10 },
  external: { pad: 10, tmax: 200, fs: 10 },
};

function hoverScaleFactor() {
  const z = cy.zoom();
  if (z >= 1.5) return 1.12;
  if (z >= 1.0) return 1.18;
  if (z >= 0.55) return 1.35;
  if (z >= 0.25) return 1.75;
  return Math.min(3.2, 1.4 + 0.9 / Math.max(z, 0.1));
}

function applyHoverStyle(n, opts) {
  const mild = opts && opts.mild;
  const kind = n.data('kind') || 'project';
  const b = NODE_BASE[kind] || NODE_BASE.project;
  let s = hoverScaleFactor();
  if (mild) s = 1 + (s - 1) * 0.55;
  const fs = Math.round(b.fs * s);
  const tmax = Math.round(b.tmax * s);
  n.style({
    'width': 'label',
    'height': 'label',
    'padding': Math.round(b.pad * s),
    'text-max-width': tmax,
    'font-size': fs,
    'z-index': mild ? 9998 : 9999,
    'border-width': mild ? Math.min(4, 1 + s * 0.35) : Math.min(5, 2 + s * 0.3),
    'border-color': mild ? '#e6c76a' : '#f7c948',
    'opacity': 1,
  });
}

function clearHoverStyle(n) {
  n.removeStyle();
}

let mode = 'overview';
let currentArea = null;
let focused = false;

function setStatus(msg) { document.getElementById('status').textContent = msg; }
function setCrumb(msg)  { document.getElementById('crumb').innerHTML = msg; }

function savePositions() {
  cy.nodes().forEach(n => n.scratch('_pos', Object.assign({}, n.position())));
}

let savedView = null;
function saveView() {
  savedView = { zoom: cy.zoom(), pan: Object.assign({}, cy.pan()) };
}

// ----- Collapsible / resizable right sidebar -------------------------------
const sidebar = document.getElementById('sidebar');
const sidebarToggle = document.getElementById('sidebar-toggle');
const sidebarResize = document.getElementById('sidebar-resize');
const INFO_EMPTY = '<div class="sidebar-empty">Click a project to inspect dependencies.</div>';
const SIDEBAR_W_KEY = 'lyo-graph-sidebar-w';
const SIDEBAR_MIN_W = 200;
const SIDEBAR_MAX_VW = 0.55;

function sidebarMaxWidth() {
  return Math.max(SIDEBAR_MIN_W, Math.floor(window.innerWidth * SIDEBAR_MAX_VW));
}

function readSidebarWidth() {
  const stored = parseInt(localStorage.getItem(SIDEBAR_W_KEY), 10);
  if (!Number.isFinite(stored)) return 260;
  return Math.max(SIDEBAR_MIN_W, Math.min(stored, sidebarMaxWidth()));
}

function setSidebarWidth(px, persist) {
  const w = Math.max(SIDEBAR_MIN_W, Math.min(px, sidebarMaxWidth()));
  document.documentElement.style.setProperty('--sidebar-w', w + 'px');
  if (persist) localStorage.setItem(SIDEBAR_W_KEY, String(w));
  scheduleCyResize();
}

function clampSidebarWidth() {
  if (sidebar.classList.contains('collapsed')) return;
  const cur = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--sidebar-w'));
  if (Number.isFinite(cur)) setSidebarWidth(cur, true);
}

function updateSidebarResizable() {
  const on = !sidebar.classList.contains('collapsed') &&
    !document.getElementById('info').querySelector('.sidebar-empty');
  sidebar.classList.toggle('resizable', on);
}

function updateSidebarToggle() {
  const collapsed = sidebar.classList.contains('collapsed');
  sidebarToggle.textContent = collapsed ? '\u2039' : '\u203A';
  sidebarToggle.title = collapsed ? 'Show details panel' : 'Hide details panel';
  sidebarToggle.setAttribute('aria-expanded', String(!collapsed));
  updateSidebarResizable();
}

function scheduleCyResize() {
  requestAnimationFrame(() => { cy.resize(); });
}

setSidebarWidth(readSidebarWidth(), false);

sidebarToggle.addEventListener('click', () => {
  sidebar.classList.toggle('collapsed');
  updateSidebarToggle();
  scheduleCyResize();
});

sidebar.addEventListener('transitionend', evt => {
  if (evt.propertyName === 'width') scheduleCyResize();
});

window.addEventListener('resize', clampSidebarWidth);

let sidebarResizeDrag = null;

function endSidebarResize(e) {
  if (!sidebarResizeDrag) return;
  if (e && sidebarResizeDrag.pointerId !== undefined &&
      e.pointerId !== sidebarResizeDrag.pointerId) return;
  sidebar.classList.remove('resizing');
  document.body.style.cursor = '';
  document.body.style.userSelect = '';
  try { sidebarResize.releasePointerCapture(sidebarResizeDrag.pointerId); } catch (_) {}
  localStorage.setItem(SIDEBAR_W_KEY, String(sidebar.offsetWidth));
  sidebarResizeDrag = null;
  scheduleCyResize();
}

sidebarResize.addEventListener('pointerdown', e => {
  if (!sidebar.classList.contains('resizable')) return;
  e.preventDefault();
  sidebarResize.setPointerCapture(e.pointerId);
  sidebarResizeDrag = { pointerId: e.pointerId, startX: e.clientX, startW: sidebar.offsetWidth };
  sidebar.classList.add('resizing');
  document.body.style.cursor = 'col-resize';
  document.body.style.userSelect = 'none';
});

sidebarResize.addEventListener('pointermove', e => {
  if (!sidebarResizeDrag || e.pointerId !== sidebarResizeDrag.pointerId) return;
  const delta = sidebarResizeDrag.startX - e.clientX;
  setSidebarWidth(sidebarResizeDrag.startW + delta, false);
});

sidebarResize.addEventListener('pointerup', endSidebarResize);
sidebarResize.addEventListener('pointercancel', endSidebarResize);

function expandSidebar() {
  if (sidebar.classList.contains('collapsed')) {
    sidebar.classList.remove('collapsed');
    updateSidebarToggle();
    scheduleCyResize();
  }
}

function loadOverview() {
  cy.elements().remove();
  cy.add(buildOverviewElements());
  runWrappedLayerLayout(cy.nodes(), cy.edges(), { maxCols: 4, rowGap: 80 });
  cy.fit(undefined, 40);
  savePositions();
  savedView = null;
  mode = 'overview'; currentArea = null; focused = false;
  setCrumb('<b>Areas overview</b> &mdash; click an area to drill in');
  setStatus(DATA.areas.length + ' areas');
  hideInfo();
  if (typeof resetNav === 'function') resetNav();
}

function loadArea(area) {
  cy.elements().remove();
  cy.add(buildAreaElements(area));
  const nodes = cy.nodes();
  applyGraphLabelSizing(nodes);
  runWrappedLayerLayout(nodes, cy.edges(), {
    maxCols: maxColsForCount(nodes.length),
  });
  cy.fit(undefined, 40);
  savePositions();
  savedView = null;
  mode = 'area'; currentArea = area; focused = false;
  const n = cy.nodes().length, e = cy.edges().length;
  setCrumb('<b>' + area + '</b> &mdash; click a project to focus on it');
  setStatus(n + ' nodes, ' + e + ' edges');
  hideInfo();
  // Note: no resetNav here. _showProject() calls loadArea internally when
  // jumping across areas, and we want to preserve the navigation stack
  // across that transition. Manual page changes (via the dropdown / overview
  // button) reset navigation in their own click handlers.
}

// ----- Interactions --------------------------------------------------------
cy.on('tap', 'node[kind="area"]', evt => loadArea(evt.target.data('area')));

cy.on('tap', 'node[kind="project"], node[kind="external"]', evt => {
  jumpToProject(evt.target.id());
});

cy.on('tap', evt => {
  if (evt.target === cy) { unfocus(); clearHighlight(); hideInfo(); resetNav(); }
});

// Hover: enlarge node + neighbors, dim everything else.
let hoverNode = null;
let hoverRelated = null;

function clearHoverState() {
  if (hoverRelated) {
    hoverRelated.nodes().forEach(nn => {
      nn.removeClass('hover-neighbor');
      clearHoverStyle(nn);
    });
    hoverRelated.edges().removeClass('hover-edge');
    hoverRelated = null;
  }
  if (hoverNode) {
    hoverNode.removeClass('hover');
    clearHoverStyle(hoverNode);
    hoverNode = null;
  }
  if (!cy.$('.hi, .hi-down, .hi-up').length) {
    cy.elements().removeClass('dim');
  }
}

cy.on('mouseover', 'node', evt => {
  const n = evt.target;
  clearHoverState();
  if (cy.$('.hi, .hi-down, .hi-up').length) {
    n.addClass('hover');
    applyHoverStyle(n, { mild: true });
    hoverNode = n;
    return;
  }

  const related = n.closedNeighborhood();
  hoverNode = n;
  hoverRelated = related;

  cy.elements().addClass('dim');
  related.removeClass('dim');
  related.edges().addClass('hover-edge');

  n.addClass('hover');
  applyHoverStyle(n);
  related.nodes().not(n).forEach(m => {
    m.addClass('hover-neighbor');
    applyHoverStyle(m, { mild: true });
  });
});

cy.on('mouseout', 'node', evt => {
  clearHoverState();
});

cy.on('mouseout', evt => {
  if (evt.target === cy) clearHoverState();
});

function highlightNeighborhood(node) {
  cy.elements().removeClass('hi hi-down hi-up').addClass('dim');
  node.removeClass('dim').addClass('hi');
  const downstream = node.successors();
  downstream.removeClass('dim').addClass('hi-down');
  const upstream = node.predecessors();
  upstream.removeClass('dim').addClass('hi-up');
}

function clearHighlight() {
  cy.elements().removeClass('dim hi hi-down hi-up');
}

// ----- Focus / unfocus -----------------------------------------------------
function focusOnNode(node) {
  // Save layout positions and camera state the first time we focus
  // from a fresh view.
  if (!focused) {
    savePositions();
    saveView();
    focused = true;
  }

  const related = node.successors().union(node.predecessors()).add(node);
  const relatedNodes = related.nodes();
  const others = cy.elements().difference(related);

  // BFS distance (treating the graph as undirected within the related set)
  // so the clicked node ends up at the centre and each ring is a hop further.
  const dist = {};
  dist[node.id()] = 0;
  let frontier = [node];
  while (frontier.length) {
    const next = [];
    for (const n of frontier) {
      n.connectedEdges().connectedNodes().forEach(m => {
        if (relatedNodes.contains(m) && !(m.id() in dist)) {
          dist[m.id()] = dist[n.id()] + 1;
          next.push(m);
        }
      });
    }
    frontier = next;
  }
  const maxDist = Math.max(0, ...Object.values(dist));

  others.style('display', 'none');
  related.style('display', 'element');

  const byRing = {};
  relatedNodes.forEach(nn => {
    const ring = maxDist - (dist[nn.id()] || 0);
    if (!byRing[ring]) byRing[ring] = [];
    byRing[ring].push(nn.id());
  });
  const maxCols = maxColsForCount(relatedNodes.length);
  let y = 0;
  for (let ring = 0; ring <= maxDist; ring++) {
    const row = (byRing[ring] || []).sort((a, b) => a.localeCompare(b));
    for (let i = 0; i < row.length; i += maxCols) {
      const slice = row.slice(i, i + maxCols);
      const sizes = measureRow(relatedNodes, slice);
      const rowH = placeRow(relatedNodes, slice, sizes, y, LAYOUT_NODE_GAP);
      y += rowH + LAYOUT_ROW_GAP;
    }
  }
  cy.animate({ fit: { eles: related, padding: 60 } }, {
    duration: 450, easing: 'ease-out',
  });
}

function unfocus() {
  if (!focused) return;
  cy.elements().style('display', 'element');
  cy.nodes().forEach(n => {
    const p = n.scratch('_pos');
    if (p) {
      n.animation({
        position: { x: p.x, y: p.y },
        duration: 450,
        easing: 'ease-out',
      }).play();
    }
  });
  if (savedView) {
    cy.animate({
      zoom: savedView.zoom,
      pan:  savedView.pan,
    }, {
      duration: 450,
      easing: 'ease-out',
    });
  }
  focused = false;
}

function projLink(name) {
  const n = escapeHtml(name);
  return '<span class="proj-link" data-jump="' + n + '" tabindex="0" role="link">' +
         n + '</span>';
}

// Treat anything in the Microsoft.* or System.* namespace as a Microsoft
// package; everything else is third-party.
function isMicrosoft(name) {
  return /^(Microsoft|System)\./i.test(String(name));
}

function frameworkChipHtml(fw, count) {
  const slug = String(fw).replace(/[^A-Za-z0-9_]/g, '_');
  const label = (fw === 'all') ? 'All frameworks' : fw;
  return '<div class="fw-label fw-' + slug + '">' +
         escapeHtml(label) + ' &middot; ' + count + '</div>';
}

function tagHtml(cls, label) {
  return '<span class="tag tag-' + cls + '">' + label + '</span>';
}

function versionChipsHtml(versions) {
  const vs = (versions && versions.length) ? versions : [''];
  const multi = vs.length > 1;
  return vs.map(v => {
    const label = v ? escapeHtml(v) : '<i>unspecified</i>';
    return '<span class="ver' + (multi ? ' multi' : '') + '">' + label + '</span>';
  }).join('');
}

// ----- Dependency panel: two tabs, tags, filters, overview ----------------
let activeTab = 'depends';
let depFilters = {
  transitive: true,
  lyo: true,
  ms: true,
  tp: true,
};

function buildDependsRows(p) {
  const directRefSet = new Set(p.refs || []);
  const directPkgNames = new Set();
  const rows = [];

  for (const r of (p.refs || [])) {
    rows.push({
      kind: 'lyo', name: r, scope: 'direct',
      framework: null, versions: null, vendor: null,
    });
  }
  for (const r of (p.transitiveLyo || [])) {
    if (directRefSet.has(r)) continue;
    rows.push({
      kind: 'lyo', name: r, scope: 'transitive',
      framework: null, versions: null, vendor: null,
    });
  }

  function addPackageGroups(groups, scope) {
    for (const g of groups) {
      for (const pkg of g.packages) {
        if (scope === 'transitive' && directPkgNames.has(pkg.name)) continue;
        if (scope === 'direct') directPkgNames.add(pkg.name);
        const ms = isMicrosoft(pkg.name);
        rows.push({
          kind: 'pkg', name: pkg.name, scope: scope,
          framework: g.framework, versions: pkg.versions, vendor: ms ? 'ms' : 'tp',
        });
      }
    }
  }
  addPackageGroups(p.directPackages || [], 'direct');
  addPackageGroups(p.transitivePackages || [], 'transitive');
  return rows;
}

function depRowHtml(row) {
  const scope = row.scope;
  const isLyo = row.kind === 'lyo';
  const isMs = row.vendor === 'ms';
  const tags =
    tagHtml(scope, scope) +
    (isLyo ? tagHtml('lyo', 'lyo') : '') +
    (!isLyo && isMs ? tagHtml('ms', 'ms') : '') +
    (!isLyo && !isMs ? tagHtml('tp', '3rd') : '');

  const attrs =
    ' data-kind="' + row.kind + '"' +
    ' data-scope="' + scope + '"' +
    ' data-lyo="' + (isLyo ? '1' : '0') + '"' +
    ' data-ms="' + (!isLyo && isMs ? '1' : '0') + '"' +
    ' data-tp="' + (!isLyo && !isMs ? '1' : '0') + '"' +
    (row.framework ? ' data-framework="' + escapeHtml(row.framework) + '"' : '');

  let body;
  if (isLyo) {
    body = projLink(row.name);
  } else {
    body = '<span class="dep-name">' + escapeHtml(row.name) + '</span>' +
           versionChipsHtml(row.versions);
  }
  return '<div class="dep-row"' + attrs + '>' + tags + body + '</div>';
}

function renderDependsList(rows) {
  if (!rows.length) {
    return '<div class="empty">No dependencies.</div>';
  }
  const lyoRows = rows.filter(r => r.kind === 'lyo');
  const pkgRows = rows.filter(r => r.kind === 'pkg');

  let html = '<div class="dep-list" id="dep-list">';
  if (lyoRows.length) {
    html += '<div class="section"><h4>Projects</h4>';
    for (const r of lyoRows) html += depRowHtml(r);
    html += '</div>';
  }

  if (pkgRows.length) {
    html += '<div class="section"><h4>Packages</h4>';
    const byFw = {};
    for (const r of pkgRows) {
      const fw = r.framework || 'all';
      if (!byFw[fw]) byFw[fw] = [];
      byFw[fw].push(r);
    }
    const fwKeys = Object.keys(byFw).sort((a, b) => {
      if (a === 'all') return -1;
      if (b === 'all') return 1;
      return a.localeCompare(b);
    });
    for (const fw of fwKeys) {
      const blockRows = byFw[fw];
      html += '<div class="fw-block" data-fw-block="' + escapeHtml(fw) + '">' +
              frameworkChipHtml(fw, blockRows.length);
      for (const r of blockRows) html += depRowHtml(r);
      html += '</div>';
    }
    html += '</div>';
  }
  html += '</div>';
  return html;
}

function buildUsedByRows(name) {
  return DATA.projects.filter(q => q.refs.includes(name)).map(q => ({
    kind: 'lyo', name: q.name, scope: 'direct',
    framework: null, versions: null, vendor: null,
  }));
}

function renderUsedByList(rows) {
  if (!rows.length) {
    return '<div class="empty">Nothing in the solution depends on this project.</div>';
  }
  let html = '<div class="dep-list" id="usedby-list">';
  for (const r of rows) html += depRowHtml(r);
  html += '</div>';
  return html;
}

function depOverviewHtml(stats) {
  return '<div class="dep-overview" id="dep-overview">' +
    '<span><b id="ov-shown">' + stats.shown + '</b> / <b id="ov-total">' +
    stats.total + '</b> shown</span>' +
    '<span>Lyo <b id="ov-lyo">' + stats.lyo + '</b></span>' +
    '<span>Pkgs <b id="ov-pkg">' + stats.pkg + '</b></span>' +
    '<span>Direct <b id="ov-direct">' + stats.direct + '</b></span>' +
    '<span>Transitive <b id="ov-trans">' + stats.transitive + '</b></span>' +
    '<span class="ov-tp">3rd <b id="ov-tp">' + stats.tp + '</b></span>' +
    '<span>MS <b id="ov-ms">' + stats.ms + '</b></span>' +
    '</div>';
}

function depFiltersHtml() {
  function chk(key, label) {
    const on = depFilters[key] ? ' checked' : '';
    return '<label><input type="checkbox" data-dep-filter="' + key + '"' +
           on + '/> ' + label + '</label>';
  }
  return '<div class="dep-filters" id="dep-filters">' +
    chk('transitive', 'Transitive') +
    chk('lyo', 'Lyo') +
    chk('ms', 'Microsoft') +
    chk('tp', '3rd party') +
    '</div>';
}

function applyDepFilters(panelId) {
  const panel = document.querySelector('.tabpanel[data-tab="' + panelId + '"]');
  if (!panel) return;
  const list = panel.querySelector('.dep-list');
  if (!list) return;

  list.querySelectorAll('.dep-row').forEach(el => {
    const scope = el.dataset.scope;
    const isLyo = el.dataset.lyo === '1';
    const isMs = el.dataset.ms === '1';
    const isTp = el.dataset.tp === '1';
    let show = true;
    if (panelId === 'depends') {
      if (scope === 'transitive' && !depFilters.transitive) show = false;
      if (isLyo && !depFilters.lyo) show = false;
      if (isMs && !depFilters.ms) show = false;
      if (isTp && !depFilters.tp) show = false;
    } else {
      // Depended on by tab: incoming Lyo projects only
      if (isLyo && !depFilters.lyo) show = false;
    }
    el.classList.toggle('dep-hidden', !show);
  });

  list.querySelectorAll('.fw-block').forEach(block => {
    const visible = block.querySelectorAll('.dep-row:not(.dep-hidden)').length;
    block.classList.toggle('fw-hidden', visible === 0);
  });

  if (panelId === 'depends') updateDependsOverview();
  else updateUsedByOverview();
}

function updateDependsOverview() {
  const list = document.getElementById('dep-list');
  if (!list) return;
  const total = list.querySelectorAll('.dep-row').length;
  const stats = { total: total, shown: 0, lyo: 0, pkg: 0,
                  direct: 0, transitive: 0, ms: 0, tp: 0 };
  list.querySelectorAll('.dep-row:not(.dep-hidden)').forEach(el => {
    stats.shown++;
    if (el.dataset.lyo === '1') stats.lyo++;
    if (el.dataset.kind === 'pkg') stats.pkg++;
    if (el.dataset.scope === 'direct') stats.direct++;
    if (el.dataset.scope === 'transitive') stats.transitive++;
    if (el.dataset.ms === '1') stats.ms++;
    if (el.dataset.tp === '1') stats.tp++;
  });
  const set = (id, v) => { const e = document.getElementById(id); if (e) e.textContent = v; };
  set('ov-shown', stats.shown);
  set('ov-total', stats.total);
  set('ov-lyo', stats.lyo);
  set('ov-pkg', stats.pkg);
  set('ov-direct', stats.direct);
  set('ov-trans', stats.transitive);
  set('ov-ms', stats.ms);
  set('ov-tp', stats.tp);
  // Update tab count
  const tab = document.querySelector('.tab[data-tab="depends"]');
  if (tab) tab.textContent = 'Depends (' + stats.shown + '/' + stats.total + ')';
}

function updateUsedByOverview() {
  const list = document.getElementById('usedby-list');
  const total = list ? list.querySelectorAll('.dep-row').length : 0;
  const shown = list ? list.querySelectorAll('.dep-row:not(.dep-hidden)').length : 0;
  const set = (id, v) => { const e = document.getElementById(id); if (e) e.textContent = v; };
  set('ov-shown-usedby', shown);
  set('ov-total-usedby', total);
  set('ov-lyo-usedby', shown);
  const tab = document.querySelector('.tab[data-tab="usedby"]');
  if (tab) tab.textContent = 'Depended on by (' + shown + '/' + total + ')';
}

function showInfo(node) {
  const info = document.getElementById('info');
  const name = node.id();
  const p = projectByName[name];
  if (!p) { hideInfo(); return; }
  expandSidebar();

  const dependsRows = buildDependsRows(p);
  const usedByRows = buildUsedByRows(name);

  const dependsStats = {
    total: dependsRows.length, shown: dependsRows.length,
    lyo: dependsRows.filter(r => r.kind === 'lyo').length,
    pkg: dependsRows.filter(r => r.kind === 'pkg').length,
    direct: dependsRows.filter(r => r.scope === 'direct').length,
    transitive: dependsRows.filter(r => r.scope === 'transitive').length,
    ms: dependsRows.filter(r => r.vendor === 'ms').length,
    tp: dependsRows.filter(r => r.vendor === 'tp').length,
  };

  const dependsPanel =
    depOverviewHtml(dependsStats) +
    depFiltersHtml() +
    renderDependsList(dependsRows);

  const usedByStats = {
    total: usedByRows.length, shown: usedByRows.length,
    lyo: usedByRows.length, pkg: 0, direct: usedByRows.length,
    transitive: 0, ms: 0, tp: 0,
  };
  const usedByPanel =
    '<div class="dep-overview" id="dep-overview-usedby">' +
      '<span><b id="ov-shown-usedby">' + usedByStats.shown + '</b> / ' +
        '<b id="ov-total-usedby">' + usedByStats.total + '</b> shown</span>' +
      '<span>Lyo <b id="ov-lyo-usedby">' + usedByStats.lyo + '</b></span>' +
    '</div>' +
    '<div class="dep-filters" id="usedby-filters">' +
      '<label><input type="checkbox" data-dep-filter="lyo"' +
      (depFilters.lyo ? ' checked' : '') + '/> Lyo</label>' +
    '</div>' +
    renderUsedByList(usedByRows);

  info.innerHTML =
    '<div class="header-row">' +
      '<button class="back" id="info-back" title="Back" hidden>&larr;</button>' +
      '<h3>' + escapeHtml(name) + '</h3>' +
    '</div>' +
    '<div class="meta">' + escapeHtml(p.folder) + ' &middot; ' + escapeHtml(p.area) + '</div>' +
    '<div class="tabs">' +
      '<button class="tab active" data-tab="depends">Depends (' +
        dependsStats.shown + '/' + dependsStats.total + ')</button>' +
      '<button class="tab" data-tab="usedby">Depended on by (' +
        usedByStats.shown + '/' + usedByStats.total + ')</button>' +
    '</div>' +
    '<div class="tabpanel" data-tab="depends">' + dependsPanel + '</div>' +
    '<div class="tabpanel" data-tab="usedby" hidden>' + usedByPanel + '</div>';

  const tabAliases = {
    direct: 'depends', pkg: 'deps', deps: 'depends', lyo: 'depends', depended: 'usedby',
  };
  if (tabAliases[activeTab]) activeTab = tabAliases[activeTab];
  const validTabs = ['depends', 'usedby'];
  if (!validTabs.includes(activeTab)) activeTab = 'depends';

  info.querySelectorAll('.tab').forEach(b => b.classList.toggle('active',
    b.getAttribute('data-tab') === activeTab));
  info.querySelectorAll('.tabpanel').forEach(pan => {
    if (pan.getAttribute('data-tab') === activeTab) pan.removeAttribute('hidden');
    else pan.setAttribute('hidden', '');
  });

  info.querySelectorAll('.tab').forEach(btn => {
    btn.addEventListener('click', () => {
      activeTab = btn.getAttribute('data-tab');
      info.querySelectorAll('.tab').forEach(b => b.classList.toggle('active',
        b.getAttribute('data-tab') === activeTab));
      info.querySelectorAll('.tabpanel').forEach(pan => {
        if (pan.getAttribute('data-tab') === activeTab) pan.removeAttribute('hidden');
        else pan.setAttribute('hidden', '');
      });
    });
  });

  applyDepFilters('depends');
  applyDepFilters('usedby');
  updateSidebarResizable();
  scheduleCyResize();
}

function hideInfo() {
  document.getElementById('info').innerHTML = INFO_EMPTY;
  updateSidebarResizable();
}

function rerenderPanel() {
  if (!navCurrent) return;
  const n = cy.getElementById(navCurrent);
  if (!n.empty()) showInfo(n);
}

// ----- Back-navigation history --------------------------------------------
let navHistory = [];   // previous project names (oldest -> newest)
let navCurrent = null; // project currently shown in the panel

function refreshBackButton() {
  const back = document.getElementById('info-back');
  if (!back) return;
  if (navHistory.length) back.removeAttribute('hidden');
  else back.setAttribute('hidden', '');
}

function _showProject(name) {
  const target = projectByName[name];
  if (!target) return false;
  // If the target isn't currently in the graph, switch to its home area first.
  let n = cy.getElementById(name);
  if (n.empty() && target.area) {
    loadArea(target.area);
    n = cy.getElementById(name);
  }
  if (n.empty()) return false;
  highlightNeighborhood(n);
  showInfo(n);
  focusOnNode(n);
  refreshBackButton();
  return true;
}

function jumpToProject(name) {
  if (!_showProject(name)) return;
  if (navCurrent && navCurrent !== name) navHistory.push(navCurrent);
  navCurrent = name;
  refreshBackButton();
}

function navBack() {
  if (!navHistory.length) return;
  const prev = navHistory.pop();
  if (_showProject(prev)) navCurrent = prev;
  refreshBackButton();
}

function resetNav() {
  navHistory = [];
  navCurrent = null;
  refreshBackButton();
}

document.getElementById('info').addEventListener('click', evt => {
  const back = evt.target.closest('#info-back');
  if (back) {
    evt.preventDefault();
    evt.stopPropagation();
    navBack();
    return;
  }
  const link = evt.target.closest('.proj-link');
  if (!link) return;
  evt.preventDefault();
  evt.stopPropagation();
  jumpToProject(link.getAttribute('data-jump'));
});

document.getElementById('info').addEventListener('keydown', evt => {
  if (evt.key !== 'Enter' && evt.key !== ' ') return;
  const link = evt.target.closest('.proj-link');
  if (!link) return;
  evt.preventDefault();
  jumpToProject(link.getAttribute('data-jump'));
});

document.getElementById('info').addEventListener('change', evt => {
  const key = evt.target.getAttribute('data-dep-filter');
  if (!key || !(key in depFilters)) return;
  depFilters[key] = evt.target.checked;
  applyDepFilters('depends');
  applyDepFilters('usedby');
});

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, c => ({
    '&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'
  }[c]));
}

// ----- Header controls -----------------------------------------------------
const sel = document.getElementById('area-select');
for (const a of DATA.areas) {
  const o = document.createElement('option');
  o.value = a; o.textContent = a + ' (' + DATA.byArea[a] + ')';
  sel.appendChild(o);
}
sel.addEventListener('change', () => {
  if (sel.value) { resetNav(); loadArea(sel.value); }
  sel.value = '';
});

document.getElementById('overview-btn').onclick = loadOverview;
document.getElementById('reset-btn').onclick = () => {
  unfocus(); clearHighlight(); hideInfo(); resetNav();
};

const search = document.getElementById('search');
search.addEventListener('input', () => {
  const q = search.value.trim().toLowerCase();
  if (!q) { clearHighlight(); return; }
  cy.elements().addClass('dim').removeClass('hi');
  const matches = cy.nodes().filter(n => n.id().toLowerCase().includes(q));
  matches.removeClass('dim').addClass('hi');
  matches.connectedEdges().forEach(e => {
    const o = e.source().hasClass('hi') ? e.target() : e.source();
    if (matches.contains(o) || matches.contains(e.source()) && matches.contains(e.target())) {
      e.removeClass('dim');
    }
  });
});

// ----- Legend --------------------------------------------------------------
const legend = document.getElementById('legend');
for (const a of DATA.areas) {
  const span = document.createElement('span');
  span.innerHTML = '<span class="swatch" style="background:' +
    FILLS[a] + ';border:1px solid ' + COLORS[a] + '"></span>' + a;
  legend.appendChild(span);
}
const tip = document.createElement('span');
tip.style.marginLeft = 'auto';
tip.style.color = 'var(--text-dim)';
tip.innerHTML = 'Tip: <span class="kbd">click</span> = drill / highlight &middot; ' +
                '<span class="kbd">scroll</span> = zoom &middot; <span class="kbd">drag</span> = pan';
legend.appendChild(tip);

loadOverview();
updateSidebarToggle();
window.addEventListener('resize', scheduleCyResize);
</script>
</body>
</html>
"""


def build_html(data: dict) -> str:
    return HTML_TEMPLATE.replace("__DATA_JSON__", json.dumps(data, separators=(",", ":")))


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--include-tests",
        action="store_true",
        help="Include *.Tests / *.Benchmarks projects (off by default).",
    )
    parser.add_argument("--html", type=Path, default=OUT_HTML)
    args = parser.parse_args()

    projects = load_slnx_projects()
    for p in projects:
        parse_refs(p)

    if not args.include_tests:
        kept = {p.name for p in projects if not is_test_like(p.name)}
        projects = [p for p in projects if p.name in kept]
        for p in projects:
            p.refs = [r for r in p.refs if r in kept]

    data = build_graph_data(projects)
    args.html.write_text(build_html(data), encoding="utf-8")

    by_area: dict[str, int] = defaultdict(int)
    for p in projects:
        by_area[top_area(p.folder)] += 1
    total_refs = sum(len(p.refs) for p in projects)

    print(f"Projects:  {len(projects)}")
    print(f"Edges:     {total_refs}")
    print("By area:")
    for a in AREA_ORDER:
        if a in by_area:
            print(f"  {a:<14} {by_area[a]:>3}")
    print(f"Wrote:     {args.html.relative_to(REPO_ROOT)}")


if __name__ == "__main__":
    main()
