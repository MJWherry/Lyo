(function () {
  var R = window.BenchmarkDashboard;
  var cardsEl = document.getElementById("cards");
  try {
    var k6 = window.__BENCHMARK_K6__;
    var encryption = window.__BENCHMARK_ENCRYPTION__;
    var compression = window.__BENCHMARK_COMPRESSION__;
    if (!k6 || !encryption || !compression) {
      throw new Error("Missing benchmark data scripts. Run build-manifests.py and reload.");
    }
    var k6Spike = (k6.suites || []).find(function (s) { return s.name === "query_spike"; });
    var encCmp = (encryption.comparisonTables && encryption.comparisonTables.encryptOrCompress || []).find(function (r) {
      return r.algorithm === "AesGcm" && r.sizeLabel === "1 MB";
    });
    var compCmp = (compression.comparisonTables && compression.comparisonTables.encryptOrCompress || []).find(function (r) {
      return r.algorithm === "LZ4" && r.sizeLabel === "1 MB";
    });
    R.renderIndexCards(cardsEl, [
      {
        href: "k6.html",
        title: "Query API (k6)",
        description: "Load, stress, spike, and soak profiles for /person/query and /person/QueryProject.",
        headline: k6Spike ? k6Spike.p95.toFixed(0) + " ms p95 spike" : "k6 matrix",
        subline: "Run " + k6.runId + " · updated " + new Date(k6.generatedAt).toLocaleString(),
      },
      {
        href: "encryption.html",
        title: "Encryption",
        description: "BenchmarkDotNet suite for AES-GCM, ChaCha, RSA, hybrid, and streaming paths.",
        headline: encCmp && encCmp.mean ? encCmp.mean : "Encryption benchmarks",
        subline: "Run " + encryption.runId + " · " + ((encryption.environment && encryption.environment.runtime) || "BenchmarkDotNet"),
      },
      {
        href: "compression.html",
        title: "Compression",
        description: "Algorithm comparison and streaming benchmarks for Lyo.Compression.",
        headline: compCmp && compCmp.mean ? compCmp.mean : "Compression benchmarks",
        subline: "Run " + compression.runId + " · " + ((compression.environment && compression.environment.runtime) || "BenchmarkDotNet"),
      },
    ]);
  } catch (error) {
    R.renderError(cardsEl, error);
  }
})();
