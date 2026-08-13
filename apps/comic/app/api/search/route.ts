import { NextRequest, NextResponse } from "next/server";
import { isWhereClause } from "lyo-query";
import { isProjectedQueryRes } from "lyo-api-client";
import { normalizeComicCardRows } from "lyo-comic-api-client";
import { getComicApi } from "@/lib/api/serverClient";
import { isUnauthorized } from "@/lib/auth/unauthorized";
import { buildProjectionQuery, normalizeScope, type SearchBody } from "@/lib/search/buildQuery";

export const dynamic = "force-dynamic";

/**
 * App-owned QueryProject BFF. Browser sends simple fields + optional whereClause only.
 * Select, paging, sort, and table/endpoint are fixed server-side.
 */
export async function POST(request: NextRequest) {
  let body: SearchBody = {};
  try {
    body = (await request.json()) as SearchBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const scope = normalizeScope(body.scope);
  const start = Math.max(0, Number(body.start ?? 0) || 0);
  const amount = Math.min(48, Math.max(1, Number(body.amount ?? 24) || 24));

  if (body.whereClause != null && !isWhereClause(body.whereClause)) {
    return NextResponse.json({ error: "Invalid whereClause shape." }, { status: 400 });
  }

  const comic = await getComicApi();
  const tags = body.simple?.tags?.filter((t) => typeof t === "string" && t.trim()) ?? [];
  let keys: unknown[][] | undefined;

  if (tags.length > 0) {
    const tagRes = await comic.searchSeries({ tags, limit: 500 });
    const ids = (tagRes.data ?? []).map((s) => s.id).filter(Boolean);
    if (ids.length === 0) {
      return NextResponse.json({
        isSuccess: true,
        items: [],
        total: 0,
        hasMore: false,
        start,
        amount,
        scope,
      });
    }
    if (scope === "series") {
      keys = ids.map((id) => [id]);
    } else {
      // Volume/chapter: constrain via series id In after resolving tags.
      const extra = {
        $type: "condition" as const,
        Field: "SeriesId",
        Comparison: "In" as const,
        Value: ids,
      };
      body.whereClause = body.whereClause
        ? { $type: "group", Operator: "And", Children: [body.whereClause, extra] }
        : extra;
    }
  }

  const req = buildProjectionQuery(scope, body.simple, body.whereClause ?? null, start, amount, keys);

  try {
    const response = await comic.queryProjected(scope, req);

    if (!isProjectedQueryRes(response.data)) {
      return NextResponse.json({ error: "Unexpected QueryProject response." }, { status: 502 });
    }

    return NextResponse.json({
      isSuccess: response.data.isSuccess,
      items: normalizeComicCardRows(response.data.items),
      total: response.data.total ?? null,
      hasMore: response.data.hasMore ?? null,
      start,
      amount,
      scope,
      error: response.data.error ?? null,
    });
  } catch (err) {
    if (isUnauthorized(err)) {
      return NextResponse.json({ error: "Sign in required." }, { status: 401 });
    }
    const message = err instanceof Error ? err.message : "Search failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
