import { NextResponse } from "next/server";
import { comicApiBaseUrl } from "@/lib/api/serverClient";

export const dynamic = "force-dynamic";

/** Proxies Comic API readiness for compose healthchecks. Anonymous. */
export async function GET() {
  try {
    const res = await fetch(`${comicApiBaseUrl()}/health`, { method: "GET", cache: "no-store" });
    const text = await res.text();
    let body: unknown = text;
    try {
      body = JSON.parse(text);
    } catch {
      /* keep text */
    }
    return NextResponse.json(
      { ok: res.ok, upstreamStatus: res.status, body },
      { status: res.ok ? 200 : 502 }
    );
  } catch (err) {
    const message = err instanceof Error ? err.message : "Upstream unreachable";
    return NextResponse.json({ ok: false, error: message }, { status: 502 });
  }
}
