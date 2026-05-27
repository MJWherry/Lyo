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
    package_refs: list[tuple[str, str]] = field(default_factory=list)


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

    # NuGet / external packages. Version may be missing under Central Package
    # Management; store empty string in that case.
    pkg_seen: set[tuple[str, str]] = set()
    for pkg in root.iter("PackageReference"):
        include = pkg.attrib.get("Include")
        if not include:
            continue
        version = pkg.attrib.get("Version", "")
        # Some csprojs use a child element, e.g.
        #   <PackageReference Include="X"><Version>1.2.3</Version></PackageReference>
        if not version:
            v_el = pkg.find("Version")
            if v_el is not None and v_el.text:
                version = v_el.text.strip()
        key = (include, version)
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


def _aggregate_packages(
    names: list[str], name_to_proj: dict[str, Project]
) -> list[dict]:
    """Union of (package, version) pairs across the given projects."""
    versions_by_pkg: dict[str, set[str]] = defaultdict(set)
    for n in names:
        proj = name_to_proj.get(n)
        if proj is None:
            continue
        for pkg, ver in proj.package_refs:
            versions_by_pkg[pkg].add(ver)
    out: list[dict] = []
    for pkg in sorted(versions_by_pkg):
        versions = sorted(v for v in versions_by_pkg[pkg] if v)
        # If every entry was empty, keep a single empty placeholder so the UI
        # can still list the package name.
        if not versions and "" in versions_by_pkg[pkg]:
            versions = [""]
        out.append({"name": pkg, "versions": versions})
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
            "directPackages": [
                {"name": pkg, "versions": [ver] if ver else [""]}
                for pkg, ver in sorted(set(p.package_refs))
            ],
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
  #cy { flex: 1; min-height: 0; background: #15171b; }
  #legend {
    display: flex; gap: 10px; flex-wrap: wrap;
    padding: 6px 16px; background: var(--bg-elev);
    border-top: 1px solid var(--border);
    font-size: 12px;
  }
  .swatch {
    display: inline-block; width: 10px; height: 10px;
    border-radius: 2px; margin-right: 6px; vertical-align: middle;
  }
  #info {
    position: absolute; right: 16px; bottom: 60px;
    width: 380px; max-width: calc(100vw - 32px);
    max-height: 65vh;
    background: var(--bg-elev); border: 1px solid var(--border);
    border-radius: 8px; padding: 12px 14px;
    font-size: 12px; line-height: 1.5;
    box-shadow: 0 4px 16px rgba(0,0,0,0.4);
    display: none; flex-direction: column;
  }
  #info.open { display: flex; }
  #info h3 { margin: 0; font-size: 13px; color: var(--accent); }
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
<div id="cy"></div>
<div id="legend"></div>
<div id="info"></div>
<script>
const DATA = __DATA_JSON__;
const COLORS = DATA.colors;
const FILLS = DATA.fills;

