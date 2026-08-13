import "server-only";

import { createCipheriv, createDecipheriv, randomBytes, scryptSync } from "node:crypto";
import { cookies } from "next/headers";
import { SESSION_COOKIE } from "./constants";

export { SESSION_COOKIE };
const COOKIE_MAX_AGE_SEC = 60 * 60 * 24 * 14;

export type SessionTokens = {
  accessToken: string;
  refreshToken?: string | null;
  expiresAt: number;
};

function cookieKey(): Buffer {
  const secret = process.env.AUTH_COOKIE_SECRET?.trim();
  if (!secret) throw new Error("AUTH_COOKIE_SECRET is not set.");
  return scryptSync(secret, "lyo-comic-session-v1", 32);
}

export function sealSession(tokens: SessionTokens): string {
  const iv = randomBytes(12);
  const cipher = createCipheriv("aes-256-gcm", cookieKey(), iv);
  const plaintext = Buffer.from(JSON.stringify(tokens), "utf8");
  const encrypted = Buffer.concat([cipher.update(plaintext), cipher.final()]);
  const tag = cipher.getAuthTag();
  return Buffer.concat([iv, tag, encrypted]).toString("base64url");
}

export function unsealSession(sealed: string): SessionTokens | null {
  try {
    const buf = Buffer.from(sealed, "base64url");
    if (buf.length < 29) return null;
    const iv = buf.subarray(0, 12);
    const tag = buf.subarray(12, 28);
    const encrypted = buf.subarray(28);
    const decipher = createDecipheriv("aes-256-gcm", cookieKey(), iv);
    decipher.setAuthTag(tag);
    const plain = Buffer.concat([decipher.update(encrypted), decipher.final()]);
    const parsed = JSON.parse(plain.toString("utf8")) as SessionTokens;
    if (!parsed?.accessToken || typeof parsed.expiresAt !== "number") return null;
    return parsed;
  } catch {
    return null;
  }
}

export function sessionCookieOptions(secure: boolean) {
  return {
    httpOnly: true,
    sameSite: "lax" as const,
    path: "/",
    secure,
    maxAge: COOKIE_MAX_AGE_SEC,
  };
}

export async function readSession(): Promise<SessionTokens | null> {
  const jar = await cookies();
  const raw = jar.get(SESSION_COOKIE)?.value;
  if (!raw) return null;
  return unsealSession(raw);
}
