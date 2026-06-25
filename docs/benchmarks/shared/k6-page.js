(function () {
  var R = window.BenchmarkDashboard;
  try {
    var data = window.__BENCHMARK_K6__;
    if (!data) throw new Error("Missing k6 data. Run build-manifests.py and reload.");

    R.mountMeta(document.getElementById("meta"), [
      "Run " + data.runId,
      "Generated " + new Date(data.generatedAt).toLocaleString(),
      data.workloadNote ? "Sorting mostly disabled" : "",
    ].filter(Boolean));

    document.getElementById("caveats").replaceChildren(
      R.el("h2", { text: "Environment" }),
      R.el("div", { className: "callout" }, [
        R.el("p", { text: "Single host: API + PostgreSQL + Redis + k6 on one machine with dev tooling active. Intentionally pessimistic vs split production infra." }),
      ]),
      R.el("div", { className: "callout warn" }, [
        R.el("p", { text: "Cache bypass + no indexes: the k6 matrix bypasses the caching system and runs against an unindexed DB, so latencies are worst-case for query/join cost." }),
        R.el("p", { text: data.workloadNote || "" }),
      ])
    );

    var deltaByName = new Map((data.previous && data.previous.deltas || []).map(function (d) { return [d.name, d]; }));
    document.getElementById("combined").replaceChildren(
      R.el("h2", { text: "Combined Results" }),
      R.table(
        [
          { label: "Suite" },
          { label: "Avg", className: "num" },
          { label: "p95", className: "num" },
          { label: "p99", className: "num" },
          { label: "Throughput", className: "num" },
          { label: "Requests", className: "num" },
          { label: "Checks", className: "num" },
          { label: "Dropped", className: "num" },
        ].concat(data.previous ? [{ label: "p95 Δ", className: "num" }] : []),
        (data.suites || []).map(function (s) {
          var d = deltaByName.get(s.name);
          return [
            s.name,
            { className: "num", text: R.fmtMs(s.avg) },
            { className: "num", text: R.fmtMs(s.p95) },
            { className: "num", text: R.fmtMs(s.p99) },
            { className: "num", text: R.fmtRate(s.throughput) },
            { className: "num", text: R.fmtInt(s.requests) },
            { className: "num", text: R.fmtPct(s.checksPass) },
            { className: "num", text: R.fmtInt(s.droppedIterations) },
          ].concat(data.previous ? [{ className: "num", html: R.fmtDelta(d && d.p95Delta, " ms", true) }] : []);
        })
      )
    );

    var rollup = data.rollup || {};
    document.getElementById("rollup").replaceChildren(
      R.el("h2", { text: "Endpoint Rollup" }),
      R.table(
        [
          { label: "Endpoint family" },
          { label: "Requests", className: "num" },
          { label: "Checks", className: "num" },
          { label: "Status", className: "num" },
          { label: "Shape", className: "num" },
          { label: "Latency", className: "num" },
        ],
        [
          [" /person/query", rollup.query],
          [" /person/QueryProject", rollup.queryproject],
        ]
          .filter(function (pair) { return pair[1] && pair[1].totalRequests; })
          .map(function (pair) {
            var label = pair[0];
            var r = pair[1];
            return [
              label,
              { className: "num", text: R.fmtInt(r.totalRequests) },
              { className: "num", text: R.fmtPct(r.checksPass) },
              { className: "num", text: R.fmtPct(r.statusPass) },
              { className: "num", text: R.fmtPct(r.shapePass) },
              { className: "num", text: R.fmtPct(r.latencyPass) },
            ];
          })
      )
    );

    var profileCards = R.groupByProfile(data.suites || [], "query").map(function (item) {
      var profile = item.profile;
      var query = item.query;
      var queryproject = item.queryproject;
      return R.el("article", { className: "card" }, [
        R.el("h3", { text: profile.charAt(0).toUpperCase() + profile.slice(1) }),
        R.table(
          [
            { label: "Endpoint" },
            { label: "p95", className: "num" },
            { label: "Throughput", className: "num" },
            { label: "Checks", className: "num" },
          ],
          [
            query ? ["/person/query", { className: "num", text: R.fmtMs(query.p95) }, { className: "num", text: R.fmtRate(query.throughput) }, { className: "num", text: R.fmtPct(query.checksPass) }] : null,
            queryproject ? ["/person/QueryProject", { className: "num", text: R.fmtMs(queryproject.p95) }, { className: "num", text: R.fmtRate(queryproject.throughput) }, { className: "num", text: R.fmtPct(queryproject.checksPass) }] : null,
          ].filter(Boolean)
        ),
      ]);
    });
    document.getElementById("profiles").replaceChildren(R.el("h2", { text: "Profiles" }), R.el("div", { className: "profile-grid" }, profileCards));

    document.getElementById("slo").replaceChildren(
      R.el("h2", { text: "Business Standards (single-instance)" }),
      R.table(
        [{ label: "Area" }, { label: "Target p95" }, { label: "Latest" }, { label: "Result" }],
        (data.sloAssessment || []).map(function (row) {
          return [row.area, row.target, row.latest, { html: '<span class="' + R.sloBadge(row.result) + '">' + row.result + "</span>" }];
        })
      )
    );

    document.getElementById("grades").replaceChildren(
      R.el("h2", { text: "Grades" }),
      R.table(
        [{ label: "Category" }, { label: "Grade" }, { label: "Rationale" }],
        (data.grades || []).map(function (g) {
          return [g.category, { html: '<span class="' + R.gradeClass(g.grade) + '">' + g.grade + "</span>" }, g.rationale];
        })
      )
    );

    function hotspotSection(title, suites) {
      var rows = [];
      suites.forEach(function (suite) {
        (suite.hotspots || []).slice(0, 4).forEach(function (hot) {
          rows.push([suite.name, hot.case, { className: "num", text: R.fmtMs(hot.p95) }]);
        });
      });
      return R.el("div", {}, [
        R.el("h3", { text: title }),
        rows.length
          ? R.table([{ label: "Suite" }, { label: "Case" }, { label: "p95", className: "num" }], rows)
          : R.el("p", { className: "empty", text: "No hotspot data." }),
      ]);
    }

    document.getElementById("hotspots").replaceChildren(
      R.el("h2", { text: "Hotspots" }),
      hotspotSection("Stress", (data.suites || []).filter(function (s) { return s.name.endsWith("_stress"); })),
      hotspotSection("Spike", (data.suites || []).filter(function (s) { return s.name.endsWith("_spike"); }))
    );

    var deltasSection = document.getElementById("deltas");
    if (data.previous && data.previous.deltas && data.previous.deltas.length) {
      deltasSection.replaceChildren(
        R.el("h2", { text: "Comparison vs " + data.previous.runId }),
        R.table(
          [
            { label: "Suite" },
            { label: "p95 Δ", className: "num" },
            { label: "Throughput Δ", className: "num" },
            { label: "Checks Δ", className: "num" },
            { label: "Dropped Δ", className: "num" },
          ],
          data.previous.deltas.map(function (d) {
            return [
              d.name,
              { className: "num", html: R.fmtDelta(d.p95Delta, " ms", true) },
              { className: "num", html: R.fmtDelta(d.throughputDelta, " req/s") },
              { className: "num", html: R.fmtDelta(d.checksPassDelta, " pp") },
              { className: "num", html: R.fmtDelta(-d.droppedIterationsDelta, "", false) },
            ];
          })
        )
      );
    } else {
      deltasSection.replaceChildren();
    }
  } catch (error) {
    R.renderError(document.querySelector(".page"), error);
  }
})();
