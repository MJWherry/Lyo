import { NextRequest, NextResponse } from "next/server";
import { ApiClientError } from "lyo-api-client";
import { isProjectedQueryRes, isQueryRes } from "lyo-person-api-client";
import { isWhereClause, type ProjectionQueryReq, type QueryConcreteReq, type QueryReq } from "lyo-query";
import { getPersonApi } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";

type GridMode = "project" | "concrete" | "query";

function parseMode(value: string | null): GridMode {
  if (value === "concrete" || value === "query" || value === "project") return value;
  return "project";
}

function clampPaging(req: { Start?: number; Amount?: number }) {
  req.Start = Math.max(0, Number(req.Start ?? 0) || 0);
  req.Amount = Math.min(100, Math.max(1, Number(req.Amount ?? 25) || 25));
}

function failWhere(whereClause: unknown): NextResponse | null {
  if (whereClause === undefined || whereClause === null) return null;
  if (!isWhereClause(whereClause)) {
    return NextResponse.json({ error: "Invalid whereClause shape." }, { status: 400 });
  }
  return null;
}

/** BFF for the DataGrid demo: `?mode=project|concrete|query`. */
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

  const mode = parseMode(request.nextUrl.searchParams.get("mode"));

  try {
    const personApi = getPersonApi(request.signal);

    if (mode === "concrete") {
      const req = body as QueryConcreteReq;
      clampPaging(req);
      req.Options ??= { TotalCountMode: "Exact", IncludeFilterMode: "Full" };
      const whereErr = failWhere(req.whereClause);
      if (whereErr) return whereErr;
      const response = await personApi.queryPerson(req);
      if (!isQueryRes(response.data)) {
        return NextResponse.json({ error: "Unexpected response shape from Person QueryConcrete." }, { status: 502 });
      }
      return NextResponse.json(response.data);
    }

    if (mode === "query") {
      const req = body as QueryReq;
      clampPaging(req);
      req.Options ??= { TotalCountMode: "Exact", IncludeFilterMode: "Full" };
      if (!req.From?.EntityType?.trim()) {
        return NextResponse.json({ error: "Root Query requires From.EntityType." }, { status: 400 });
      }
      if (!req.From.Alias?.trim()) req.From.Alias = "p";
      if (!Array.isArray(req.Select) || req.Select.length === 0) {
        return NextResponse.json({ error: "Root Query requires a non-empty Select array." }, { status: 400 });
      }
      const whereErr = failWhere(req.whereClause);
      if (whereErr) return whereErr;
      const response = await personApi.queryRoot(req);
      if (!isProjectedQueryRes(response.data)) {
        return NextResponse.json({ error: "Unexpected response shape from root Query." }, { status: 502 });
      }
      return NextResponse.json(response.data);
    }

    const req = body as ProjectionQueryReq;
    clampPaging(req);
    req.Options ??= { TotalCountMode: "Exact", IncludeFilterMode: "Full" };
    if (!Array.isArray(req.Select) || req.Select.length === 0) {
      return NextResponse.json({ error: "Projection requires a non-empty Select array." }, { status: 400 });
    }
    const whereErr = failWhere(req.whereClause);
    if (whereErr) return whereErr;
    const response = await personApi.queryPersonProjected(req);
    if (!isProjectedQueryRes(response.data)) {
      return NextResponse.json({ error: "Unexpected response shape from Person QueryProject." }, { status: 502 });
    }
    return NextResponse.json(response.data);
  } catch (err) {
    const aborted = abortedUpstreamResponse(err, request.signal);
    if (aborted) return aborted;
    if (err instanceof ApiClientError) {
      const status = err.status && err.status >= 400 && err.status < 600 ? err.status : 502;
      return NextResponse.json(
        { error: err.message, details: err.details ?? { status, title: err.message }, isSuccess: false, items: [] },
        { status }
      );
    }
    if (err instanceof Error && err.message.includes("LYO_API_BASE_URL")) {
      return NextResponse.json({ error: err.message, isSuccess: false, items: [] }, { status: 503 });
    }
    const message = err instanceof Error ? err.message : "Request failed";
    return NextResponse.json({ error: message, isSuccess: false, items: [] }, { status: 502 });
  }
}
