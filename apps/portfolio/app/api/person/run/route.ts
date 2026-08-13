import { NextRequest, NextResponse } from "next/server";
import { ApiClientError } from "lyo-api-client";
import { isWhereClause, type QueryBuilderMode } from "lyo-query";
import {
  isProjectedQueryRes,
  isQueryRes,
  type GetByIdReq,
  type ProjectionQueryReq,
  type QueryConcreteReq,
  type QueryReq,
} from "lyo-person-api-client";
import { getPersonApi } from "@/lib/api/serverClient";
import { abortedUpstreamResponse } from "@/lib/api/abortedResponse";

export const dynamic = "force-dynamic";

type RunBody = {
  mode?: QueryBuilderMode;
  request?: unknown;
};

/**
 * BFF dispatcher for Person Concrete / Project / Root Query / Get.
 * Host and routes are fixed server-side — the browser never picks endpoints.
 */
export async function POST(request: NextRequest) {
  let body: RunBody = {};
  try {
    body = (await request.json()) as RunBody;
  } catch {
    return NextResponse.json({ error: "Invalid JSON body." }, { status: 400 });
  }

  const mode = body.mode;
  if (
    mode !== "concrete" &&
    mode !== "project" &&
    mode !== "query" &&
    mode !== "get"
  ) {
    return NextResponse.json(
      { error: "mode must be concrete | project | query | get." },
      { status: 400 }
    );
  }

  const started = performance.now();
  try {
    const personApi = getPersonApi(request.signal);

    if (mode === "get") {
      const getReq = normalizeGet(body.request);
      if (!getReq) {
        return NextResponse.json(
          { error: "Get mode requires request.id (non-empty string)." },
          { status: 400 }
        );
      }
      const response = await personApi.getPerson(getReq.id, getReq.Include);
      return NextResponse.json({
        mode,
        isSuccess: true,
        item: response.data ?? null,
        items: response.data ? [response.data] : [],
        elapsedMs: performance.now() - started,
      });
    }

    if (mode === "concrete") {
      const req = normalizeConcrete(body.request);
      if (!req.ok) return req.response;
      const response = await personApi.queryPerson(req.value);
      if (!isQueryRes(response.data)) {
        return NextResponse.json(
          { error: "Unexpected response shape from Person QueryConcrete." },
          { status: 502 }
        );
      }
      return listPayload(mode, response.data, req.value, started);
    }

    if (mode === "project") {
      const req = normalizeProject(body.request);
      if (!req.ok) return req.response;
      const response = await personApi.queryPersonProjected(req.value);
      if (!isProjectedQueryRes(response.data)) {
        return NextResponse.json(
          { error: "Unexpected response shape from Person QueryProject." },
          { status: 502 }
        );
      }
      return listPayload(mode, response.data, req.value, started);
    }

    const req = normalizeRoot(body.request);
    if (!req.ok) return req.response;
    const response = await personApi.queryRoot(req.value);
    if (!isProjectedQueryRes(response.data)) {
      return NextResponse.json(
        { error: "Unexpected response shape from root Query." },
        { status: 502 }
      );
    }
    return listPayload(mode, response.data, req.value, started);
  } catch (err) {
    return errorResponse(err, started, request.signal);
  }
}

function listPayload(
  mode: QueryBuilderMode,
  data: {
    isSuccess: boolean;
    total?: number | null;
    hasMore?: boolean | null;
    items?: unknown[] | null;
  },
  queryReq: { Start?: number; Amount?: number },
  started: number
) {
  return NextResponse.json({
    mode,
    isSuccess: data.isSuccess,
    total: data.total ?? null,
    hasMore: data.hasMore ?? null,
    items: data.items ?? [],
    start: queryReq.Start ?? 0,
    amount: queryReq.Amount ?? 10,
    elapsedMs: performance.now() - started,
  });
}

function normalizeGet(raw: unknown): GetByIdReq | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const id = typeof r.id === "string" ? r.id.trim() : "";
  if (!id) return null;
  const Include = Array.isArray(r.Include)
    ? r.Include.filter((x): x is string => typeof x === "string")
    : [];
  return { id, Include };
}

function clampPaging(req: { Start?: number; Amount?: number }) {
  req.Start = Math.max(0, Number(req.Start ?? 0) || 0);
  req.Amount = Math.min(50, Math.max(1, Number(req.Amount ?? 10) || 10));
}

