#!/usr/bin/env node
/**
 * Packs a curated allowlist of Lyo packages and writes nupkg sizes to
 * apps/portfolio/content/package-sizes.json for the /packages page.
 *
 * Usage (from repo root or apps/portfolio):
 *   node scripts/portfolio/measure-package-sizes.mjs
 *   npm run measure-sizes   # from apps/portfolio
 */

import { spawnSync } from "node:child_process";
import { createHash } from "node:crypto";
import {
  existsSync,
  mkdirSync,
  readdirSync,
  readFileSync,
  rmSync,
  statSync,
  writeFileSync,
} from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "../..");
const outFile = path.join(repoRoot, "apps/portfolio/content/package-sizes.json");
const packDir = path.join(repoRoot, ".portfolio-pack-out");

/** @type {{ id: string, csproj: string }[]} */
const allowlist = [
  { id: "Lyo.Cache", csproj: "Lyo.Net/Core/Cache/Lyo.Cache/Lyo.Cache.csproj" },
  { id: "Lyo.Lock", csproj: "Lyo.Net/Core/Lock/Lyo.Lock/Lyo.Lock.csproj" },
  { id: "Lyo.Privacy", csproj: "Lyo.Net/Core/Privacy/Lyo.Privacy/Lyo.Privacy.csproj" },
  { id: "Lyo.Resilience", csproj: "Lyo.Net/Core/Resilience/Lyo.Resilience/Lyo.Resilience.csproj" },
  { id: "Lyo.People.Postgres", csproj: "Lyo.Net/Core/People/Lyo.People.Postgres/Lyo.People.Postgres.csproj" },
  { id: "Lyo.Hashing", csproj: "Lyo.Net/Security/Hashing/Lyo.Hashing/Lyo.Hashing.csproj" },
  { id: "Lyo.Query", csproj: "Lyo.Net/Data/Query/Lyo.Query/Lyo.Query.csproj" },
  { id: "Lyo.FileStorage", csproj: "Lyo.Net/Data/FileStorage/Lyo.FileStorage/Lyo.FileStorage.csproj" },
  { id: "Lyo.Compression", csproj: "Lyo.Net/Data/Compression/Lyo.Compression/Lyo.Compression.csproj" },
  { id: "Lyo.IO.Temp", csproj: "Lyo.Net/Data/IOTemp/Lyo.IO.Temp/Lyo.IO.Temp.csproj" },
  { id: "Lyo.Csv", csproj: "Lyo.Net/Data/Csv/Lyo.Csv/Lyo.Csv.csproj" },
  { id: "Lyo.Xlsx", csproj: "Lyo.Net/Data/Xlsx/Lyo.Xlsx/Lyo.Xlsx.csproj" },
  { id: "Lyo.MessageQueue", csproj: "Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue/Lyo.MessageQueue.csproj" },
  {
    id: "Lyo.MessageQueue.RabbitMq",
    csproj: "Lyo.Net/Communication/MessageQueue/Lyo.MessageQueue.RabbitMq/Lyo.MessageQueue.RabbitMq.csproj",
  },
  { id: "Lyo.Sms", csproj: "Lyo.Net/Communication/Sms/Lyo.Sms/Lyo.Sms.csproj" },
  { id: "Lyo.Email", csproj: "Lyo.Net/Communication/Email/Lyo.Email/Lyo.Email.csproj" },
  { id: "Lyo.Encryption", csproj: "Lyo.Net/Security/Encryption/Lyo.Encryption/Lyo.Encryption.csproj" },
  { id: "Lyo.Keystore", csproj: "Lyo.Net/Security/Encryption/Lyo.Keystore/Lyo.Keystore.csproj" },
  {
    id: "Lyo.Authentication",
    csproj: "Lyo.Net/Security/Authentication/Lyo.Authentication/Lyo.Authentication.csproj",
  },
  { id: "Lyo.Job.Postgres", csproj: "Lyo.Net/Integration/Job/Lyo.Job.Postgres/Lyo.Job.Postgres.csproj" },
  { id: "Lyo.Api.Client", csproj: "Lyo.Net/Integration/Api/Lyo.Api.Client/Lyo.Api.Client.csproj" },
  {
    id: "Lyo.Reporting.Postgres",
    csproj: "Lyo.Net/Integration/Reporting/Lyo.Reporting.Postgres/Lyo.Reporting.Postgres.csproj",
  },
  { id: "Lyo.Comic.Postgres", csproj: "Lyo.Net/Features/Comic/Lyo.Comic.Postgres/Lyo.Comic.Postgres.csproj" },
  { id: "Lyo.Config.Postgres", csproj: "Lyo.Net/Features/Config/Lyo.Config.Postgres/Lyo.Config.Postgres.csproj" },
];

