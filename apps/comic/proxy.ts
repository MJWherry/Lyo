import { NextRequest, NextResponse } from "next/server";
import { SESSION_COOKIE } from "@/lib/auth/constants";

const PUBLIC_PREFIXES = ["/login", "/auth"];
const PUBLIC_EXACT = ["/api/health"];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  if (PUBLIC_EXACT.includes(pathname) || PUBLIC_PREFIXES.some((p) => pathname === p || pathname.startsWith(`${p}/`))) {
    return NextResponse.next();
  }

  const session = request.cookies.get(SESSION_COOKIE)?.value;
  if (!session) {
    if (pathname.startsWith("/api/")) {
      return NextResponse.json({ error: "Sign in required." }, { status: 401 });
    }

    const url = request.nextUrl.clone();
    url.pathname = "/login";
    const dest = pathname + request.nextUrl.search;
    url.search = dest && dest !== "/" ? `?return=${encodeURIComponent(dest)}` : "";
    return NextResponse.redirect(url);
  }

  const headers = new Headers(request.headers);
  headers.set("x-lyo-pathname", pathname + request.nextUrl.search);
  return NextResponse.next({ request: { headers } });
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
