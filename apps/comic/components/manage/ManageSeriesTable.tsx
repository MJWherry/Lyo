"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import type { ComicCardRow } from "lyo-comic-api-client";
import { comicCardRowKey, comicFileUrl, comicStatusLabel, comicTypeLabel } from "lyo-comic-api-client";
import { bffFetch } from "@/lib/api/bffFetch";
import { TrashIcon } from "./TrashIcon";

export function ManageSeriesTable({ initial }: { initial: ComicCardRow[] }) {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [rows, setRows] = useState(initial);
  const filtered = rows.filter((r) => (r.title ?? "").toLowerCase().includes(q.toLowerCase()));

  async function remove(id: string) {
    if (!confirm("Delete this series?")) return;
    const res = await bffFetch(`/api/comic/series/${encodeURIComponent(id)}`, { method: "DELETE" });
    if (res.ok) {
      setRows((cur) => cur.filter((r) => r.id !== id));
      router.refresh();
    }
  }

  return (
    <div>
      <div className="field" style={{ maxWidth: "20rem", marginBottom: "1rem" }}>
        <label htmlFor="filter">Filter</label>
        <input id="filter" value={q} onChange={(e) => setQ(e.target.value)} />
      </div>
      <table className="manage-table">
        <thead>
          <tr>
            <th></th>
            <th>Title</th>
            <th>Type</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {filtered.map((r, i) => (
            <tr key={comicCardRowKey(r, i)}>
              <td style={{ width: "3rem" }}>
                {comicFileUrl(r.coverImageRef, r.updatedTimestamp) ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={comicFileUrl(r.coverImageRef, r.updatedTimestamp)!} alt="" width={36} height={54} style={{ objectFit: "cover" }} />
                ) : null}
              </td>
              <td>
                <Link href={`/manage/series/${r.id}`}>{r.title}</Link>
              </td>
              <td>{comicTypeLabel(r.comicType as never)}</td>
              <td>{comicStatusLabel(r.status as never)}</td>
              <td>
                <button type="button" className="icon-btn icon-btn--danger" aria-label={`Delete ${r.title ?? "series"}`} onClick={() => r.id && remove(r.id)}>
                  <TrashIcon />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
