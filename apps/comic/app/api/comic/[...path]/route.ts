import { NextRequest, NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/serverClient";

export const dynamic = "force-dynamic";

const ALLOWED = new Set(["GET", "POST", "PUT", "PATCH", "DELETE"]);

/** JSON BFF catch-all for `/api/comic/*` → Comic API with session Bearer. */
export async function GET(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  return proxy(request, ctx);
}

export async function POST(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  return proxy(request, ctx);
}

export async function PUT(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  return proxy(request, ctx);
}

export async function PATCH(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  return proxy(request, ctx);
}

export async function DELETE(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  return proxy(request, ctx);
}

async function proxy(request: NextRequest, ctx: { params: Promise<{ path: string[] }> }) {
  if (!ALLOWED.has(request.method)) {
    return NextResponse.json({ error: "Method not allowed." }, { status: 405 });
  }
  const { path } = await ctx.params;
  const suffix = (path ?? []).map(encodeURIComponent).join("/");
  const search = request.nextUrl.search;
  const upstreamPath = `/api/comic/${suffix}${search}`;

  const headers: Record<string, string> = {};
  const contentType = request.headers.get("content-type");
  if (contentType) headers["Content-Type"] = contentType;

  let body: string | undefined;
  if (request.method !== "GET" && request.method !== "HEAD") {
    body = await request.text();
  }

  try {
    const res = await apiFetch(upstreamPath, {
      method: request.method,
      headers,
      body,
    });
    const outHeaders = new Headers();
    const ct = res.headers.get("content-type");
    if (ct) outHeaders.set("content-type", ct);
    // 204/205/304 must not include a body — NextResponse rejects an empty buffer and surfaces 500 after the upstream call already succeeded.
    if (res.status === 204 || res.status === 205 || res.status === 304) {
      return new NextResponse(null, { status: res.status, headers: outHeaders });
    }
    const buf = await res.arrayBuffer();
    return new NextResponse(buf, { status: res.status, headers: outHeaders });
  } catch (err) {
    const message = err instanceof Error ? err.message : "Upstream failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
