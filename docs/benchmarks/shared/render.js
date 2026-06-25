/* Shared benchmark dashboard rendering utilities (classic script — works from file://). */
(function () {
  function el(tag, attrs, children) {
    attrs = attrs || {};
    children = children || [];
    var node = document.createElement(tag);
    Object.keys(attrs).forEach(function (key) {
      var value = attrs[key];
      if (key === "className") node.className = value;
      else if (key === "text") node.textContent = value;
      else if (key === "html") node.innerHTML = value;
      else node.setAttribute(key, value);
    });
    children.forEach(function (child) {
      if (child == null) return;
      node.appendChild(typeof child === "string" ? document.createTextNode(child) : child);
    });
    return node;
  }

  function fmtMs(value, digits) {
    digits = digits === undefined ? 1 : digits;
    if (value == null || isNaN(value)) return "—";
    return Number(value).toFixed(digits) + " ms";
  }

  function fmtRate(value, digits) {
    digits = digits === undefined ? 2 : digits;
    if (value == null || isNaN(value)) return "—";
    return Number(value).toFixed(digits) + " req/s";
  }

  function fmtPct(value, digits) {
    digits = digits === undefined ? 2 : digits;
    if (value == null || isNaN(value)) return "—";
    return Number(value).toFixed(digits) + "%";
  }

  function fmtInt(value) {
    if (value == null || isNaN(value)) return "—";
    return Number(value).toLocaleString();
  }

  function fmtDelta(value, unit, invertGood) {
    unit = unit || "";
    if (value == null || isNaN(value) || value === 0) return "—";
    var sign = value > 0 ? "+" : "";
    var good = invertGood ? value < 0 : value > 0;
    var cls = good ? "delta-good" : "delta-bad";
    var text = sign + Number(value).toFixed(unit === " ms" ? 1 : 2) + unit;
    return '<span class="' + cls + '">' + text + "</span>";
  }

  function gradeClass(grade) {
    var normalized = String(grade || "")
      .replace(/\+/g, "plus")
      .replace(/-/g, "minus")
      .replace(/\s/g, "")
      .toLowerCase();
    return "grade grade-" + (normalized || "c");
  }

  function sloBadge(result) {
    var text = String(result || "").toLowerCase();
    if (text.indexOf("exceed") >= 0) return "badge exceeds";
    if (text.indexOf("meet") >= 0 || text.indexOf("slightly") >= 0) return "badge meets";
    return "badge miss";
  }

  function table(headers, rows) {
    var thead = el("thead", {}, [
      el("tr", {}, headers.map(function (h) {
        return el("th", { text: h.label, className: h.className || "" });
      })),
    ]);
    var tbody = el(
      "tbody",
      {},
      rows.map(function (cells) {
        return el(
          "tr",
          {},
          cells.map(function (cell) {
            if (cell && typeof cell === "object" && cell.nodeType === 1) {
              return el("td", { className: cell.className || "" }, [cell]);
            }
            var className = cell && typeof cell === "object" ? cell.className || "" : "";
            var content = cell && typeof cell === "object" ? (cell.html != null ? cell.html : cell.text != null ? cell.text : "") : cell != null ? cell : "";
            var td = el("td", { className: className });
            if (typeof content === "string" && content.indexOf("<") >= 0) td.innerHTML = content;
            else td.textContent = content != null ? content : "";
            return td;
          })
        );
      })
    );
    return el("div", { className: "table-wrap" }, [el("table", {}, [thead, tbody])]);
  }

  function renderError(container, error) {
    container.innerHTML = "";
    container.appendChild(
      el("div", { className: "error-box" }, [
        el("strong", { text: "Could not load benchmark data." }),
        el("p", { text: String(error && error.message ? error.message : error) }),
        el("p", {
          text: "Run python3 scripts/benchmarks/build-manifests.py after benchmarks, then reload this page.",
        }),
      ])
    );
  }

  function mountMeta(container, items) {
    container.innerHTML = "";
    items.forEach(function (item) {
      container.appendChild(el("span", { className: "chip", text: item }));
    });
  }

  function groupByProfile(suites, endpointPrefix) {
    return ["load", "stress", "spike", "soak"].map(function (profile) {
      return {
        profile: profile,
        query: suites.find(function (s) { return s.name === endpointPrefix + "_" + profile; }),
        queryproject: suites.find(function (s) { return s.name === "queryproject_" + profile; }),
      };
    });
  }

  function comparisonRowsBySize(tableRows) {
    var bySize = new Map();
    (tableRows || []).forEach(function (row) {
      if (!bySize.has(row.sizeLabel)) bySize.set(row.sizeLabel, []);
      bySize.get(row.sizeLabel).push(row);
    });
    return Array.from(bySize.entries());
  }

  function dedicatedClassTable(classRows) {
    if (!classRows || !classRows.length) {
      return el("p", { className: "empty", text: "No data for this benchmark class." });
    }
    var methods = Array.from(new Set(classRows.map(function (r) { return r.method; })));
    var sizes = Array.from(new Set(classRows.map(function (r) { return r.dataSizeLabel; })));
    var lookup = new Map(classRows.map(function (r) { return [r.method + "|" + r.dataSizeLabel, r]; }));
    var headers = [{ label: "Method" }].concat(sizes.map(function (s) { return { label: s, className: "num" }; }));
    var rows = methods.map(function (method) {
      return [method].concat(
        sizes.map(function (size) {
          var hit = lookup.get(method + "|" + size);
          return { className: "num", text: hit && hit.mean ? hit.mean : "—" };
        })
      );
    });
    return table(headers, rows);
  }

  function renderIndexCards(container, cards) {
    container.innerHTML = "";
    cards.forEach(function (card) {
      container.appendChild(
        el("a", { className: "card card-link", href: card.href }, [
          el("h2", { text: card.title }),
          el("p", { text: card.description }),
          el("div", { className: "metric-value", text: card.headline != null ? card.headline : "—" }),
          el("div", { className: "metric-label", text: card.subline != null ? card.subline : "" }),
        ])
      );
    });
  }

  function comparisonTable(title, rows) {
    var R = window.BenchmarkDashboard;
    var sections = comparisonRowsBySize(rows).map(function (entry) {
      var sizeLabel = entry[0];
      var entries = entry[1];
      return el("div", { className: "card" }, [
        el("h3", { text: title + " @ " + sizeLabel }),
        table(
          [
            { label: "Algorithm" },
            { label: "Mean", className: "num" },
            { label: "Ratio", className: "num" },
            { label: "vs baseline", className: "num" },
            { label: "Allocated", className: "num" },
          ],
          entries.map(function (r) {
            return [
              r.algorithm,
              { className: "num", text: r.mean != null ? r.mean : "—" },
              { className: "num", text: r.ratio != null ? r.ratio.toFixed(2) + "×" : "—" },
              { className: "num", text: r.ratioVsBaseline != null ? r.ratioVsBaseline.toFixed(2) + "×" : "—" },
              { className: "num", text: r.allocated != null ? r.allocated : "—" },
            ];
          })
        ),
      ]);
    });
    var wrap = el("div", {});
    wrap.appendChild(el("h2", { text: title }));
    sections.forEach(function (s) { wrap.appendChild(s); });
    return wrap;
  }

  window.BenchmarkDashboard = {
    el: el,
    fmtMs: fmtMs,
    fmtRate: fmtRate,
    fmtPct: fmtPct,
    fmtInt: fmtInt,
    fmtDelta: fmtDelta,
    gradeClass: gradeClass,
    sloBadge: sloBadge,
    table: table,
    renderError: renderError,
    mountMeta: mountMeta,
    groupByProfile: groupByProfile,
    comparisonRowsBySize: comparisonRowsBySize,
    dedicatedClassTable: dedicatedClassTable,
    renderIndexCards: renderIndexCards,
    comparisonTable: comparisonTable,
  };
})();
