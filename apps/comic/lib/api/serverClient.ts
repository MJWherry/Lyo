import "server-only";

import { createAsyncApiClient, type AsyncApiClient, type AsyncApiTransport } from "lyo-api-client";
import { createAsyncComicApiClient } from "lyo-comic-api-client";
import { cookies } from "next/headers";
import { fetchTransport } from "./fetchTransport";
import { rejectUnauthorized } from "@/lib/auth/unauthorized";
import {
  readSession,
  sealSession,
  SESSION_COOKIE,
  sessionCookieOptions,
  type SessionTokens,
  unsealSession,
} from "@/lib/auth/session";

function requireApiBaseUrl(): string {
  const baseUrl = process.env.LYO_COMIC_API_BASE_URL?.trim();
  if (!baseUrl) {
    throw new Error(
      "LYO_COMIC_API_BASE_URL is not set. Point it at the Comic API (e.g. http://localhost:5000)."
    );
  }
  return baseUrl.replace(/\/+$/, "");
}

export function comicApiBaseUrl(): string {
  return requireApiBaseUrl();
}

type TokenResponse = {
  access_token?: string;
  expires_in?: number;
  refresh_token?: string | null;
  token_type?: string;
};

async function persistSession(tokens: SessionTokens): Promise<void> {
  const jar = await cookies();
  jar.set(SESSION_COOKIE, sealSession(tokens), sessionCookieOptions(process.env.NODE_ENV === "production"));
}

async function refreshSession(session: SessionTokens): Promise<SessionTokens | null> {
  if (!session.refreshToken) return null;
  const res = await fetch(`${requireApiBaseUrl()}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refresh_token: session.refreshToken }),
    cache: "no-store",
  });
  if (!res.ok) return null;
  const body = (await res.json()) as TokenResponse;
  if (!body.access_token) return null;
  const next: SessionTokens = {
    accessToken: body.access_token,
    refreshToken: body.refresh_token ?? session.refreshToken,
    expiresAt: Date.now() + (body.expires_in ?? 3600) * 1000,
  };
  await persistSession(next);
  return next;
}

const authedTransport: AsyncApiTransport = async (request) => {
  const attempt = (token?: string) => {
    const headers = { ...request.headers };
    if (token) headers.Authorization = `Bearer ${token}`;
    else delete headers.Authorization;
    return fetchTransport({ ...request, headers });
  };

  let session = await readSession();
  if (!session?.accessToken) return await rejectUnauthorized();
  if (session.expiresAt - Date.now() < 60_000) {
    session = (await refreshSession(session)) ?? session;
  }

  let response = await attempt(session.accessToken);
  if (response.status === 401 && session.refreshToken) {
    const refreshed = await refreshSession(session);
    if (refreshed) {
      session = refreshed;
      response = await attempt(refreshed.accessToken);
    }
  }
  if (response.status === 401) return await rejectUnauthorized();
  return response;
};

/** Server-only base Lyo API client with the session Bearer. */
export async function getApi(signal?: AbortSignal): Promise<AsyncApiClient> {
  return createAsyncApiClient({
    baseUrl: requireApiBaseUrl(),
    transport: authedTransport,
    signal,
  });
}

/** Server-only Comic API client. Never import from Client Components. */
export async function getComicApi(signal?: AbortSignal) {
  return createAsyncComicApiClient(await getApi(signal));
}

function throwIfAborted(signal?: AbortSignal | null): void {
  if (!signal?.aborted)
    return;
  const err = new Error("This operation was aborted");
  err.name = "AbortError";
  throw err;
}

/** Raw fetch against the Comic API, attaching the session Bearer. Refreshes once on 401. */
export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  throwIfAborted(init?.signal);
  const base = requireApiBaseUrl();
  const url = path.startsWith("http") ? path : `${base}${path.startsWith("/") ? path : `/${path}`}`;

  const attempt = async (token?: string) => {
    throwIfAborted(init?.signal);
    const headers = new Headers(init?.headers);
    if (token && !headers.has("Authorization")) {
      headers.set("Authorization", `Bearer ${token}`);
    }
    return fetch(url, { ...init, headers, cache: "no-store", keepalive: false });
  };

  let session = await readSession();
  let res = await attempt(session?.accessToken);
  if (res.status === 401 && session?.refreshToken) {
    throwIfAborted(init?.signal);
    const refreshed = await refreshSession(session);
    if (refreshed) {
      session = refreshed;
      res = await attempt(refreshed.accessToken);
    }
  }
  return res;
}

export async function exchangeHandoff(code: string, origin: string): Promise<SessionTokens | null> {
  const res = await fetch(`${requireApiBaseUrl()}/auth/handoff/exchange`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Origin: origin,
      "X-Lyo-Caller-Origin": origin,
    },
    body: JSON.stringify({ code }),
    cache: "no-store",
  });
  if (!res.ok) return null;
  const body = (await res.json()) as TokenResponse;
  if (!body.access_token) return null;
  return {
    accessToken: body.access_token,
    refreshToken: body.refresh_token ?? null,
    expiresAt: Date.now() + (body.expires_in ?? 3600) * 1000,
  };
}

export async function logoutUpstream(): Promise<void> {
  const jar = await cookies();
  const raw = jar.get(SESSION_COOKIE)?.value;
  const session = raw ? unsealSession(raw) : null;
  if (session?.refreshToken) {
    await fetch(`${requireApiBaseUrl()}/auth/logout`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refresh_token: session.refreshToken }),
      cache: "no-store",
    }).catch(() => undefined);
  }
}

export { persistSession };
