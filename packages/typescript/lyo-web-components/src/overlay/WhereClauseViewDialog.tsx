"use client";

import type { WhereClause } from "lyo-query";
import { LyoJsonViewDialog } from "./LyoJsonViewDialog.js";

export function WhereClauseViewDialog({
  open,
  onClose,
  value,
}: {
  open: boolean;
  onClose: () => void;
  value: WhereClause | null | undefined;
}) {
  return (
    <LyoJsonViewDialog open={open} onClose={onClose} title="Where clause" value={value ?? null} />
  );
}
