import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";

/** Streams Comic API file bytes with the session Bearer. */
export async function GET(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  try {
    const res = await apiFetch(`/files/${encodeURIComponent(id)}`, { method: "GET", signal: request.signal });
    if (!res.ok) {
      return NextResponse.json({ error: "File not found." }, { status: res.status });
    }
    const buf = await res.arrayBuffer();
    const contentType = res.headers.get("content-type") ?? "application/octet-stream";
    return new NextResponse(buf, {
      status: 200,
      headers: {
        "Content-Type": contentType,
        "Cache-Control": "private, max-age=0, must-revalidate",
      },
    });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    const message = err instanceof Error ? err.message : "Upstream failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}

export async function DELETE(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  try {
    const res = await apiFetch(`/files/${encodeURIComponent(id)}`, { method: "DELETE", signal: request.signal });
    return new NextResponse(await res.arrayBuffer(), {
      status: res.status,
      headers: { "Content-Type": res.headers.get("content-type") ?? "application/json" },
    });
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    const message = err instanceof Error ? err.message : "Upstream failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
