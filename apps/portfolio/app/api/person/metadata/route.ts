import { NextResponse } from "next/server";
import { ApiClientError } from "lyo-api-client";
import { getApi } from "@/lib/api/serverClient";

export const dynamic = "force-dynamic";

/** BFF for GET /person/Metadata via base client `getMetadata("person")`. */
export async function GET() {
  try {
    const api = getApi();
    const response = await api.getMetadata("person");
    return NextResponse.json(response.data ?? null);
  } catch (err) {
    if (err instanceof ApiClientError) {
      const status = err.status && err.status >= 400 && err.status < 600 ? err.status : 502;
      return NextResponse.json(
        {
          error: err.message,
          details: err.details ?? { status, title: err.message },
        },
        { status }
      );
    }
    if (err instanceof Error && err.message.includes("LYO_API_BASE_URL")) {
      return NextResponse.json({ error: err.message }, { status: 503 });
    }
    const message = err instanceof Error ? err.message : "Request failed";
    return NextResponse.json({ error: message }, { status: 502 });
  }
}
