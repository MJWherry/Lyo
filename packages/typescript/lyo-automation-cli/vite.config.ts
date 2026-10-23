import path from "node:path";
import {fileURLToPath} from "node:url";
import {defineConfig} from "vite";
import react from "@vitejs/plugin-react";

const packageRoot = path.dirname(fileURLToPath(import.meta.url));
const local = (...parts: string[]) => path.resolve(packageRoot, "node_modules", ...parts);

export default defineConfig({
    root: "src/page",
    resolve: {
        alias: {
            react: local("react"),
            "react-dom": local("react-dom"),
            "@mui/material": local("@mui/material"),
            "@mui/system": local("@mui/system"),
            "@emotion/react": local("@emotion/react"),
            "@emotion/styled": local("@emotion/styled"),
            "lyo-web-components": path.resolve(packageRoot, "src/page/web.ts"),
        },
    },
    build: {
        outDir: "../../dist/page",
        emptyOutDir: true,
    },
});
