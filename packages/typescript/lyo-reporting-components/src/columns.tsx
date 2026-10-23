"use client";

import { LyoChip, chipOf, createLyoColumn, createLyoIdColumn, type LyoChipSpec, type LyoColumn } from "lyo-web-components";
import { formatChip } from "./gridDisplay.js";
import { generationStatusColor, projectedField } from "./reportColors.js";

type Row = Record<string, unknown>;

function text(row: Row, field: string): string {
    const value = projectedField(row, field);
    return value == null ? "" : String(value);
}

function boolOf(value: unknown): boolean | null {
    if (value === true || value === "true" || value === "True") return true;
    if (value === false || value === "false" || value === "False") return false;
    return null;
}

function statusChip(status: string | null | undefined): LyoChipSpec {
    const color = generationStatusColor(status);
    return chipOf(status, { color: color === "inherit" || color === "secondary" ? "default" : color });
}

function activeChip(value: boolean | null): LyoChipSpec {
    return value === true ? chipOf("Active", { color: "success" }) : value === false ? chipOf("Off") : chipOf(null);
}

function idColumn(id: string, size = 120): LyoColumn<Row> {
    return createLyoIdColumn({ id, field: "Id", header: "ID", size, idAbbreviation: "prefix", idAbbreviationLength: 8 });
}

export function reportDefinitionColumns(actions?: { onToggleActive?: (row: Row) => void }): LyoColumn<Row>[] {
    return [
        createLyoColumn({
            id: "active",
            field: "IsActive",
            header: "Active",
            size: 110,
            cell: (row) => (
                <LyoChip
                    spec={activeChip(boolOf(projectedField(row, "IsActive")))}
                    onClick={
                        actions?.onToggleActive
                            ? (event) => {
                                  event.stopPropagation();
                                  actions.onToggleActive?.(row);
                              }
                            : undefined
                    }
                    sx={actions?.onToggleActive ? { cursor: "pointer" } : undefined}
                />
            ),
        }),
        idColumn("id"),
        createLyoColumn({ id: "name", field: "Name", header: "Name", quickSearch: true }),
        createLyoColumn({ id: "description", field: "Description", header: "Description", quickSearch: true }),
        createLyoColumn({
            id: "format",
            field: "DefaultFormat",
            header: "Format",
            size: 110,
            cell: (row) => <LyoChip spec={formatChip(text(row, "DefaultFormat"))} />,
        }),
        createLyoColumn({ id: "profile", field: "GenerationProfileKey", header: "Profile" }),
        createLyoColumn({ id: "created", field: "CreatedTimestamp", header: "Created", size: 180, timestamp: true }),
    ];
}

export function reportGenerationColumns(): LyoColumn<Row>[] {
    return [
        idColumn("id"),
        createLyoColumn({ id: "definition", field: "ReportDefinition.Name", header: "Definition", quickSearch: true }),
        createLyoColumn({
            id: "status",
            field: "Status",
            header: "Status",
            size: 130,
            cell: (row) => <LyoChip spec={statusChip(text(row, "Status"))} />,
        }),
        createLyoColumn({
            id: "format",
            field: "Format",
            header: "Format",
            size: 110,
            cell: (row) => <LyoChip spec={formatChip(text(row, "Format"))} />,
        }),
        createLyoColumn({ id: "file", field: "OriginalFileName", header: "File" }),
        createLyoColumn({ id: "created", field: "CreatedTimestamp", header: "Created", size: 180, timestamp: true }),
        createLyoColumn({ id: "finished", field: "FinishedTimestamp", header: "Finished", size: 180, timestamp: true }),
    ];
}
