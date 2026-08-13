import type { NextConfig } from "next";
import path from "node:path";

const nextConfig: NextConfig = {
  output: "standalone",
  outputFileTracingRoot: path.join(__dirname, "../.."),
  transpilePackages: [
    "lyo-api-client",
    "lyo-comic-api-client",
    "lyo-query",
    "lyo-query-components",
    "lyo-web-components",
  ],
  experimental: {
    externalDir: true,
  },
};

export default nextConfig;
