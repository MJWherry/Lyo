import { NextRequest, NextResponse } from "next/server";

function sanitizeReturn(raw: string | null): string {
  if (!raw) return "/";
  return raw.startsWith("/") && !raw.startsWith("//") ? raw : "/";
}

/** 302 to Comic API Google OIDC login with browser handoff return. */
export async function GET(request: NextRequest) {
  const api = (
    process.env.LYO_COMIC_PUBLIC_AUTH_URL?.trim() ||
    process.env.LYO_COMIC_API_BASE_URL?.trim()
  )?.replace(/\/+$/, "");
  if (!api) {
    return NextResponse.json({ error: "LYO_COMIC_PUBLIC_AUTH_URL or LYO_COMIC_API_BASE_URL is not set." }, { status: 503 });
  }
  const origin = request.nextUrl.origin;
  const safeReturn = sanitizeReturn(request.nextUrl.searchParams.get("return"));
  const callback =
    `${origin}/auth/handoff` + (safeReturn === "/" ? "" : `?return=${encodeURIComponent(safeReturn)}`);
  const target = `${api}/auth/login/google?mode=browser&returnUrl=${encodeURIComponent(callback)}`;
  return NextResponse.redirect(target);
}
