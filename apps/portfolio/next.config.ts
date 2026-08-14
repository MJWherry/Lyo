import type { NextConfig } from "next";
import path from "node:path";

const repoRoot = path.join(__dirname, "../..");

const nextConfig: NextConfig = {
  output: "standalone",
  // Allow importing benchmark JSON from the monorepo docs folder.
  outputFileTracingRoot: repoRoot,
  turbopack: {
    root: repoRoot,
  },
  transpilePackages: [
    "lyo-api-client",
    "lyo-person-api-client",
    "lyo-query",
    "lyo-query-components",
    "lyo-web-components",
  ],
};

export default nextConfig;
