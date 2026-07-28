/* Builds the dashboard index cards from window.LyoBench.registry (emitted by build_manifests.py). */
(function () {
    var R = window.BenchmarkDashboard;

    // Fallback descriptions per report name; the registry's own description (from the report) wins when present.
    var DESCRIPTIONS = {
        encryption: "AES-GCM/CCM/SIV and ChaCha/XChaCha encrypt/decrypt across payload sizes for Lyo.Encryption.",
        compression: "GZip/Deflate/Zstd/LZ4/LZMA/BZip2/XZ/Brotli/ZLib compress and decompress for Lyo.Compression.",
        hashing: "SHA-2/MD5 digests, HMAC, hashing streams, and hex encode/decode for Lyo.Hashing.",
        cache: "Local IMemoryCache vs FusionCache, payload framing, and Redis backplane for Lyo.Cache.",
        query: "Where-clause, sort, projection, mapping, and end-to-end CRUD for Lyo.Query.",
        csv: "Typed write/read, streaming parse, and DataTable round-trips for Lyo.Csv.",
        xlsx: "ClosedXML write and ExcelDataReader read paths for Lyo.Xlsx.",
        lock: "Local vs Redis acquire/release and execute-with-lock for Lyo.Lock.",
        "query-api": "k6 load/stress/spike/soak across the Query and QueryProject API endpoints.",
    };

    function init() {
        var container = document.getElementById("cards");
        if (!container) return;
        var registry = (window.LyoBench && window.LyoBench.registry) || [];

        if (!registry.length) {
            container.appendChild(
                R.el("div", {className: "error-box"}, [
                    R.el("strong", {text: "No benchmark reports found."}),
                    R.el("p", {text: "Run scripts/benchmarks/run-dotnet-benchmarks.sh (or build_manifests.py) to generate reports, then reload."}),
                ])
            );
            return;
        }

        var cards = registry.map(function (entry) {
            return {
                href: "report.html#" + encodeURIComponent(entry.name),
                title: entry.title,
                // Cards stay concise (curated one-liners); the full methodology is the lead on the report page.
                description: DESCRIPTIONS[entry.name] || entry.description || (entry.type === "load" ? "Load test report." : "Micro-benchmark report."),
                badge: entry.type === "load" ? "Load test" : "Micro-benchmark",
            };
        });
        R.renderIndexCards(container, cards);
    }

    init();
})();
