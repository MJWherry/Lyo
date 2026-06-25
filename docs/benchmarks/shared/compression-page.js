(function () {
  var R = window.BenchmarkDashboard;
  var COVERAGE = [
    ["Algorithm comparison", "AlgorithmComparisonBenchmarks"],
    ["GZip-specific", "GZipCompressionBenchmarks"],
    ["Large-file streaming", "LargeFileStreamingBenchmarks"],
  ];
  var DEDICATED = ["GZipCompressionBenchmarks", "LargeFileStreamingBenchmarks"];

  try {
    var data = window.__BENCHMARK_COMPRESSION__;
    if (!data) throw new Error("Missing compression data. Run build-manifests.py and reload.");
    var env = data.environment || {};

    R.mountMeta(document.getElementById("meta"), [
      "Run " + data.runId,
      env.benchmarkDotNet ? "BenchmarkDotNet " + env.benchmarkDotNet : null,
      env.runtime,
      env.cpu,
    ].filter(Boolean));

    document.getElementById("coverage").replaceChildren(
      R.el("h2", { text: "Benchmark Coverage" }),
      R.table(
        [{ label: "Suite" }, { label: "Class" }, { label: "Present" }],
        COVERAGE.map(function (pair) {
          var className = pair[1];
          return [pair[0], className, data.classes && data.classes[className] && data.classes[className].length ? "✅" : "—"];
        })
      )
    );

    var cmp = data.comparisonTables || {};
    document.getElementById("comparison").replaceChildren(
      R.comparisonTable("Compress (GZip baseline = 1.00)", cmp.encryptOrCompress || []),
      R.comparisonTable("Decompress (vs GZip at same size)", cmp.decryptOrDecompress || [])
    );

    var dedicated = R.el("div", {});
    dedicated.appendChild(R.el("h2", { text: "Dedicated Benchmark Classes" }));
    DEDICATED.forEach(function (className) {
      dedicated.appendChild(
        R.el("div", { className: "card" }, [
          R.el("h3", { text: className }),
          R.dedicatedClassTable((data.classes && data.classes[className]) || []),
        ])
      );
    });
    document.getElementById("dedicated").replaceChildren(dedicated);

    document.getElementById("recommendations").replaceChildren(
      R.el("h2", { text: "Recommendations" }),
      R.el("div", { className: "callout" }, [
        R.el("p", { text: "LZ4 — hot paths, small/medium payloads, file-storage FastAlgorithm." }),
        R.el("p", { text: "Zstd — large files, streaming writes, balanced ratio + speed." }),
        R.el("p", { text: "Brotli — default HTTP/API response compression." }),
        R.el("p", { text: "GZip / Deflate / ZLib — legacy compatibility." }),
        R.el("p", { text: "LZMA / XZ — offline archival when CPU time is cheap." }),
        R.el("p", { text: "BZip2 — Linux .tar.bz2 interop only; skip incompressible content." }),
      ])
    );

    document.getElementById("notes").replaceChildren(
      R.el("h2", { text: "Notes" }),
      R.el("div", { className: "callout warn" }, [
        R.el("p", { text: "Benchmark payloads use RandomNumberGenerator.Fill — worst case for compression ratio; speed rankings still apply." }),
        R.el("p", { text: "BZip2 shows pathological allocation on random/incompressible data (SharpZipLib QSort3); real text/logs stay near fixed block-buffer cost." }),
        R.el("p", { text: "LargeFileStreaming uses real FileStreams and requires sufficient disk space for 1–2 GB cases." }),
      ])
    );
  } catch (error) {
    R.renderError(document.querySelector(".page"), error);
  }
})();
