import type { NextConfig } from "next";
import path from "node:path";

const repoRoot = path.join(__dirname, "../..");

const nextConfig: NextConfig = {
  output: "standalone",
  outputFileTracingRoot: repoRoot,
  turbopack: {
    root: repoRoot,
  },
  transpilePackages: [
    "lyo-api-client",
    "lyo-comic-api-client",
    "lyo-query",
    "lyo-query-components",
    "lyo-web-components",
  ],
};

export default nextConfig;
