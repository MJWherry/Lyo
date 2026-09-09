import {describe, expect, it} from "vitest";
import {createAsyncApiClient, type ApiResponse, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncReportingApiClient} from "./reporting/client.js";

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
});
