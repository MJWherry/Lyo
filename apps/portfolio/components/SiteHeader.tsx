"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import site from "@/content/site.json";
import { ThemeToggle } from "./ThemeToggle";

const links = [
  { href: "/", label: "Home", match: (p: string) => p === "/" },
  { href: "/features", label: "Features", match: (p: string) => p.startsWith("/features") },
  { href: "/benchmarks", label: "Benchmarks", match: (p: string) => p.startsWith("/benchmarks") },
  { href: "/demos", label: "Demos", match: (p: string) => p.startsWith("/demos") },
];

export function SiteHeader() {
  const pathname = usePathname();

  return (
    <header className="site-header">
      <div className="shell site-header-inner">
        <div style={{ display: "flex", alignItems: "baseline", gap: "0.85rem", flexWrap: "wrap" }}>
          <Link href="/" className="brand">
            Lyo<span>.</span>
          </Link>
          <span className="faint" style={{ fontFamily: "var(--font-display)", fontSize: "0.82rem" }}>
            Matthew Wherry
          </span>
        </div>
        <nav className="nav" aria-label="Primary">
          {links.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              aria-current={link.match(pathname) ? "page" : undefined}
            >
              {link.label}
            </Link>
          ))}
          <Link href={site.resumePath} aria-current={pathname === "/resume" ? "page" : undefined}>
            Resume
          </Link>
          <a href={site.githubUrl} target="_blank" rel="noreferrer">
            GitHub
          </a>
          <ThemeToggle />
        </nav>
      </div>
    </header>
  );
}
