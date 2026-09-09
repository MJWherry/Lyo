import {createLyoColumn, type LyoColumn} from "lyo-web-components";
import {jobResultColor, jobStateColor, projectedField, workerStateColor} from "./jobColors.js";

function text(row: Record<string, unknown>, field: string): string {
    const value = projectedField(row, field);
    return value == null ? "" : String(value);
}

export function jobDefinitionColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "enabled", field: "Enabled", header: "Enabled", size: 110}),
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 140}),
        createLyoColumn({id: "name", field: "Name", header: "Name", quickSearch: true}),
        createLyoColumn({id: "description", field: "Description", header: "Description", quickSearch: true}),
        createLyoColumn({id: "type", field: "Type", header: "Type", quickSearch: true}),
        createLyoColumn({id: "workerType", field: "WorkerType", header: "Worker Type"}),
        createLyoColumn({id: "schedules", field: "JobSchedules.Count", header: "Schedules", size: 90}),
        createLyoColumn({id: "params", field: "JobParameters.Count", header: "Parameters", size: 90}),
    ];
}

export function jobRunColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 140}),
        createLyoColumn({id: "definition", field: "JobDefinition.Name", header: "Definition"}),
        createLyoColumn({
            id: "state",
            field: "State",
            header: "State",
            size: 120,
            cell: (row) => text(row, "State"),
        }),
        createLyoColumn({
            id: "result",
            field: "Result",
            header: "Result",
            size: 140,
            cell: (row) => text(row, "Result"),
        }),
        createLyoColumn({id: "worker", field: "WorkerMachineName", header: "Worker", size: 160}),
        createLyoColumn({id: "created", field: "CreatedTimestamp", header: "Created", size: 180}),
        createLyoColumn({id: "progress", field: "ProgressPercent", header: "Progress", size: 90}),
    ];
}

export function jobScheduleColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 140}),
        createLyoColumn({id: "description", field: "Description", header: "Description", quickSearch: true}),
        createLyoColumn({id: "enabled", field: "Enabled", header: "Enabled", size: 110}),
        createLyoColumn({id: "cron", field: "CronExpression", header: "Cron"}),
        createLyoColumn({id: "definition", field: "JobDefinitionId", header: "Definition"}),
    ];
}

export function jobWorkerColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 140}),
        createLyoColumn({id: "type", field: "WorkerType", header: "Worker Type", quickSearch: true}),
        createLyoColumn({id: "machine", field: "MachineName", header: "Machine"}),
        createLyoColumn({id: "pid", field: "ProcessId", header: "PID", size: 80}),
        createLyoColumn({id: "state", field: "State", header: "State", size: 120}),
        createLyoColumn({id: "inFlight", field: "InFlightCount", header: "In flight", size: 90}),
        createLyoColumn({id: "heartbeat", field: "LastHeartbeatUtc", header: "Heartbeat"}),
    ];
}

export function jobWorkflowColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "id", field: "Id", header: "ID", size: 140}),
        createLyoColumn({id: "name", field: "Name", header: "Name", quickSearch: true}),
        createLyoColumn({id: "description", field: "Description", header: "Description"}),
        createLyoColumn({id: "enabled", field: "Enabled", header: "Enabled", size: 110}),
    ];
}

export {jobResultColor, jobStateColor, workerStateColor};
