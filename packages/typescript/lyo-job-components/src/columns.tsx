"use client";

import {
    LyoChip,
    LyoHoverPopover,
    chipOf,
    createLyoColumn,
    createLyoIdColumn,
    parameterHoverSummary,
    type LyoColumn,
} from "lyo-web-components";
import { enabledChip, jobResultChip, jobStateChip, workerStateChip } from "./gridDisplay.js";
import { projectedField } from "./jobColors.js";

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

function idColumn(id: string, size = 140): LyoColumn<Row> {
    return createLyoIdColumn({ id, field: "Id", header: "ID", size, idAbbreviation: "prefix", idAbbreviationLength: 8 });
}

function ParameterCount({ row }: { row: Row }) {
    const summary = parameterHoverSummary(row);
    const label = summary.keys.length > 0 ? String(summary.keys.length) : summary.text;
    const chip = <LyoChip label={label} size="small" />;
    if (summary.keys.length === 0) return chip;
    return (
        <LyoHoverPopover text={summary.text} title="Parameters" pinnable={false}>
            {chip}
        </LyoHoverPopover>
    );
}

export function jobDefinitionColumns(actions?: { onToggleEnabled?: (row: Row) => void }): LyoColumn<Row>[] {
    return [
        createLyoColumn({
            id: "enabled",
            field: "Enabled",
            header: "Enabled",
            size: 120,
            cell: (row) => (
                <LyoChip
                    spec={enabledChip(boolOf(projectedField(row, "Enabled")))}
                    onClick={
                        actions?.onToggleEnabled
                            ? (event) => {
                                  event.stopPropagation();
                                  actions.onToggleEnabled?.(row);
                              }
                            : undefined
                    }
                    sx={actions?.onToggleEnabled ? { cursor: "pointer" } : undefined}
                />
            ),
        }),
        idColumn("id"),
        createLyoColumn({ id: "name", field: "Name", header: "Name", quickSearch: true }),
        createLyoColumn({ id: "description", field: "Description", header: "Description", quickSearch: true }),
        createLyoColumn({
            id: "type",
            field: "Type",
            header: "Type",
            quickSearch: true,
            cell: (row) => <LyoChip spec={chipOf(text(row, "Type"))} />,
        }),
        createLyoColumn({
            id: "workerType",
            field: "WorkerType",
            header: "Worker Type",
            cell: (row) => <LyoChip spec={chipOf(text(row, "WorkerType"))} />,
        }),
        createLyoColumn({ id: "schedules", field: "JobSchedules.Count", header: "Schedules", size: 110 }),
        createLyoColumn({
            id: "params",
            field: "JobParameters.Count",
            header: "Parameters",
            size: 130,
            minSize: 120,
            cell: (row) => <ParameterCount row={row} />,
        }),
    ];
}

export function jobRunColumns(): LyoColumn<Row>[] {
    return [
        idColumn("id"),
        createLyoColumn({ id: "definition", field: "JobDefinition.Name", header: "Definition" }),
        createLyoColumn({
            id: "state",
            field: "State",
            header: "State",
            size: 130,
            cell: (row) => <LyoChip spec={jobStateChip(text(row, "State"))} />,
        }),
        createLyoColumn({
            id: "result",
            field: "Result",
            header: "Result",
            size: 160,
            cell: (row) => <LyoChip spec={jobResultChip(text(row, "Result"))} />,
        }),
        createLyoColumn({ id: "worker", field: "WorkerMachineName", header: "Worker", size: 160 }),
        createLyoColumn({ id: "created", field: "CreatedTimestamp", header: "Created", size: 180, timestamp: true }),
        createLyoColumn({ id: "progress", field: "ProgressPercent", header: "Progress", size: 100 }),
    ];
}

export function jobScheduleColumns(actions?: { onToggleEnabled?: (row: Row) => void }): LyoColumn<Row>[] {
    return [
        idColumn("id"),
        createLyoColumn({ id: "description", field: "Description", header: "Description", quickSearch: true }),
        createLyoColumn({
            id: "enabled",
            field: "Enabled",
            header: "Enabled",
            size: 120,
            cell: (row) => (
                <LyoChip
                    spec={enabledChip(boolOf(projectedField(row, "Enabled")))}
                    onClick={
                        actions?.onToggleEnabled
                            ? (event) => {
                                  event.stopPropagation();
                                  actions.onToggleEnabled?.(row);
                              }
                            : undefined
                    }
                    sx={actions?.onToggleEnabled ? { cursor: "pointer" } : undefined}
                />
            ),
        }),
        createLyoColumn({ id: "cron", field: "CronExpression", header: "Cron" }),
        createLyoColumn({ id: "definition", field: "JobDefinitionId", header: "Definition" }),
    ];
}

export function jobWorkerColumns(): LyoColumn<Row>[] {
    return [
        idColumn("id"),
        createLyoColumn({
            id: "type",
            field: "WorkerType",
            header: "Worker Type",
            quickSearch: true,
            cell: (row) => <LyoChip spec={chipOf(text(row, "WorkerType"))} />,
        }),
        createLyoColumn({ id: "machine", field: "MachineName", header: "Machine" }),
        createLyoColumn({ id: "pid", field: "ProcessId", header: "PID", size: 80 }),
        createLyoColumn({
            id: "state",
            field: "State",
            header: "State",
            size: 130,
            cell: (row) => <LyoChip spec={workerStateChip(text(row, "State"))} />,
        }),
        createLyoColumn({ id: "inFlight", field: "InFlightCount", header: "In flight", size: 100 }),
        createLyoColumn({ id: "heartbeat", field: "LastHeartbeatUtc", header: "Heartbeat", timestamp: true }),
    ];
}

export function jobWorkflowColumns(): LyoColumn<Row>[] {
    return [
        idColumn("id"),
        createLyoColumn({ id: "name", field: "Name", header: "Name", quickSearch: true }),
        createLyoColumn({ id: "description", field: "Description", header: "Description" }),
        createLyoColumn({
            id: "enabled",
            field: "Enabled",
            header: "Enabled",
            size: 120,
            cell: (row) => <LyoChip spec={enabledChip(boolOf(projectedField(row, "Enabled")))} />,
        }),
    ];
}

export { jobResultColor, jobStateColor, workerStateColor } from "./jobColors.js";
