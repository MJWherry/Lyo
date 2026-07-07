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

  /* Format a nanosecond mean as ns / µs / ms with sensible precision. */
  function fmtNs(value) {
    if (value == null || isNaN(value)) return "—";
    var ns = Number(value);
    if (ns >= 1e6) return (ns / 1e6).toFixed(2) + " ms";
    if (ns >= 1e3) return (ns / 1e3).toFixed(2) + " µs";
    return ns.toFixed(1) + " ns";
  }

  /* Format a byte count as B / KB / MB. */
  function fmtBytes(value) {
    if (value == null || isNaN(value)) return "—";
    var bytes = Number(value);
    if (bytes >= 1048576) return (bytes / 1048576).toFixed(2) + " MB";
    if (bytes >= 1024) return (bytes / 1024).toFixed(2) + " KB";
    return bytes.toFixed(0) + " B";
  }

  function fmtRatio(value) {
    if (value == null || isNaN(value)) return "—";
    return Number(value).toFixed(2) + "×";
  }

  /* Percent change vs a prior run. lowerIsBetter=true for latency/allocation (negative = green). */
  function fmtDeltaPct(pct, lowerIsBetter) {
    if (pct == null || isNaN(pct)) return "—";
    var n = Number(pct);
    var improved = lowerIsBetter ? n < -0.5 : n > 0.5;
    var regressed = lowerIsBetter ? n > 0.5 : n < -0.5;
    var cls = improved ? "delta-good" : regressed ? "delta-bad" : "";
    var sign = n >= 0 ? "+" : "";
    return { className: ("num " + cls).trim(), text: sign + n.toFixed(1) + "%" };
  }

  function hasDeltaField(items, field) {
    return (items || []).some(function (item) {
      return item && item[field] != null && !isNaN(item[field]);
    });
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
              // Wrap the node WITHOUT copying its own class onto the <td> — otherwise a badge span
              // (class "badge exceeds") would style the whole cell as a pill. Matches the viewer's
              // manual td() helper so nested/comparison tables render like the parent tables.
              return el("td", {}, [cell]);
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

  /* Index cards: each card = { href, title, description, badge }. */
  function renderIndexCards(container, cards) {
    container.innerHTML = "";
    cards.forEach(function (card) {
      var heading = el("h2", { text: card.title });
      if (card.badge) heading.appendChild(el("span", { className: "badge type-badge", text: card.badge }));
      container.appendChild(
        el("a", { className: "card card-link", href: card.href }, [
          heading,
          el("p", { text: card.description || "" }),
        ])
      );
    });
  }

  window.BenchmarkDashboard = {
    el: el,
    fmtMs: fmtMs,
    fmtRate: fmtRate,
    fmtPct: fmtPct,
    fmtInt: fmtInt,
    fmtNs: fmtNs,
    fmtBytes: fmtBytes,
    fmtRatio: fmtRatio,
    fmtDeltaPct: fmtDeltaPct,
    hasDeltaField: hasDeltaField,
    gradeClass: gradeClass,
    sloBadge: sloBadge,
    table: table,
    renderError: renderError,
    mountMeta: mountMeta,
    renderIndexCards: renderIndexCards,
  };
})();
