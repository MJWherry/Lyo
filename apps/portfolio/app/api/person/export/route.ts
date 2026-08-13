import { NextRequest, NextResponse } from "next/server";
import { ApiClientError, type ExportRequest } from "lyo-api-client";
import { getApi } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";

/** BFF: POST ExportRequest → Person /Export (CSV / XLSX / JSON). */
export async function POST(request: NextRequest) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }
  if (!body || typeof body !== "object") {
    return NextResponse.json({ error: "Missing request body." }, { status: 400 });
  }

  try {
    const api = getApi(request.signal);
    const result = await api.export("person", body as ExportRequest);
    const headers = new Headers();
    headers.set("Content-Type", result.contentType || "application/octet-stream");
    headers.set(
      "Content-Disposition",
      `attachment; filename="${result.fileName ?? "person-export.bin"}"`
    );
    return new NextResponse(result.blob, { status: 200, headers });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    if (err instanceof ApiClientError) {
      const status = err.status && err.status >= 400 && err.status < 600 ? err.status : 502;
      return NextResponse.json({ error: err.message, details: err.details }, { status });
    }
    if (err instanceof Error && err.message.includes("LYO_API_BASE_URL")) {
      return NextResponse.json({ error: err.message }, { status: 503 });
    }
    const message = err instanceof Error ? err.message : "Export failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
