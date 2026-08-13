import { NextResponse } from "next/server";
import { apiFetch } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";

/** Proxies TestApi readiness for the site status badge. */
export async function GET(request: Request) {
  try {
    const res = await apiFetch("/health", { method: "GET", signal: request.signal });
    const text = await res.text();
    let body: unknown = text;
    try {
      body = JSON.parse(text);
    } catch {
      /* keep text */
    }
    return NextResponse.json(
      {
        ok: res.ok,
        upstreamStatus: res.status,
        body,
      },
      { status: res.ok ? 200 : 502 }
    );
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    const message = err instanceof Error ? err.message : "Upstream unreachable";
    return NextResponse.json({ ok: false, error: message }, { status: 502 });
  }
}
