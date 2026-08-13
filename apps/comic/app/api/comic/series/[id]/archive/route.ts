import { NextRequest } from "next/server";
import { archiveQuery, streamUpstream } from "@/lib/api/streamUpstream";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";
export const maxDuration = 300;

/** Streams Comic API `GET /api/comic/series/{id}/archive`. */
export async function GET(request: NextRequest, ctx: { params: Promise<{ id: string }> }) {
  const { id } = await ctx.params;
  return streamUpstream(`/api/comic/series/${encodeURIComponent(id)}/archive${archiveQuery(request)}`, {
    method: "GET",
    signal: request.signal,
  });
}
