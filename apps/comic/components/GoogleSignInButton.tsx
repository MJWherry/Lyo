"use client";

import { useState } from "react";

export function GoogleSignInButton({ href }: { href: string }) {
  const [busy, setBusy] = useState(false);

  return (
    <a
      className="btn"
      href={href}
      aria-busy={busy}
      aria-disabled={busy}
      onClick={(event) => {
        if (busy) {
          event.preventDefault();
          return;
        }
        setBusy(true);
      }}
    >
      {busy ? <span className="spinner" /> : null}
      {busy ? "Signing in…" : "Sign in with Google"}
    </a>
  );
}
