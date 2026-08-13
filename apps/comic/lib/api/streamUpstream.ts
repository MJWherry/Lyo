import { NextRequest, NextResponse } from "next/server";
import { CLIENT_CLOSED_REQUEST_STATUS, isClientAborted } from "lyo-api-client";
import { apiFetch } from "@/lib/api/serverClient";

function abortError(): Error {
  const err = new Error("This operation was aborted");
  err.name = "AbortError";
  return err;
}

/**
 * Pipes an upstream Comic API zip. The body is returned immediately so a client abort
 * can cancel the stream and the Comic API fetch (Next.js `request.signal` often stays
 * quiet until this response has started).
 */
export async function streamUpstream(path: string, init?: RequestInit): Promise<NextResponse> {
  if (init?.signal?.aborted)
    return new NextResponse(null, { status: CLIENT_CLOSED_REQUEST_STATUS });

  const abort = new AbortController();
  const onIncomingAbort = () => abort.abort(abortError());
  init?.signal?.addEventListener("abort", onIncomingAbort, { once: true });

  const headers = new Headers();
  headers.set("Content-Type", "application/zip");
  headers.set("Cache-Control", "no-store");
  const disposition = contentDispositionFromPath(path);
  if (disposition)
    headers.set("Content-Disposition", disposition);

  const readable = new ReadableStream<Uint8Array>({
    async start(controller) {
      try {
        const res = await apiFetch(path, { ...init, signal: abort.signal, cache: "no-store" });
        if (abort.signal.aborted)
          return;
        if (!res.ok || !res.body) {
          controller.error(new Error(res.ok ? "Upstream returned an empty body" : `Upstream ${res.status}`));
          return;
        }

        const reader = res.body.getReader();
        while (true) {
          const { done, value } = await reader.read();
          if (done)
            break;
          if (abort.signal.aborted) {
            await reader.cancel();
            return;
          }
          if (value)
            controller.enqueue(value);
        }
        controller.close();
      } catch (err) {
        if (isClientAborted(err, abort.signal))
          return;
        try {
          controller.error(err instanceof Error ? err : new Error("Upstream failed"));
        } catch {
          /* stream already cancelled */
        }
      } finally {
        init?.signal?.removeEventListener("abort", onIncomingAbort);
      }
    },
    cancel() {
      abort.abort(abortError());
    },
  });

  return new NextResponse(readable, { status: 200, headers });
}

export function archiveQuery(request: NextRequest): string {
  const fileName = request.nextUrl.searchParams.get("fileName");
  return fileName ? `?fileName=${encodeURIComponent(fileName)}` : "";
}

function contentDispositionFromPath(path: string): string | undefined {
  try {
    const fileName = new URL(path, "http://bff.local").searchParams.get("fileName");
    if (!fileName)
      return undefined;
    return `attachment; filename="${fileName.replace(/[\r\n"]/g, "")}"`;
  } catch {
    return undefined;
  }
}
