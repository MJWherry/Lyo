import Link from "next/link";

const features = [
  { href: "/features/query", label: "Query" },
  { href: "/features/jobs", label: "Jobs" },
  { href: "/features/reporting", label: "Reporting" },
  { href: "/features/file-storage", label: "File storage" },
  { href: "/features/file-system-watcher", label: "FS watcher" },
  { href: "/features/encryption", label: "Encryption" },
  { href: "/features/compression", label: "Compression" },
  { href: "/features/temp-io", label: "Temp IO" },
  { href: "/features/platform", label: "Platform" },
];

export function FeatureNav({ current }: { current: string }) {
  return (
    <nav className="shell" aria-label="Features" style={{ marginBottom: "1.5rem" }}>
      <div className="nav" style={{ gap: "0.65rem 1rem" }}>
        {features.map((f) => (
          <Link
            key={f.href}
            href={f.href}
            aria-current={current === f.href ? "page" : undefined}
          >
            {f.label}
          </Link>
        ))}
      </div>
    </nav>
  );
}
