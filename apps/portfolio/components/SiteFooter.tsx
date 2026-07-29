import Link from "next/link";
import site from "@/content/site.json";
import { ApiStatusBadge } from "./ApiStatusBadge";

export function SiteFooter() {
  return (
    <footer className="site-footer">
      <div
        className="shell"
        style={{
          display: "flex",
          justifyContent: "space-between",
          gap: "1rem",
          flexWrap: "wrap",
          alignItems: "center",
        }}
      >
        <p style={{ margin: 0 }}>
          <Link href="/about">{site.fullName}</Link>
          {" · "}
          {site.brand} — {site.tagline}
          {" · "}
          <Link href={site.resumePath}>Resume</Link>
          {" · "}
          <a href={site.githubUrl} target="_blank" rel="noreferrer">
            GitHub
          </a>
        </p>
        <ApiStatusBadge />
      </div>
    </footer>
  );
}
