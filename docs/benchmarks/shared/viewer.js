/* Single benchmark report viewer: dispatches on report.type -> renderMicro / renderLoad.
   Reads window.LyoBench.reports[name] populated by data/<name>.js. Works from file://. */
(function () {
  var R = window.BenchmarkDashboard;
  var el = R.el;

  function formatBytes(n) {
    if (typeof n !== "number" || !isFinite(n) || n <= 0) return null;
    var gb = 1024 * 1024 * 1024;
    var mb = 1024 * 1024;
    if (n >= gb) return (n / gb).toFixed(n % gb === 0 ? 0 : 1) + " GB";
    if (n >= mb) return Math.round(n / mb) + " MB";
    return Math.round(n / 1024) + " KB";
  }

  function formatDuration(seconds) {
    if (typeof seconds !== "number" || !isFinite(seconds) || seconds < 0) return null;
    var s = Math.round(seconds);
    var h = Math.floor(s / 3600);
    var m = Math.floor((s % 3600) / 60);
    var sec = s % 60;
    if (h > 0) return h + "h " + m + "m";
    if (m > 0) return m + "m " + sec + "s";
    return sec + "s";
  }

  function formatTimestamp(value) {
    if (!value) return null;
    var d = new Date(value);
    if (isNaN(d.getTime())) return null;
    return d.toLocaleString(undefined, {
      year: "numeric", month: "short", day: "numeric",
      hour: "2-digit", minute: "2-digit",
    });
  }

  function coresChip(env) {
    if (!env.logicalCores) return null;
    var chip = env.logicalCores + " vCPU";
    if (env.physicalCores && env.physicalCores !== env.logicalCores)
      chip += " (" + env.physicalCores + " phys)";
    return chip;
  }

  function metaChips(report) {
    var env = report.environment || {};
    var started = formatTimestamp(report.runStarted);
    var ended = formatTimestamp(report.runEnded);
    var duration = formatDuration(report.durationSeconds);
    return [
      report.runId ? "Run " + report.runId : null,
      env.tool ? env.tool + (env.toolVersion ? " " + env.toolVersion : "") : null,
      env.runtime || null,
      env.dotnetSdkVersion ? "SDK " + env.dotnetSdkVersion : null,
      env.configuration || null,
      env.cpu || null,
      coresChip(env),
      env.architecture || null,
      formatBytes(env.memoryBytes) ? formatBytes(env.memoryBytes) + " RAM" : null,
      env.gcMode ? env.gcMode + " GC" : null,
      env.os || null,
      started ? "Started " + started : null,
      ended ? "Ended " + ended : null,
      duration ? "Duration " + duration : null,
    ].filter(Boolean);
  }

  function notesSection(report) {
    if (!report.notes || !report.notes.length) return null;
    return el("section", {}, [
      el("h2", { text: "Notes" }),
      el("div", { className: "callout" }, report.notes.map(function (note) { return el("p", { text: note }); })),
    ]);
  }

  function paramLabel(parameters) {
    if (!parameters) return "—";
    var keys = Object.keys(parameters);
    if (!keys.length) return "—";
    return keys
      .map(function (k) { return k + "=" + parameters[k]; })
      .join(", ");
  }

  /* A muted lead/subtitle paragraph for a report's methodology description. */
  function reportLead(text) {
    if (!text) return null;
    return el("p", { className: "report-lead", text: text });
  }

  /* Legend explaining parameters: "Name (unit) — description". */
  function paramLegend(parameters) {
    if (!parameters || !parameters.length) return null;
    return el(
      "ul",
      { className: "param-legend" },
      parameters.map(function (p) {
        var head = p.name + (p.unit ? " (" + p.unit + ")" : "");
        return el("li", {}, [
          el("strong", { text: head }),
          p.description ? el("span", { text: " — " + p.description }) : null,
        ]);
      })
    );
  }

  /* Flatten a column tree into [{depth, col}] rows for tabular display. */
  function flattenColumns(columns, depth, acc) {
    (columns || []).forEach(function (col) {
      acc.push({ depth: depth, col: col });
      if (col.children && col.children.length) flattenColumns(col.children, depth + 1, acc);
    });
    return acc;
  }

  /* Renders the data-shape descriptor (columns, types, nesting) for a benchmark group. */
  function datasetBlock(dataset) {
    if (!dataset) return null;
    var stat =
      dataset.typeName +
      " · " + (dataset.columnCount != null ? dataset.columnCount : "?") + " columns" +
      " · nesting depth " + (dataset.maxNestingDepth != null ? dataset.maxNestingDepth : 0);
    var rows = flattenColumns(dataset.columns, 0, []).map(function (entry) {
      var indent = entry.depth ? "\u2003".repeat(entry.depth) + "└ " : "";
      return [indent + entry.col.name, entry.col.type, entry.col.kind];
    });
    var children = [
      el("div", { className: "dataset-stat", text: stat }),
    ];
    if (rows.length) {
      children.push(R.table([{ label: "Column" }, { label: "Type" }, { label: "Kind" }], rows));
    }
    if (dataset.notes) children.push(el("p", { className: "muted-sub", text: dataset.notes }));
    return el("div", { className: "dataset" }, [el("h4", { text: "Data structure" })].concat(children));
  }

  /* ---------------------------------------------------------------- micro */
  function renderMicro(report, root) {
    root.appendChild(el("section", {}, [el("div", { className: "meta", id: "meta" })]));
    R.mountMeta(root.querySelector("#meta"), metaChips(report));

    var lead = reportLead(report.description);
    if (lead) root.appendChild(lead);

    if (report.comparison && report.comparison.groups && report.comparison.groups.length) {
      root.appendChild(comparisonSection(report.comparison));
    }

    var groupsWrap = el("section", {}, [el("h2", { text: "Benchmark classes" })]);
    (report.groups || []).forEach(function (group) {
      var cardChildren = [el("h3", { text: group.name })];
      if (group.description) cardChildren.push(el("p", { text: group.description }));
      var legend = paramLegend(group.parameters);
      if (legend) cardChildren.push(legend);
      var dataset = datasetBlock(group.dataset);
      if (dataset) cardChildren.push(dataset);
      cardChildren.push(measurementTable(group.measurements || []));
      groupsWrap.appendChild(el("div", { className: "card" }, cardChildren));
    });
    root.appendChild(groupsWrap);

    if (report.slo && report.slo.length) root.appendChild(sloSection(report.slo, "SLA assessment"));
    if (report.grades && report.grades.length) root.appendChild(gradesSection(report.grades));

    var notes = notesSection(report);
    if (notes) root.appendChild(notes);
  }

  /* Method-name cell: name + a "baseline" tag and/or a muted description line. */
  function methodNameCell(method, isBaseline, description) {
    var head = el("span", { className: "method-head" }, [
      el("span", { text: method }),
      isBaseline ? el("span", { className: "baseline-tag", text: "baseline" }) : null,
    ]);
    if (!description) return head;
    return el("div", {}, [head, el("div", { className: "muted-sub", text: description })]);
  }

  /* SLA verdict cell: a badge whose tooltip is the declared target. */
  function slaCell(result, target) {
    if (!result) return "—";
    var span = el("span", { className: R.sloBadge(result), text: result });
    if (target) span.setAttribute("title", target);
    return span;
  }

  /* Legend explaining the SLA verdicts plus any distinct business-standard references. */
  function slaLegend(items) {
    var standards = [];
    var seen = {};
    (items || []).forEach(function (m) {
      if (m.slaStandard && !seen[m.slaStandard]) { seen[m.slaStandard] = 1; standards.push(m.slaStandard); }
    });
    var children = [
      el("p", {
        className: "table-note",
        text: "SLA — Meets: within budget · Exceeds: comfortably under (≤50% of the latency/alloc budget, or ≥1.5× the throughput target) · Miss: over budget. Hover a badge for its target.",
      }),
    ];
    standards.forEach(function (s) { children.push(el("p", { className: "muted-sub", text: "Standard: " + s })); });
    return el("div", {}, children);
  }

  /* Wrap an arbitrary cell value (DOM node or text) into a <td>. */
  function td(content, className) {
    if (content && content.nodeType === 1) return el("td", { className: className || "" }, [content]);
    return el("td", { className: className || "", text: content == null ? "—" : String(content) });
  }

  var SLA_SEVERITY = { Exceeds: 0, Meets: 1, Miss: 2 };

  /* The worst-verdict measurement in a matrix (Miss > Meets > Exceeds), or null when none carry an SLA. */
  function worstSlaItem(items) {
    var worst = null;
    var worstSeverity = -1;
    items.forEach(function (m) {
      if (!m.slaResult) return;
      var severity = SLA_SEVERITY[m.slaResult] != null ? SLA_SEVERITY[m.slaResult] : 1;
      if (severity > worstSeverity) { worstSeverity = severity; worst = m; }
    });
    return worst;
  }

  function minMean(items) {
    return Math.min.apply(null, items.map(function (m) { return m.meanNs || 0; }));
  }

  /* Detail table shown when a matrix (multi-parameter) method row is expanded. */
  function matrixBreakdown(items, hasSla) {
    var headers = [
      { label: "Parameters" },
      { label: "Mean", className: "num" },
      { label: "Allocated", className: "num" },
      { label: "Ratio", className: "num" },
    ];
    if (hasSla) headers.push({ label: "SLA" });
    var rows = items.map(function (m) {
      var cells = [
        paramLabel(m.parameters),
        { className: "num", text: R.fmtNs(m.meanNs) },
        { className: "num", text: R.fmtBytes(m.allocatedBytes) },
        { className: "num", text: m.ratioToBaseline != null ? R.fmtRatio(m.ratioToBaseline) : "—" },
      ];
      if (hasSla) cells.push(slaCell(m.slaResult, m.slaTarget));
      return cells;
    });
    var table = R.table(headers, rows);
    table.className += " wrap";
    return el("div", { className: "detail-inner" }, [el("h4", { text: "Per-parameter breakdown" }), table]);
  }

  /* Measurement table. Methods run across multiple parameter values (a matrix) collapse into one
     expandable summary row (mean/allocated range + worst SLA); click to reveal the per-parameter sweep. */
  function measurementTable(measurements) {
    if (!measurements.length) return el("p", { className: "empty", text: "No measurements." });
    var hasSla = measurements.some(function (m) { return m.slaResult; });

    var byMethod = {};
    var order = [];
    measurements.forEach(function (m) {
      if (!byMethod[m.method]) { byMethod[m.method] = []; order.push(m.method); }
      byMethod[m.method].push(m);
    });
    order.sort(function (a, b) { return minMean(byMethod[a]) - minMean(byMethod[b]); });

    var headers = [{ label: "" }, { label: "Method" }, { label: "Parameters" }, { label: "Mean", className: "num" }, { label: "Allocated", className: "num" }, { label: "Ratio", className: "num" }];
    if (hasSla) headers.push({ label: "SLA" });
    var colspan = headers.length;
    var thead = el("thead", {}, [el("tr", {}, headers.map(function (h) { return el("th", { text: h.label, className: h.className || "" }); }))]);
    var tbody = el("tbody");
    var anyMatrix = false;

    order.forEach(function (method) {
      var items = byMethod[method].slice().sort(function (a, b) { return (a.meanNs || 0) - (b.meanNs || 0); });
      var first = items[0];

      if (items.length === 1) {
        var single = [
          td("", "toggle-cell"),
          td(methodNameCell(first.method, first.isBaseline, first.description)),
          td(paramLabel(first.parameters)),
          td(R.fmtNs(first.meanNs), "num"),
          td(R.fmtBytes(first.allocatedBytes), "num"),
          td(first.ratioToBaseline != null ? R.fmtRatio(first.ratioToBaseline) : "—", "num"),
        ];
        if (hasSla) single.push(td(slaCell(first.slaResult, first.slaTarget)));
        tbody.appendChild(el("tr", {}, single));
        return;
      }

      anyMatrix = true;
      var means = items.map(function (m) { return m.meanNs || 0; });
      var allocs = items.map(function (m) { return m.allocatedBytes; }).filter(function (x) { return x != null; });
      var meanRange = R.fmtNs(Math.min.apply(null, means)) + " – " + R.fmtNs(Math.max.apply(null, means));
      var allocRange = allocs.length ? R.fmtBytes(Math.min.apply(null, allocs)) + " – " + R.fmtBytes(Math.max.apply(null, allocs)) : "—";
      var worst = worstSlaItem(items);

      var summary = [
        td("\u25B8", "toggle-cell"),
        td(methodNameCell(first.method, first.isBaseline, first.description)),
        td(items.length + " cases"),
        td(meanRange, "num"),
        td(allocRange, "num"),
        td("—", "num"),
      ];
      if (hasSla) summary.push(td(worst ? slaCell(worst.slaResult, worst.slaTarget) : "—"));
      var mainRow = el("tr", { className: "expandable" }, summary);
      tbody.appendChild(mainRow);

      var detailRow = el("tr", { className: "detail-row hidden" }, [el("td", { colspan: String(colspan) }, [matrixBreakdown(items, hasSla)])]);
      tbody.appendChild(detailRow);
      mainRow.addEventListener("click", function () {
        var hidden = detailRow.classList.toggle("hidden");
        mainRow.classList.toggle("open", !hidden);
        mainRow.querySelector(".toggle-cell").textContent = hidden ? "\u25B8" : "\u25BE";
      });
    });

    var children = [el("div", { className: "table-wrap" }, [el("table", {}, [thead, tbody])])];
    if (anyMatrix) children.push(el("p", { className: "table-note", text: "Click a row (\u25B8) to expand its per-parameter matrix breakdown (mean/allocated shown as a range across parameters; SLA shows the worst verdict)." }));
    if (measurements.some(function (m) { return m.isBaseline; })) {
      children.push(el("p", { className: "table-note", text: "The baseline tag marks the reference method; Ratio is each row's mean relative to it (1.00× = same speed)." }));
    }
    if (hasSla) children.push(slaLegend(measurements));
    return el("div", {}, children);
  }

  function comparisonSection(comparison) {
    var wrap = el("section", {}, [el("h2", { text: "Comparison" + (comparison.baseline ? " (baseline: " + comparison.baseline + ")" : "") })]);
    if (comparison.description) wrap.appendChild(el("p", { text: comparison.description }));
    var legend = paramLegend(comparison.parameters);
    if (legend) wrap.appendChild(legend);
    (comparison.groups || []).forEach(function (group) {
      // Group rows by parameter label so each card is one operation @ one size.
      var byLabel = {};
      var order = [];
      (group.rows || []).forEach(function (row) {
        var label = row.paramLabel || "—";
        if (!byLabel[label]) { byLabel[label] = []; order.push(label); }
        byLabel[label].push(row);
      });
      order.forEach(function (label) {
        var rowsForLabel = byLabel[label];
        var hasSla = rowsForLabel.some(function (r) { return r.slaResult; });
        var headers = [
          { label: "Algorithm" },
          { label: "Mean", className: "num" },
          { label: "vs baseline", className: "num" },
          { label: "Allocated", className: "num" },
        ];
        if (hasSla) headers.push({ label: "SLA" });
        wrap.appendChild(
          el("div", { className: "card" }, [
            el("h3", { text: group.axis + " @ " + label }),
            R.table(
              headers,
              rowsForLabel
                .slice()
                .sort(function (a, b) { return (a.meanNs || 0) - (b.meanNs || 0); })
                .map(function (r) {
                  var cells = [
                    r.algorithm,
                    { className: "num", text: R.fmtNs(r.meanNs) },
                    { className: "num", text: r.ratioToBaseline != null ? R.fmtRatio(r.ratioToBaseline) : "—" },
                    { className: "num", text: R.fmtBytes(r.allocatedBytes) },
                  ];
                  if (hasSla) cells.push(slaCell(r.slaResult, r.slaTarget));
                  return cells;
                })
            ),
          ])
        );
      });
    });
    return wrap;
  }

  /* ----------------------------------------------------------------- load */
  function renderLoad(report, root) {
    root.appendChild(el("section", {}, [el("div", { className: "meta", id: "meta" })]));
    R.mountMeta(root.querySelector("#meta"), metaChips(report));

    var lead = reportLead(report.description);
    if (lead) root.appendChild(lead);

    var casesById = {};
    (report.cases || []).forEach(function (c) { casesById[c.case] = c; });

    root.appendChild(scenariosSection(report.scenarios || [], casesById));
    if (report.cases && report.cases.length) root.appendChild(casesSection(report.cases));
    if (report.rollups && report.rollups.length) root.appendChild(rollupsSection(report.rollups));
    if (report.slo && report.slo.length) root.appendChild(sloSection(report.slo));
    if (report.grades && report.grades.length) root.appendChild(gradesSection(report.grades));

    var notes = notesSection(report);
    if (notes) root.appendChild(notes);
  }

  function scenariosSection(scenarios, casesById) {
    var headers = ["", "Scenario", "Profile", "p95", "p99", "avg", "Throughput", "Requests", "Checks", "Dropped"];
    var numeric = { p95: 1, p99: 1, avg: 1, Throughput: 1, Requests: 1, Checks: 1, Dropped: 1 };
    var thead = el("thead", {}, [
      el("tr", {}, headers.map(function (h) { return el("th", { text: h, className: numeric[h] ? "num" : "" }); })),
    ]);
    var tbody = el("tbody");
    var anyExpandable = false;

    scenarios.forEach(function (s) {
      var lat = s.latency || {};
      var hotspots = (s.hotspots || []).slice();
      var hasHot = hotspots.length > 0;
      if (hasHot) anyExpandable = true;

      var nameCell = el("td", {}, [el("span", { className: "scenario-name", text: s.name })]);
      var mainRow = el("tr", { className: hasHot ? "expandable" : "" }, [
        el("td", { className: "toggle-cell", text: hasHot ? "\u25B8" : "" }),
        nameCell,
        el("td", { text: s.profile || "—" }),
        el("td", { className: "num", text: R.fmtMs(lat.p95) }),
        el("td", { className: "num", text: R.fmtMs(lat.p99) }),
        el("td", { className: "num", text: R.fmtMs(lat.avg) }),
        el("td", { className: "num", text: R.fmtRate(s.throughput) }),
        el("td", { className: "num", text: R.fmtInt(s.requests) }),
        el("td", { className: "num", text: R.fmtPct(s.checksPass) }),
        el("td", { className: "num", text: R.fmtInt(s.droppedIterations) }),
      ]);
      tbody.appendChild(mainRow);

      if (hasHot) {
        var detailRow = el("tr", { className: "detail-row hidden" }, [
          el("td", { colspan: String(headers.length) }, [
            el("div", { className: "detail-inner" }, [scenarioBreakdown(hotspots, casesById)]),
          ]),
        ]);
        tbody.appendChild(detailRow);
        mainRow.addEventListener("click", function () {
          var hidden = detailRow.classList.toggle("hidden");
          mainRow.classList.toggle("open", !hidden);
          mainRow.querySelector(".toggle-cell").textContent = hidden ? "\u25B8" : "\u25BE";
        });
      }
    });

    var wrap = el("div", { className: "table-wrap" }, [el("table", {}, [thead, tbody])]);
    var children = [el("h2", { text: "Scenarios" })];
    if (anyExpandable)
      children.push(el("p", { className: "table-note", text: "Click a row (\u25B8) to expand its per-case latency breakdown." }));
    children.push(wrap);
    return el("section", {}, children);
  }

  /* Per-case latency breakdown shown when a scenario row is expanded. */
  function scenarioBreakdown(hotspots, casesById) {
    var headers = [{ label: "Case" }, { label: "avg", className: "num" }, { label: "p95", className: "num" }, { label: "p99", className: "num" }];
    var rows = hotspots
      .sort(function (a, b) { return (b.avg || 0) - (a.avg || 0); })
      .map(function (h) {
        var meta = (casesById || {})[h.case];
        var caseCell = meta && meta.description
          ? el("div", {}, [el("span", { className: "scenario-name", text: h.case }), el("div", { className: "muted-sub", text: meta.description })])
          : h.case;
        return [
          caseCell,
          { className: "num", text: R.fmtMs(h.avg) },
          { className: "num", text: R.fmtMs(h.p95) },
          { className: "num", text: R.fmtMs(h.p99) },
        ];
      });
    var table = R.table(headers, rows);
    table.className += " wrap";
    return el("div", {}, [el("h4", { text: "Per-case breakdown" }), table]);
  }

  function rollupsSection(rollups) {
    var headers = [
      { label: "Endpoint" },
      { label: "Requests", className: "num" },
      { label: "Checks", className: "num" },
      { label: "Status", className: "num" },
      { label: "Shape", className: "num" },
      { label: "Latency", className: "num" },
    ];
    var rows = rollups.map(function (r) {
      return [
        r.endpoint,
        { className: "num", text: R.fmtInt(r.totalRequests) },
        { className: "num", text: R.fmtPct(r.checksPass) },
        { className: "num", text: R.fmtPct(r.statusPass) },
        { className: "num", text: R.fmtPct(r.shapePass) },
        { className: "num", text: R.fmtPct(r.latencyPass) },
      ];
    });
    return el("section", {}, [el("h2", { text: "Endpoint rollups" }), R.table(headers, rows)]);
  }

  function sloSection(slo, title) {
    var rows = slo.map(function (row) {
      return [
        { className: "no-break", text: row.area },
        row.target,
        { className: "num", text: row.latest },
        { html: '<span class="' + R.sloBadge(row.result) + '">' + row.result + "</span>" },
      ];
    });
    var table = R.table([{ label: "Area" }, { label: "Target" }, { label: "Latest", className: "num" }, { label: "Result" }], rows);
    table.className += " wrap";
    return el("section", {}, [
      el("h2", { text: title || "SLO assessment" }),
      table,
    ]);
  }

  function gradesSection(grades) {
    var rows = grades.map(function (g) {
      return [
        g.category,
        { html: '<span class="' + R.gradeClass(g.grade) + '">' + g.grade + "</span>" },
        g.rationale,
      ];
    });
    return el("section", {}, [
      el("h2", { text: "Grades" }),
      R.table([{ label: "Category" }, { label: "Grade" }, { label: "Rationale" }], rows),
    ]);
  }

  function joinList(arr) {
    return arr && arr.length ? arr.join(", ") : "—";
  }

  function casesSection(cases) {
    var headers = [
      { label: "Case" },
      { label: "Endpoint" },
      { label: "Where", className: "num" },
      { label: "Filters" },
      { label: "Sort" },
      { label: "Includes" },
      { label: "Select fields", className: "num" },
    ];
    var rows = cases.map(function (c) {
      var caseCell = c.description
        ? el("div", {}, [el("span", { text: c.case }), el("div", { className: "muted-sub", text: c.description })])
        : c.case;
      return [
        caseCell,
        c.endpoint || "—",
        { className: "num", text: c.whereClauses != null ? String(c.whereClauses) : "—" },
        joinList(c.filters),
        joinList(c.sortFields),
        joinList(c.includes),
        { className: "num", text: c.selectionFieldCount != null ? String(c.selectionFieldCount) : "—" },
      ];
    });
    var table = R.table(headers, rows);
    table.className += " wrap";
    return el("section", {}, [el("h2", { text: "Query cases" }), table]);
  }

  /* --------------------------------------------------------------- dispatch */
  function renderReport(report, root) {
    root.innerHTML = "";
    if (!report || !report.type) {
      R.renderError(root, "Report is missing its 'type' discriminator.");
      return;
    }
    if (report.type === "micro") renderMicro(report, root);
    else if (report.type === "load") renderLoad(report, root);
    else R.renderError(root, "Unknown report type: " + report.type);
  }

  window.LyoBenchViewer = { renderReport: renderReport };
})();
