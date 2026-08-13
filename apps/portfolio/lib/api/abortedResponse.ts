import { NextResponse } from "next/server";
import { CLIENT_CLOSED_REQUEST_STATUS, isClientAborted } from "lyo-api-client";

export function abortedUpstreamResponse(err: unknown, signal?: AbortSignal): NextResponse | null {
  if (!isClientAborted(err, signal))
    return null;

  return new NextResponse(null, { status: CLIENT_CLOSED_REQUEST_STATUS });
}
