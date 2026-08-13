import { ApiClientError } from "lyo-api-client";
import { headers } from "next/headers";
import { redirect } from "next/navigation";

/** Thrown from the server API client when a Route Handler should return 401 JSON. */
export class UnauthorizedError extends Error {
  readonly status = 401 as const;

  constructor(message = "Sign in required.") {
    super(message);
    this.name = "UnauthorizedError";
  }
}

export function isUnauthorized(err: unknown): boolean {
  if (err instanceof UnauthorizedError) return true;
  return err instanceof ApiClientError && err.status === 401;
}

/**
 * Page RSC: clear the dead cookie via `/auth/expired` then land on login.
 * `/api/*` Route Handlers: throw {@link UnauthorizedError} (return JSON 401).
 */
export async function rejectUnauthorized(): Promise<never> {
  const path = (await headers()).get("x-lyo-pathname") ?? "/";
  if (path.startsWith("/api/")) throw new UnauthorizedError();
  const safe = path.startsWith("/") && !path.startsWith("//") ? path : "/";
  redirect(`/auth/expired?return=${encodeURIComponent(safe)}`);
}
