"use client";

import { useEffect, useRef, useState, type AnchorHTMLAttributes, type MouseEvent, type ReactNode } from "react";
import { DownloadIcon } from "@/components/DownloadIcon";

type Props = {
  href: string;
  children?: ReactNode;
  /** Saved zip name after the fetch completes. */
  fileName?: string;
  /** Accessible name when the control is icon-only. */
  label?: string;
} & Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href" | "download" | "onClick">;

function fileNameFromResponse(res: Response, href: string, fallback?: string): string {
  const raw = res.headers.get("content-disposition");
  if (raw) {
    const star = /filename\*=UTF-8''([^;]+)/i.exec(raw);
    if (star?.[1])
      return decodeURIComponent(star[1]);
    const quoted = /filename="([^"]+)"/i.exec(raw);
    if (quoted?.[1])
      return quoted[1];
    const plain = /filename=([^;]+)/i.exec(raw);
    if (plain?.[1])
      return plain[1].trim();
  }

  if (fallback)
    return fallback;

  const leaf = href.split("?")[0]?.split("/").pop();
  return leaf && leaf.endsWith(".zip") ? leaf : "download.zip";
}

/**
 * Zip download that aborts on refresh / navigation. A plain {@code <a download>} keeps the
 * browser request alive after the page is gone, so the API would keep fetching page images.
 */
export function ArchiveDownloadLink({ href, children, className, fileName, label = "Download", ...rest }: Props) {
  const abortRef = useRef<AbortController | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const abort = () => abortRef.current?.abort();
    window.addEventListener("pagehide", abort);
    window.addEventListener("beforeunload", abort);
    return () => {
      abort();
      window.removeEventListener("pagehide", abort);
      window.removeEventListener("beforeunload", abort);
    };
  }, []);

  async function onClick(event: MouseEvent<HTMLAnchorElement>) {
    event.preventDefault();
    if (busy)
      return;

    abortRef.current?.abort();
    const ac = new AbortController();
    abortRef.current = ac;
    setBusy(true);
    try {
      const res = await fetch(href, { method: "GET", signal: ac.signal, credentials: "same-origin", cache: "no-store" });
      if (!res.ok)
        return;

      const blob = await res.blob();
      if (ac.signal.aborted)
        return;

      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileNameFromResponse(res, href, fileName);
      link.click();
      URL.revokeObjectURL(url);
    }
    catch {
      if (ac.signal.aborted)
        return;
    }
    finally {
      if (!ac.signal.aborted)
        setBusy(false);
    }
  }

  return (
    <a
      className={className ?? "icon-btn"}
      href={href}
      aria-label={label}
      aria-busy={busy}
      aria-disabled={busy}
      onClick={onClick}
      {...rest}
    >
      {busy ? <span className="spinner" /> : <DownloadIcon />}
      {children ? (busy ? "Downloading…" : children) : null}
    </a>
  );
}
