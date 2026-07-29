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
  { href: "/about", label: "About", match: (p: string) => p.startsWith("/about") },
];

export function SiteHeader() {
  const pathname = usePathname();

  return (
    <header className="site-header">
      <div className="shell site-header-inner">
        <Link href="/" className="brand">
          Lyo<span>.</span>
        </Link>
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
          <a href={site.githubUrl} target="_blank" rel="noreferrer">
            GitHub
          </a>
          <ThemeToggle />
        </nav>
      </div>
    </header>
  );
}
