import { NextRequest, NextResponse } from "next/server";
import { logoutUpstream } from "@/lib/api/serverClient";
import { SESSION_COOKIE, sessionCookieOptions } from "@/lib/auth/session";

export async function GET(request: NextRequest) {
  await logoutUpstream();
  const res = NextResponse.redirect(new URL("/login", request.url));
  res.cookies.set(SESSION_COOKIE, "", { ...sessionCookieOptions(request.nextUrl.protocol === "https:"), maxAge: 0 });
  return res;
}

export async function POST(request: NextRequest) {
  return GET(request);
}
