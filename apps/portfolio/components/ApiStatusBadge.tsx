"use client";

import { useEffect, useState } from "react";

type HealthState = "loading" | "up" | "down";

export function ApiStatusBadge() {
  const [state, setState] = useState<HealthState>("loading");

  useEffect(() => {
    let cancelled = false;
    fetch("/api/health")
      .then(async (res) => {
        if (cancelled) return;
        setState(res.ok ? "up" : "down");
      })
      .catch(() => {
        if (!cancelled) setState("down");
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const label =
    state === "loading" ? "API…" : state === "up" ? "API online" : "API offline";

  return (
    <span className={`badge ${state === "up" ? "badge-ok" : state === "down" ? "badge-warn" : ""}`}>
      <span className={`status-dot ${state === "up" ? "on" : ""}`} aria-hidden />
      {label}
    </span>
  );
}
