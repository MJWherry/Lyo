"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { FormEvent, useState } from "react";
import { ThemeToggle } from "./ThemeToggle";

const links = [
  { href: "/", label: "Home", match: (p: string) => p === "/" },
  { href: "/search", label: "Browse", match: (p: string) => p.startsWith("/search") },
  { href: "/manage", label: "Library", match: (p: string) => p.startsWith("/manage") },
];

export function SiteHeader() {
  const pathname = usePathname();
  const router = useRouter();
  const [q, setQ] = useState("");

  function onSearch(e: FormEvent) {
    e.preventDefault();
    const query = q.trim();
    router.push(query ? `/search?q=${encodeURIComponent(query)}` : "/search");
  }

  return (
    <header className="site-header">
      <div className="shell site-header-inner">
        <Link href="/" className="brand">
          Lyo<span>Comic</span>
        </Link>
        <form className="header-search" onSubmit={onSearch} role="search">
          <input
            type="search"
            name="q"
            value={q}
            onChange={(e) => setQ(e.target.value)}
            placeholder="Search manga…"
            aria-label="Search"
          />
        </form>
        <nav className="nav" aria-label="Primary">
          {links.map((link) => (
            <Link key={link.href} href={link.href} aria-current={link.match(pathname) ? "page" : undefined}>
              {link.label}
            </Link>
          ))}
          <Link href="/auth/sign-out">Sign out</Link>
          <ThemeToggle />
        </nav>
      </div>
    </header>
  );
}
