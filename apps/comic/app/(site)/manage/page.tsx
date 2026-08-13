import Link from "next/link";
import { ManageSeriesTable } from "@/components/manage/ManageSeriesTable";

export const dynamic = "force-dynamic";

export default function ManagePage() {
  return (
    <div className="shell">
      <div className="section-header">
        <h1>Library</h1>
        <Link className="btn" href="/manage/series/new">
          New series
        </Link>
      </div>
      <ManageSeriesTable />
    </div>
  );
}
