import {mkdtempSync, readFileSync, rmSync} from "node:fs";
import {tmpdir} from "node:os";
import path from "node:path";
import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncAutomationApiClient} from "lyo-automation-api-client";
import {createPlanFile, listPlanFiles, showPlanFile, writePlanFile} from "./plans.js";
import {startPlanPage} from "./pageServer.js";
import {execute, parseArgv, studioDotnetArgs} from "./run.js";

function ok<T>(data: T): ApiResponse<T> {
    return {status: 200, ok: true, data};
}

describe("automation cli", () => {
    it("CreatePlan_WritesOneNamedJsonFile", () => {
        const dir = mkdtempSync(path.join(tmpdir(), "lyo-plans-"));
        try {
            const fileName = createPlanFile(dir, "Checkout");
            expect(fileName).toBe("Checkout.json");
            expect(fileName).toMatch(/^[^/\\]+\.json$/);
            const plan = JSON.parse(readFileSync(path.join(dir, fileName), "utf8")) as {name: string; steps: unknown[]};
            expect(plan).toEqual({name: "Checkout", steps: []});
            expect(listPlanFiles(dir).map((item) => item.fileName)).toEqual(["Checkout.json"]);
        } finally {
            rmSync(dir, {recursive: true, force: true});
        }
    });

    it("ShowPlan_PrintsThatFile", () => {
        const dir = mkdtempSync(path.join(tmpdir(), "lyo-plans-"));
        try {
            createPlanFile(dir, "Checkout");
            const shown = JSON.parse(showPlanFile(dir, "Checkout")) as {name: string};
            expect(shown.name).toBe("Checkout");
        } finally {
            rmSync(dir, {recursive: true, force: true});
        }
    });

    it("WritePlan_LeavesTheFileWhenJsonIsInvalid", () => {
        const dir = mkdtempSync(path.join(tmpdir(), "lyo-plans-"));
        try {
            const fileName = createPlanFile(dir, "Checkout");
            const before = readFileSync(path.join(dir, fileName), "utf8");
            expect(() => writePlanFile(dir, fileName, "{")).toThrow();
            expect(readFileSync(path.join(dir, fileName), "utf8")).toBe(before);
        } finally {
            rmSync(dir, {recursive: true, force: true});
        }
    });

    it("StudioArgs_ForwardsPlan", () => {
        expect(studioDotnetArgs("/repo/Lyo.Web.Automation.Studio.csproj", {plan: "plans/Checkout.json", url: "https://example.com"})).toEqual([
            "run",
            "--project",
            "/repo/Lyo.Web.Automation.Studio.csproj",
            "--",
            "--url",
            "https://example.com",
            "--plan",
            "plans/Checkout.json",
        ]);
    });

    it("Run_PostsThePlanFile", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: typeof req.body === "string" ? req.body : undefined});
            if (req.method === "POST")
                return {status: 202, ok: true, data: {runId: "run-1", status: "queued"}};
            return ok({runId: "run-1", status: "completed"});
        };
        const client = createAsyncAutomationApiClient(createAsyncApiClient({baseUrl: "http://api", transport}));
        const lines: string[] = [];
        await execute(parseArgv(["run", "checkout.json", "--api", "http://api"]), {
            readText: async () => JSON.stringify({name: "Checkout", steps: []}),
            stdout: (line) => lines.push(line),
            client,
        });
        const post = calls.find((call) => call.method === "POST");
        expect(post?.url).toBe("http://api/Automation/Run");
        expect(JSON.parse(post!.body!)).toEqual({plan: {name: "Checkout", steps: []}, headless: true});
        expect(lines.join("\n")).toContain("completed");
    });

    it("PlanPage_CreateThenOpen_ReadsTheFile", async () => {
        const dir = mkdtempSync(path.join(tmpdir(), "lyo-plans-"));
        const page = await startPlanPage({plansDir: dir, staticDir: dir});
        try {
            const created = await fetch(`${page.url}/api/plans`, {
                method: "POST",
                headers: {"Content-Type": "application/json"},
                body: JSON.stringify({name: "Checkout"}),
            });
            expect(created.status).toBe(201);
            const listed = (await (await fetch(`${page.url}/api/plans`)).json()) as {plans: Array<{fileName: string; name: string}>};
            expect(listed.plans).toEqual([{fileName: "Checkout.json", name: "Checkout"}]);
            const opened = (await (await fetch(`${page.url}/api/plans/Checkout.json`)).json()) as {name: string; steps: unknown[]};
            expect(opened).toEqual({name: "Checkout", steps: []});
        } finally {
            await page.close();
            rmSync(dir, {recursive: true, force: true});
        }
    });
});
