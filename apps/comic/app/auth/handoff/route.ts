import { NextRequest, NextResponse } from "next/server";
import { exchangeHandoff } from "@/lib/api/serverClient";
import { SESSION_COOKIE, sessionCookieOptions, sealSession } from "@/lib/auth/session";

function sanitizeReturn(raw: string | null): string {
  if (!raw) return "/";
  return raw.startsWith("/") && !raw.startsWith("//") ? raw : "/";
}

/** Redeem `lyo_handoff`, seal tokens in an httpOnly cookie, redirect home. */
export async function GET(request: NextRequest) {
  const code = request.nextUrl.searchParams.get("lyo_handoff");
  const dest = sanitizeReturn(request.nextUrl.searchParams.get("return"));
  if (!code) {
    return NextResponse.redirect(new URL("/login?error=missing_handoff", request.url));
  }

  const origin = request.nextUrl.origin;
  const tokens = await exchangeHandoff(code, origin);
  if (!tokens) {
    return NextResponse.redirect(new URL("/login?error=handoff_failed", request.url));
  }

  const res = NextResponse.redirect(new URL(dest, request.url));
  res.cookies.set(SESSION_COOKIE, sealSession(tokens), sessionCookieOptions(request.nextUrl.protocol === "https:"));
  return res;
}
