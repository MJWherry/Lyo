import {createLyoColumn, type LyoColumn} from "lyo-web-components";
import {projectedField} from "./reportColors.js";

function text(row: Record<string, unknown>, field: string): string {
    const value = projectedField(row, field);
    return value == null ? "" : String(value);
}

export function reportDefinitionColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "active", field: "IsActive", header: "Active", size: 96}),
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 110}),
        createLyoColumn({id: "name", field: "Name", header: "Name", quickSearch: true}),
        createLyoColumn({id: "description", field: "Description", header: "Description", quickSearch: true}),
        createLyoColumn({id: "format", field: "DefaultFormat", header: "Format", size: 100}),
        createLyoColumn({id: "profile", field: "GenerationProfileKey", header: "Profile"}),
        createLyoColumn({id: "created", field: "CreatedTimestamp", header: "Created", size: 180}),
    ];
}

export function reportGenerationColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 110}),
        createLyoColumn({id: "definition", field: "ReportDefinition.Name", header: "Definition", quickSearch: true}),
        createLyoColumn({
            id: "status",
            field: "Status",
            header: "Status",
            size: 120,
            cell: (row) => text(row, "Status"),
        }),
        createLyoColumn({id: "format", field: "Format", header: "Format", size: 90}),
        createLyoColumn({id: "file", field: "OriginalFileName", header: "File"}),
        createLyoColumn({id: "created", field: "CreatedTimestamp", header: "Created", size: 180}),
        createLyoColumn({id: "finished", field: "FinishedTimestamp", header: "Finished", size: 180}),
    ];
}