function validateWhere(
  whereClause: unknown
): { ok: true; value: unknown } | { ok: false; response: NextResponse } {
  if (whereClause === undefined || whereClause === null) {
    return { ok: true, value: null };
  }
  if (!isWhereClause(whereClause)) {
    return {
      ok: false,
      response: NextResponse.json(
        { error: "Invalid whereClause shape." },
        { status: 400 }
      ),
    };
  }
  return { ok: true, value: whereClause };
}

type NormOk<T> = { ok: true; value: T };
type NormErr = { ok: false; response: NextResponse };

function normalizeConcrete(raw: unknown): NormOk<QueryConcreteReq> | NormErr {
  if (!raw || typeof raw !== "object") {
    return {
      ok: false,
      response: NextResponse.json({ error: "Missing request body." }, { status: 400 }),
    };
  }
  const req = { ...(raw as QueryConcreteReq) };
  clampPaging(req);
  req.Options ??= {
    TotalCountMode: "Exact",
    IncludeFilterMode: "Full",
  };
  const where = validateWhere(req.whereClause);
  if (!where.ok) return where;
  req.whereClause = where.value as QueryConcreteReq["whereClause"];
  return { ok: true, value: req };
}

function normalizeProject(raw: unknown): NormOk<ProjectionQueryReq> | NormErr {
  if (!raw || typeof raw !== "object") {
    return {
      ok: false,
      response: NextResponse.json({ error: "Missing request body." }, { status: 400 }),
    };
  }
  const req = { ...(raw as ProjectionQueryReq) };
  clampPaging(req);
  req.Options ??= {
    TotalCountMode: "Exact",
    IncludeFilterMode: "Full",
  };
  if (!Array.isArray(req.Select) || req.Select.length === 0) {
    return {
      ok: false,
      response: NextResponse.json(
        { error: "Projection requires a non-empty Select array." },
        { status: 400 }
      ),
    };
  }
  const where = validateWhere(req.whereClause);
  if (!where.ok) return where;
  req.whereClause = where.value as ProjectionQueryReq["whereClause"];
  return { ok: true, value: req };
}

function normalizeRoot(raw: unknown): NormOk<QueryReq> | NormErr {
  if (!raw || typeof raw !== "object") {
    return {
      ok: false,
      response: NextResponse.json({ error: "Missing request body." }, { status: 400 }),
    };
  }
  const req = { ...(raw as QueryReq) };
  clampPaging(req);
  req.Options ??= {
    TotalCountMode: "Exact",
    IncludeFilterMode: "Full",
  };
  if (!req.From || typeof req.From.EntityType !== "string" || !req.From.EntityType.trim()) {
    return {
      ok: false,
      response: NextResponse.json(
        { error: "Root Query requires From.EntityType." },
        { status: 400 }
      ),
    };
  }
  if (!req.From.Alias?.trim()) req.From.Alias = "p";
  if (!Array.isArray(req.Select) || req.Select.length === 0) {
    return {
      ok: false,
      response: NextResponse.json(
        { error: "Root Query requires a non-empty Select array." },
        { status: 400 }
      ),
    };
  }
  const where = validateWhere(req.whereClause);
  if (!where.ok) return where;
  req.whereClause = where.value as QueryReq["whereClause"];
  return { ok: true, value: req };
}

function errorResponse(err: unknown, started: number, signal?: AbortSignal) {
  const aborted = abortedUpstreamResponse(err, signal);
  if (aborted) return aborted;
  if (err instanceof ApiClientError) {
    const status = err.status && err.status >= 400 && err.status < 600 ? err.status : 502;
    return NextResponse.json(
      {
        error: err.message,
        // Upstream ProblemDetails (or raw body) for the demo Results panel.
        details: err.details ?? { status, title: err.message },
        status,
        elapsedMs: performance.now() - started,
      },
      { status }
    );
  }
  if (err instanceof Error && err.message.includes("LYO_API_BASE_URL")) {
    return NextResponse.json({ error: err.message }, { status: 503 });
  }
  const message = err instanceof Error ? err.message : "Request failed";
  return NextResponse.json(
    { error: message, elapsedMs: performance.now() - started },
    { status: 502 }
  );
}
