import { NextRequest, NextResponse } from "next/server";
import { resolveReport } from "@/lib/benchmarks/loadReport";
import { getRegistryEntry } from "@/lib/benchmarks/registry";

export const dynamic = "force-dynamic";

/** Serves latest or history snapshot JSON for the portfolio benchmark viewer. */
export async function GET(
  request: NextRequest,
  context: { params: Promise<{ name: string }> }
) {
  const { name } = await context.params;
  if (!getRegistryEntry(name)) {
    return NextResponse.json({ error: "Unknown suite." }, { status: 404 });
  }

  const snapshot = request.nextUrl.searchParams.get("snapshot");
  const report = resolveReport(name, snapshot);
  if (!report) {
    return NextResponse.json({ error: "Report not found." }, { status: 404 });
  }

  return NextResponse.json(report);
}
