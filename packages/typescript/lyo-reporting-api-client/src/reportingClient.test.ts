import {describe, expect, it} from "vitest";
import {createApiClient, createAsyncApiClient, type ApiResponse, type ApiTransport, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncReportingApiClient, createReportingApiClient} from "./reporting/client.js";

function ok<T>(data: T, extra: Partial<ApiResponse<T>> = {}): ApiResponse<T> {
    return {status: 200, ok: true, data, ...extra};
}

describe("createAsyncReportingApiClient", () => {
    it("hits generate, query, resolve, and download routes", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            if (req.responseType === "blob") {
                return ok(undefined, {
                    blob: new Blob(["pdf"], {type: "application/pdf"}),
                    headers: {"content-disposition": 'attachment; filename="out.pdf"', "content-type": "application/pdf"},
                });
            }
            return ok({isSuccess: true, items: [], queryScore: 0});
        };
        const api = createAsyncApiClient({baseUrl: "http://x", transport});
        const reports = createAsyncReportingApiClient(api);

        await reports.generations.generate({reportDefinitionId: "d1", createdBy: "ui"});
        await reports.definitions.queryConcrete({Amount: 10, Options: {TotalCountMode: "Exact", IncludeFilterMode: "Full"}});
        await reports.resolveParameterOptions({optionsJson: "{}", siblingValues: {}});
        const file = await reports.generations.download("g1");

        expect(calls[0]?.url).toBe("http://x/Reporting/Generation/Generate");
        expect(calls[1]?.url).toBe("http://x/Reporting/Definition/QueryConcrete");
        expect(calls[2]?.url).toBe("http://x/Reporting/ResolveParameterOptions");
        expect(calls[3]?.url).toBe("http://x/Reporting/Generation/g1/Download");
        expect(file.fileName).toBe("out.pdf");
    });

    it("patchById sends a single key", async () => {
        const calls: Array<{method: string; url: string; body?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body});
            return ok({});
        };
        const reports = createAsyncReportingApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        await reports.definitions.patch({keys: [["keep"]], properties: {Name: "a"}});
        await reports.definitions.patchById("def-9", {properties: {IsActive: false}});
        expect(JSON.parse(calls[0]!.body!)).toEqual({keys: [["keep"]], properties: {Name: "a"}});
        expect(JSON.parse(calls[1]!.body!)).toEqual({keys: [["def-9"]], properties: {IsActive: false}});
    });
});

describe("createReportingApiClient", () => {
    it("covers definition update, parameter crud, and generation read routes", () => {
        const calls: Array<{method: string; url: string}> = [];
        const transport: ApiTransport = (req) => {
            calls.push({method: req.method, url: req.url});
            return ok({isSuccess: true, items: [], id: "g1"});
        };
        const reports = createReportingApiClient(createApiClient({baseUrl: "http://x", transport}));
        reports.definitions.queryProject({Amount: 1, Select: ["Id"]});
        reports.definitions.update("d1", {name: "n", reportDataJson: "{}"});
        reports.definitions.patchById("d1", {properties: {IsActive: true}});
        reports.definitions.export({format: "Csv", query: {Amount: 1, Select: ["Id"]}});
        reports.definitionParameters.get("p1");
        reports.generations.get("g1");
        reports.generations.queryProject({Amount: 1, Select: ["Id"]});
        reports.generations.rerun("g1");
        reports.generations.download("g1");
        reports.generations.deleteById("g1");

        expect(calls.map((c) => `${c.method} ${c.url}`)).toEqual([
            "POST http://x/Reporting/Definition/QueryProject",
            "POST http://x/Reporting/Definition/Update",
            "PATCH http://x/Reporting/Definition",
            "POST http://x/Reporting/Definition/Export",
            "GET http://x/Reporting/Definition/Parameter/p1",
            "GET http://x/Reporting/Generation/g1",
            "POST http://x/Reporting/Generation/QueryProject",
            "POST http://x/Reporting/Generation/g1/Rerun",
            "GET http://x/Reporting/Generation/g1/Download",
            "DELETE http://x/Reporting/Generation/g1",
        ]);
    });
});
