import { NextRequest, NextResponse } from "next/server";
import { ApiClientError } from "lyo-api-client";
import { isWhereClause } from "lyo-query";
import {
  baselineQuery,
  buildOptions,
  isQueryRes,
  type QueryConcreteReq,
  type QueryTotalCountMode,
} from "lyo-person-api-client";
import { getPersonApi } from "@/lib/api/serverClient";

export const dynamic = "force-dynamic";

/**
 * BFF: Person QueryConcrete against the internal TestApi.
 * GET — baseline page (smoke). POST — optional whereClause from the demo builder.
 */
export async function GET(request: NextRequest) {
  const { searchParams } = request.nextUrl;
  const start = Math.max(0, Number(searchParams.get("start") ?? "0") || 0);
  const amount = Math.min(50, Math.max(1, Number(searchParams.get("amount") ?? "10") || 10));
  return runQuery(baselineQuery({ start, amount }));
}

export async function POST(request: NextRequest) {
  let body: {
    start?: number;
    amount?: number;
    totalCountMode?: QueryTotalCountMode;
    whereClause?: unknown;
  } = {};
  try {
    body = (await request.json()) as typeof body;
  } catch {
    /* empty */
  }

  const start = Math.max(0, Number(body.start ?? 0) || 0);
  const amount = Math.min(50, Math.max(1, Number(body.amount ?? 10) || 10));
  const totalCountMode: QueryTotalCountMode = body.totalCountMode ?? "Exact";

  const req: QueryConcreteReq = {
    Options: buildOptions({ TotalCountMode: totalCountMode }),
    Start: start,
    Amount: amount,
    Include: [],
    SortBy: [],
  };

  if (body.whereClause !== undefined && body.whereClause !== null) {
    if (!isWhereClause(body.whereClause)) {
      return NextResponse.json({ error: "Invalid whereClause shape." }, { status: 400 });
    }
    req.whereClause = body.whereClause;
  }

  return runQuery(req);
}

async function runQuery(queryReq: QueryConcreteReq) {
  const started = performance.now();
  try {
    const personApi = getPersonApi();
    const response = await personApi.queryPerson(queryReq);
    const elapsedMs = performance.now() - started;

    if (!isQueryRes(response.data)) {
      return NextResponse.json(
        { error: "Unexpected response shape from Person QueryConcrete." },
        { status: 502 }
      );
    }

    return NextResponse.json({
      isSuccess: response.data.isSuccess,
      total: response.data.total ?? null,
      hasMore: response.data.hasMore ?? null,
      items: response.data.items ?? [],
      start: queryReq.Start ?? 0,
      amount: queryReq.Amount ?? 10,
      elapsedMs,
    });
  } catch (err) {
    if (err instanceof ApiClientError) {
      return NextResponse.json(
        {
          error: err.message,
          details: err.details,
          elapsedMs: performance.now() - started,
        },
        { status: err.status && err.status >= 400 && err.status < 600 ? err.status : 502 }
      );
    }
    if (err instanceof Error && err.message.includes("LYO_API_BASE_URL")) {
      return NextResponse.json({ error: err.message }, { status: 503 });
    }
    const message = err instanceof Error ? err.message : "Query failed";
    return NextResponse.json(
      { error: message, elapsedMs: performance.now() - started },
      { status: 502 }
    );
  }
}