const projectByName = {};
for (const p of DATA.projects) projectByName[p.name] = p;

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
      id: p.name, label: p.name,
      area: p.area, folder: p.folder, kind: 'project'
    }});
  }
  for (const n of externals) {
    const p = projectByName[n];
    nodes.push({ data: {
      id: n, label: n, area: p.area, folder: p.folder, kind: 'external'
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
        'width': 200, 'height': 70,
        'font-size': 14, 'font-weight': 'bold',
    }},
    { selector: 'node[kind="project"]', style: {
        'background-color': function(e) { return FILLS[e.data('area')] || '#fff'; },
        'border-color':     function(e) { return COLORS[e.data('area')] || '#444'; },
        'width': 220, 'height': 36,
    }},
    { selector: 'node[kind="external"]', style: {
        'background-color': '#2c3038',
        'color': '#cfd3da',
        'border-color': function(e) { return COLORS[e.data('area')] || '#666'; },
        'border-style': 'dashed',
        'width': 220, 'height': 36,
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
    { selector: '.dim', style: { 'opacity': 0.10 } },
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

const layoutOpts = {
  overview: { name: 'dagre', rankDir: 'TB', nodeSep: 60, rankSep: 110, edgeSep: 30, animate: false },
  area:     { name: 'dagre', rankDir: 'TB', nodeSep: 22, rankSep: 70,  edgeSep: 14, animate: false },
};

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

function loadOverview() {
  cy.elements().remove();
  cy.add(buildOverviewElements());
  cy.layout(layoutOpts.overview).run();
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
  cy.layout(layoutOpts.area).run();
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

  relatedNodes.layout({
    name: 'concentric',
    concentric: n => maxDist - (dist[n.id()] || 0),
    levelWidth: () => 1,
    minNodeSpacing: 40,
    spacingFactor: 1.1,
    animate: true,
    animationDuration: 450,
    animationEasing: 'ease-out',
    fit: true,
    padding: 80,
  }).run();
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

function listSection(title, items) {
  if (!items.length) {
    return '<div class="section"><h4>' + escapeHtml(title) + ' (0)</h4>' +
           '<div class="empty">none</div></div>';
  }
  return '<div class="section"><h4>' + escapeHtml(title) +
         ' (' + items.length + ')</h4>' +
         '<ul>' + items.map(r => '<li>' + projLink(r) + '</li>').join('') +
         '</ul></div>';
}

function packageSection(title, pkgs) {
  if (!pkgs.length) {
    return '<div class="section"><h4>' + escapeHtml(title) + ' (0)</h4>' +
           '<div class="empty">none</div></div>';
  }
  const rows = pkgs.map(pkg => {
    const versions = (pkg.versions && pkg.versions.length)
      ? pkg.versions
      : [''];
    const multi = versions.length > 1;
    const chips = versions.map(v => {
      const label = v ? escapeHtml(v) : '<i>unspecified</i>';
      return '<span class="ver' + (multi ? ' multi' : '') + '">' + label + '</span>';
    }).join('');
    return '<div class="pkg-row"><span class="pkg-name">' +
           escapeHtml(pkg.name) + '</span>' + chips + '</div>';
  }).join('');
  return '<div class="section"><h4>' + escapeHtml(title) +
         ' (' + pkgs.length + ')</h4>' + rows + '</div>';
}

function showInfo(node) {
  const info = document.getElementById('info');
  const name = node.id();
  const p = projectByName[name];
  if (!p) { info.classList.remove('open'); return; }

  const directRefs = p.refs || [];
  const directDeps = DATA.projects.filter(q => q.refs.includes(name)).map(q => q.name);
  const directPkgs = p.directPackages || [];
  const transLyo   = p.transitiveLyo || [];
  const transPkgs  = p.transitivePackages || [];

  const directHtml =
    listSection('Depends on', directRefs) +
    listSection('Depended on by', directDeps) +
    packageSection('Direct packages', directPkgs);

  const lyoHtml = transLyo.length
    ? listSection('Transitive Lyo projects', transLyo)
    : '<div class="empty">No transitive Lyo dependencies.</div>';

  const pkgHtml = transPkgs.length
    ? packageSection('Transitive packages', transPkgs)
    : '<div class="empty">No transitive packages reachable from this project.</div>';

  info.innerHTML =
    '<div class="header-row">' +
      '<button class="back" id="info-back" title="Back" hidden>&larr;</button>' +
      '<h3>' + escapeHtml(name) + '</h3>' +
    '</div>' +
    '<div class="meta">' + escapeHtml(p.folder) + ' &middot; ' + escapeHtml(p.area) + '</div>' +
    '<div class="tabs">' +
      '<button class="tab active" data-tab="direct">Direct (' +
        (directRefs.length + directDeps.length + directPkgs.length) + ')</button>' +
      '<button class="tab" data-tab="lyo">Transitive Lyo (' + transLyo.length + ')</button>' +
      '<button class="tab" data-tab="pkg">Transitive packages (' + transPkgs.length + ')</button>' +
    '</div>' +
    '<div class="tabpanel" data-tab="direct">' + directHtml + '</div>' +
    '<div class="tabpanel" data-tab="lyo" hidden>' + lyoHtml + '</div>' +
    '<div class="tabpanel" data-tab="pkg" hidden>' + pkgHtml + '</div>';

  info.classList.add('open');

  info.querySelectorAll('.tab').forEach(btn => {
    btn.addEventListener('click', () => {
      const which = btn.getAttribute('data-tab');
      info.querySelectorAll('.tab').forEach(b => b.classList.toggle('active',
        b.getAttribute('data-tab') === which));
      info.querySelectorAll('.tabpanel').forEach(p => {
        if (p.getAttribute('data-tab') === which) p.removeAttribute('hidden');
        else p.setAttribute('hidden', '');
      });
    });
  });
}

function hideInfo() { document.getElementById('info').classList.remove('open'); }

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
