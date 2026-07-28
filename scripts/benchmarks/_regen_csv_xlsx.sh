#!/usr/bin/env bash
set -euo pipefail
ROOT=/home/matt/RiderProjects/Lyo
cd "$ROOT"
common=(--join --warmupCount 3 --iterationCount 7 --launchCount 1)

echo "==> CSV"
dotnet run -c Release --project Lyo.Net/Data/Csv/Lyo.Csv.Benchmarks/Lyo.Csv.Benchmarks.csproj -- \
  "${common[@]}" --artifacts "$ROOT/Lyo.Net/Data/Csv/Lyo.Csv.Benchmarks/BenchmarkDotNet.Artifacts" --filter '*' \
  2>&1 | tr '\r' '\n' | tail -n 4

echo "==> XLSX"
dotnet run -c Release --project Lyo.Net/Data/Xlsx/Lyo.Xlsx.Benchmarks/Lyo.Xlsx.Benchmarks.csproj -- \
  "${common[@]}" --artifacts "$ROOT/Lyo.Net/Data/Xlsx/Lyo.Xlsx.Benchmarks/BenchmarkDotNet.Artifacts" --filter '*' \
  2>&1 | tr '\r' '\n' | tail -n 4

echo "==> manifests"
python3 scripts/benchmarks/build_manifests.py >/dev/null 2>&1
echo "DONE_REGEN"
