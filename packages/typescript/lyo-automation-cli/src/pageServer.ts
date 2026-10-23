import {createReadStream, existsSync} from "node:fs";
import {createServer, type IncomingMessage, type ServerResponse} from "node:http";
import path from "node:path";
import {createPlanFile, listPlanFiles, showPlanFile, writePlanFile} from "./plans.js";

export type PlanPage = {
    url: string;
    close: () => Promise<void>;
};

function send(response: ServerResponse, status: number, body: string, type: string): void {
    response.writeHead(status, {"Content-Type": type});
    response.end(body);
}

async function readBody(request: IncomingMessage): Promise<string> {
    const chunks: Buffer[] = [];
    for await (const chunk of request)
        chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
    return Buffer.concat(chunks).toString("utf8");
}

function contentType(file: string): string {
    if (file.endsWith(".html"))
        return "text/html; charset=utf-8";
    if (file.endsWith(".css"))
        return "text/css; charset=utf-8";
    if (file.endsWith(".js"))
        return "text/javascript; charset=utf-8";
    return "application/octet-stream";
}

/** Serves the plan library page and the directory API. The React page is the only caller. */
export function startPlanPage(options: {plansDir: string; staticDir?: string}): Promise<PlanPage> {
    const staticDir = options.staticDir ?? path.join(path.dirname(new URL(import.meta.url).pathname), "page");
    const server = createServer(async (request, response) => {
        const url = new URL(request.url ?? "/", "http://127.0.0.1");
        try {
            if (url.pathname === "/api/plans" && request.method === "GET") {
                send(response, 200, JSON.stringify({plans: listPlanFiles(options.plansDir)}), "application/json");
                return;
            }
            if (url.pathname === "/api/plans" && request.method === "POST") {
                const body = JSON.parse(await readBody(request)) as {name?: string};
                if (!body.name)
                    throw new Error("A plan name is required.");
                const fileName = createPlanFile(options.plansDir, body.name);
                send(response, 201, JSON.stringify({fileName}), "application/json");
                return;
            }
            const fileMatch = url.pathname.match(/^\/api\/plans\/([^/]+)$/);
            if (fileMatch && request.method === "GET") {
                send(response, 200, showPlanFile(options.plansDir, decodeURIComponent(fileMatch[1] ?? "")), "application/json");
                return;
            }
            if (fileMatch && request.method === "PUT") {
                writePlanFile(options.plansDir, decodeURIComponent(fileMatch[1] ?? ""), await readBody(request));
                send(response, 204, "", "application/json");
                return;
            }
            const root = path.resolve(staticDir);
            const relative = url.pathname === "/" ? "index.html" : url.pathname.replace(/^\/+/, "");
            const asset = path.resolve(root, relative);
            const inside = asset === root || asset.startsWith(`${root}${path.sep}`);
            if (!inside || !existsSync(asset) || asset === root) {
                send(response, 404, "Not found", "text/plain; charset=utf-8");
                return;
            }
            response.writeHead(200, {"Content-Type": contentType(asset)});
            createReadStream(asset).pipe(response);
        } catch (error) {
            send(response, 400, error instanceof Error ? error.message : "Bad request", "text/plain; charset=utf-8");
        }
    });

    return new Promise((resolve, reject) => {
        server.once("error", reject);
        server.listen(0, "127.0.0.1", () => {
            const address = server.address();
            const port = address && typeof address === "object" ? address.port : 0;
            resolve({
                url: `http://127.0.0.1:${port}`,
                close: () => new Promise((done, fail) => server.close((error) => (error ? fail(error) : done()))),
            });
        });
    });
}
