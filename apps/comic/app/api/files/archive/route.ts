import { NextRequest } from "next/server";
import { streamUpstream } from "@/lib/api/streamUpstream";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const maxDuration = 300;

/** Streams Comic API `POST /files/archive` without buffering the zip. */
export async function POST(request: NextRequest) {
  const contentType = request.headers.get("content-type") ?? "application/json";
  const body = await request.text();
  return streamUpstream("/files/archive", {
    method: "POST",
    headers: { "Content-Type": contentType },
    body,
    signal: request.signal,
  });
}
