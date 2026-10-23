import type {AsyncApiClient} from "lyo-api-client";
import {
    automationRunCancelPath,
    automationRunLogPath,
    automationRunPath,
    automationRunSnapshotPath,
    automationRunSnapshotsPath,
    automationRunStepScreenshotPath,
    automationPlanByIdPath,
    automationPlanPath,
    AutomationRoutes,
} from "./routes.js";
import {isSettled, type AutomationRunAccepted, type AutomationRunReq, type AutomationRunRes} from "./models.js";

export type AsyncAutomationApiClient = {
    start(request: AutomationRunReq): Promise<AutomationRunAccepted>;
    get(runId: string): Promise<AutomationRunRes>;
    cancel(runId: string): Promise<void>;
    log(runId: string): Promise<string>;
    snapshots(runId: string): Promise<string[]>;
    snapshot(runId: string, name: string): Promise<Blob | undefined>;
    stepScreenshot(runId: string, index: number): Promise<Blob | undefined>;
    createPlan(name: string): Promise<unknown>;
    getPlan(name: string): Promise<unknown>;
    getPlanById(id: string): Promise<unknown>;
    savePlan(name: string, plan: unknown): Promise<unknown>;
    /** Polls `get` until the run is completed, failed, or cancelled. */
    waitUntilSettled(runId: string, pauseMs?: number): Promise<AutomationRunRes>;
};

function fail(status: number, what: string): never {
    throw new Error(`${what} failed: ${status}`);
}

/** Typed calls on an {@link AsyncApiClient}. The browser stays on the API host. */
export function createAsyncAutomationApiClient(api: AsyncApiClient): AsyncAutomationApiClient {
    return {
        async start(request) {
            const response = await api.request<AutomationRunAccepted, AutomationRunReq>({
                method: "POST",
                path: AutomationRoutes.runs,
                body: request,
            });
            if (!response.ok || !response.data)
                fail(response.status, "Start");
            return response.data;
        },
        async get(runId) {
            const response = await api.request<AutomationRunRes>({method: "GET", path: automationRunPath(runId)});
            if (!response.ok || !response.data)
                fail(response.status, "Get");
            return response.data;
        },
        async cancel(runId) {
            const response = await api.request({method: "POST", path: automationRunCancelPath(runId)});
            if (!response.ok)
                fail(response.status, "Cancel");
        },
        async log(runId) {
            const response = await api.request({method: "GET", path: automationRunLogPath(runId)});
            if (!response.ok)
                fail(response.status, "Log");
            return response.rawBody ?? "";
        },
        async snapshots(runId) {
            const response = await api.request<string[]>({method: "GET", path: automationRunSnapshotsPath(runId)});
            if (!response.ok || !response.data)
                fail(response.status, "Snapshots");
            return response.data;
        },
        async snapshot(runId, name) {
            const response = await api.request({
                method: "GET",
                path: automationRunSnapshotPath(runId, name),
                responseType: "blob",
            });
            if (!response.ok)
                fail(response.status, "Snapshot");
            return response.blob;
        },
        async stepScreenshot(runId, index) {
            const response = await api.request({
                method: "GET",
                path: automationRunStepScreenshotPath(runId, index),
                responseType: "blob",
            });
            if (!response.ok)
                fail(response.status, "Step screenshot");
            return response.blob;
        },
        async createPlan(name) {
            const response = await api.request({method: "POST", path: AutomationRoutes.plans, body: {name}});
            if (!response.ok || !response.data)
                fail(response.status, "Create plan");
            return response.data;
        },
        async getPlan(name) {
            const response = await api.request({method: "GET", path: automationPlanPath(name)});
            if (!response.ok || !response.data)
                fail(response.status, "Get plan");
            return response.data;
        },
        async getPlanById(id) {
            const response = await api.request({method: "GET", path: automationPlanByIdPath(id)});
            if (!response.ok || !response.data)
                fail(response.status, "Get plan");
            return response.data;
        },
        async savePlan(name, plan) {
            const response = await api.request({method: "PUT", path: automationPlanPath(name), body: plan});
            if (!response.ok || !response.data)
                fail(response.status, "Save plan");
            return response.data;
        },
        async waitUntilSettled(runId, pauseMs = 0) {
            for (let attempt = 0; attempt < 120; attempt++) {
                const run = await this.get(runId);
                if (isSettled(run.status))
                    return run;
                if (pauseMs > 0)
                    await new Promise((resolve) => setTimeout(resolve, pauseMs));
            }
            throw new Error("The run did not finish.");
        },
    };
}
