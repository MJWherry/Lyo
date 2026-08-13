import { NextRequest, NextResponse } from "next/server";
import { SESSION_COOKIE, sessionCookieOptions } from "@/lib/auth/session";

function sanitizeReturn(raw: string | null): string {
  if (!raw) return "/";
  return raw.startsWith("/") && !raw.startsWith("//") ? raw : "/";
}

/** Drop a stale session cookie and send the browser to `/login`. */
export async function GET(request: NextRequest) {
  const dest = sanitizeReturn(request.nextUrl.searchParams.get("return"));
  const login = new URL("/login", request.url);
  if (dest !== "/") login.searchParams.set("return", dest);
  const res = NextResponse.redirect(login);
  res.cookies.set(SESSION_COOKIE, "", {
    ...sessionCookieOptions(request.nextUrl.protocol === "https:"),
    maxAge: 0,
  });
  return res;
}
