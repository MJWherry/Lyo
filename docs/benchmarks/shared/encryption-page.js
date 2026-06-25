(function () {
  var R = window.BenchmarkDashboard;
  var COVERAGE = [
    ["AES-GCM", "AesGcmEncryptionBenchmarks"],
    ["ChaCha20-Poly1305", "ChaCha20Poly1305EncryptionBenchmarks"],
    ["AES-CCM", "AesCcmEncryptionBenchmarks"],
    ["AES-SIV", "AesSivEncryptionBenchmarks"],
    ["XChaCha20-Poly1305", "XChaCha20Poly1305EncryptionBenchmarks"],
    ["RSA (2048 OAEP-SHA256)", "RsaEncryptionBenchmarks"],
    ["AES-GCM-RSA hybrid", "AesGcmRsaEncryptionBenchmarks"],
    ["Two-key envelope", "TwoKeyEncryptionBenchmarks"],
    ["Large-file streaming", "LargeFileStreamingBenchmarks"],
    ["Algorithm comparison", "AlgorithmComparisonBenchmarks"],
  ];
  var DEDICATED = [
    "AesGcmEncryptionBenchmarks",
    "ChaCha20Poly1305EncryptionBenchmarks",
    "AesCcmEncryptionBenchmarks",
    "AesSivEncryptionBenchmarks",
    "XChaCha20Poly1305EncryptionBenchmarks",
    "RsaEncryptionBenchmarks",
    "AesGcmRsaEncryptionBenchmarks",
    "TwoKeyEncryptionBenchmarks",
    "LargeFileStreamingBenchmarks",
  ];

  try {
    var data = window.__BENCHMARK_ENCRYPTION__;
    if (!data) throw new Error("Missing encryption data. Run build-manifests.py and reload.");
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
        [{ label: "Algorithm / pattern" }, { label: "Class" }, { label: "Present" }],
        COVERAGE.map(function (pair) {
          var className = pair[1];
          return [pair[0], className, data.classes && data.classes[className] && data.classes[className].length ? "✅" : "—"];
        })
      )
    );

    var cmp = data.comparisonTables || {};
    document.getElementById("comparison").replaceChildren(
      R.comparisonTable("Encrypt (symmetric comparison)", cmp.encryptOrCompress || []),
      R.comparisonTable("Decrypt (symmetric comparison)", cmp.decryptOrDecompress || [])
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
        R.el("p", { text: "AES-GCM — default for bulk encryption on AES-NI hardware." }),
        R.el("p", { text: "ChaCha20-Poly1305 — software-only or ChaCha compatibility; typically ~30–45% slower than GCM here." }),
        R.el("p", { text: "XChaCha20-Poly1305 — when a 24-byte nonce is required; expect ~3× GCM at 1 MB+ on this stack." }),
        R.el("p", { text: "AES-CCM / AES-SIV — protocol/properties choice, not throughput leaders." }),
        R.el("p", { text: "RSA / AES-GCM-RSA — key exchange and small secrets; hybrid for large blobs." }),
        R.el("p", { text: "Two-key envelope — key rotation/compliance; avoid for latency-sensitive sub-4 KB hot paths." }),
      ])
    );

    document.getElementById("notes").replaceChildren(
      R.el("h2", { text: "Notes" }),
      R.el("div", { className: "callout warn" }, [
        R.el("p", { text: "XChaCha benchmarks use an explicit 32-byte key (KeyStore nonce sizing differs for 12-byte nonces)." }),
        R.el("p", { text: "AlgorithmComparison @ 100 MB may OOM with all five symmetric algorithms — use dedicated classes or streaming benchmarks." }),
        R.el("p", { text: "Payloads use RandomNumberGenerator.Fill (random bytes)." }),
      ])
    );
  } catch (error) {
    R.renderError(document.querySelector(".page"), error);
  }
})();
