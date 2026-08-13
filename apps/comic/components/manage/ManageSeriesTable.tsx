"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import type { ComicCardRow } from "lyo-comic-api-client";
import { comicFileUrl, comicStatusLabel, comicTypeLabel } from "lyo-comic-api-client";
import { filterProperty } from "lyo-query";
import {
  LyoDataGridFeatureFlags,
  LyoDataGridProjected,
  createBffQueryClient,
  createLyoColumn,
} from "lyo-web-components";
import { bffFetch } from "@/lib/api/bffFetch";
import { TrashIcon } from "./TrashIcon";

const client = createBffQueryClient({
  projectPath: "/api/comic/series/QueryProject",
  fetchImpl: (input, init) => bffFetch(input, init),
});

const FEATURES =
  LyoDataGridFeatureFlags.Filterable |
  LyoDataGridFeatureFlags.Searchable |
  LyoDataGridFeatureFlags.BulkMenu;

export function ManageSeriesTable() {
  const router = useRouter();

  return (
    <LyoDataGridProjected<ComicCardRow>
      apiClient={client}
      gridKey="ComicSeriesManage"
      route="/api/comic/series"
      columns={[
        createLyoColumn<ComicCardRow>({
          id: "cover",
          field: "CoverImageRef",
          header: "",
          sortable: false,
          filterable: false,
          hideable: false,
          cell: (r) =>
            comicFileUrl(r.coverImageRef, r.updatedTimestamp) ? (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                src={comicFileUrl(r.coverImageRef, r.updatedTimestamp)!}
                alt=""
                width={36}
                height={54}
                style={{ objectFit: "cover" }}
              />
            ) : null,
        }),
        createLyoColumn<ComicCardRow>({
          id: "title",
          field: "Title",
          header: "Title",
          quickSearch: true,
          cell: (r) => <Link href={`/manage/series/${r.id}`}>{r.title}</Link>,
        }),
        createLyoColumn<ComicCardRow>({
          id: "type",
          field: "ComicType",
          header: "Type",
          cell: (r) => comicTypeLabel(r.comicType as never),
        }),
        createLyoColumn<ComicCardRow>({
          id: "status",
          field: "Status",
          header: "Status",
          cell: (r) => comicStatusLabel(r.status as never),
        }),
      ]}
      keySelector={(r) => (r.id ? [r.id] : [])}
      quickSearchProperties={["Title"]}
      filterPropertyDefinitions={[filterProperty("Title"), filterProperty("ComicType"), filterProperty("Status")]}
      features={FEATURES}
      pageSizes={[25, 50, 100]}
      rowMenu={(r) => (
        <button
          type="button"
          className="icon-btn icon-btn--danger"
          aria-label={`Delete ${r.title ?? "series"}`}
          onClick={async () => {
            if (!r.id || !confirm("Delete this series?")) return;
            const res = await bffFetch(`/api/comic/series/${encodeURIComponent(r.id)}`, { method: "DELETE" });
            if (res.ok) router.refresh();
          }}
        >
          <TrashIcon />
        </button>
      )}
    />
  );
}
