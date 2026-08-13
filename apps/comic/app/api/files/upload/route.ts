import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

/** Multipart proxy to Comic API `POST /files/upload`. Do not JSON-stringify. */
export async function POST(request: NextRequest) {
  try {
    const form = await request.formData();
    const qs = request.nextUrl.search;
    const res = await apiFetch(`/files/upload${qs}`, {
      method: "POST",
      body: form,
      signal: request.signal,
    });
    const text = await res.text();
    return new NextResponse(text, {
      status: res.status,
      headers: { "Content-Type": res.headers.get("content-type") ?? "application/json" },
    });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    const message = err instanceof Error ? err.message : "Upload failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