function run(cmd, args, cwd) {
  const result = spawnSync(cmd, args, {
    cwd,
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });
  if (result.status !== 0) {
    const err = (result.stderr || result.stdout || "").trim();
    throw new Error(`${cmd} ${args.join(" ")} failed:\n${err}`);
  }
  return result.stdout;
}

function findNupkg(dir, packageId) {
  if (!existsSync(dir)) return null;
  const files = readdirSync(dir).filter(
    (f) => f.toLowerCase().startsWith(packageId.toLowerCase()) && f.endsWith(".nupkg")
  );
  if (files.length === 0) return null;
  files.sort();
  return path.join(dir, files[files.length - 1]);
}

function versionFromNupkg(fileName) {
  // Lyo.Query.1.2.3.nupkg → 1.2.3
  const base = path.basename(fileName, ".nupkg");
  const m = base.match(/\.(\d+\.\d+\.\d+(?:[-+][A-Za-z0-9.+-]+)?)$/);
  return m?.[1] ?? "0.0.0";
}

function main() {
  rmSync(packDir, { recursive: true, force: true });
  mkdirSync(packDir, { recursive: true });

  /** @type {{ id: string, name: string, version: string, bytes: number, sha256?: string }[]} */
  const packages = [];

  for (const item of allowlist) {
    const csproj = path.join(repoRoot, item.csproj);
    if (!existsSync(csproj)) {
      console.warn(`skip missing ${item.csproj}`);
      packages.push({ id: item.id, name: item.id, version: "0.0.0", bytes: 0 });
      continue;
    }

    console.log(`pack ${item.id}…`);
    try {
      run(
        "dotnet",
        [
          "pack",
          csproj,
          "-c",
          "Release",
          "-o",
          packDir,
          "--nologo",
          "-p:IncludeSymbols=false",
          "-p:GenerateDocumentationFile=false",
        ],
        repoRoot
      );
      const nupkg = findNupkg(packDir, item.id);
      if (!nupkg) {
        console.warn(`no nupkg for ${item.id}`);
        packages.push({ id: item.id, name: item.id, version: "0.0.0", bytes: 0 });
        continue;
      }
      const bytes = statSync(nupkg).size;
      const version = versionFromNupkg(nupkg);
      const sha256 = createHash("sha256").update(readFileSync(nupkg)).digest("hex").slice(0, 16);
      packages.push({ id: item.id, name: item.id, version, bytes, sha256 });
      console.log(`  ${item.id} ${version} → ${bytes} bytes`);
    } catch (err) {
      console.warn(`pack failed for ${item.id}: ${err instanceof Error ? err.message : err}`);
      packages.push({ id: item.id, name: item.id, version: "0.0.0", bytes: 0 });
    }
  }

  const payload = {
    generatedAt: new Date().toISOString(),
    note: "NuGet package (.nupkg) sizes from curated dotnet pack allowlist.",
    packages,
  };

  mkdirSync(path.dirname(outFile), { recursive: true });
  writeFileSync(outFile, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
  console.log(`wrote ${outFile}`);
}

main();
