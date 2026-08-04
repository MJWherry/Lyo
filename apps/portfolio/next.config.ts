import type { NextConfig } from "next";
import path from "node:path";

const nextConfig: NextConfig = {
  output: "standalone",
  // Allow importing benchmark JSON from the monorepo docs folder.
  outputFileTracingRoot: path.join(__dirname, "../.."),
  transpilePackages: [
    "lyo-api-client",
    "lyo-person-api-client",
    "lyo-query",
    "lyo-query-components",
  ],
  experimental: {
    externalDir: true,
  },
};

export default nextConfig;
