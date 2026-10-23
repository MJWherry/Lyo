export type ScheduleType = "SetTimes" | "Interval" | "OneShot" | "Cron";

export interface ScheduleDefinition {
    type: ScheduleType;
    dayFlags?: number;
    monthFlags?: number;
    times?: string[];
    startTime?: string;
    endTime?: string;
    intervalMinutes?: number;
    /** Instant the interval counts from, or the one-shot instant. */
    executeAt?: string;
    timeZone?: string;
    enabled?: boolean;
    description?: string;
    cronExpression?: string;
}

const DAILY_CRON = /^(\d{1,2}) (\d{1,2}) \* \* \*$/;

function monthBounds(year: number, month: number): {start: number; end: number} {
    return {start: Date.UTC(year, month - 1, 1), end: Date.UTC(year, month, 1)};
}

function inMonth(instant: number, year: number, month: number, fromMs: number): boolean {
    const bounds = monthBounds(year, month);
    return instant >= Math.max(fromMs, bounds.start) && instant < bounds.end;
}

/** Interval hits inside the visible month, stepping from `executeAt` (or `from`). */
export function intervalPreview(definition: ScheduleDefinition, from: Date, year: number, month: number): string[] {
    const step = (definition.intervalMinutes ?? 1440) * 60_000;
    if (step <= 0) return [];
    const origin = new Date(definition.executeAt ?? from.toISOString()).getTime();
    const bounds = monthBounds(year, month);
    const floor = Math.max(from.getTime(), bounds.start);
    let cursor = origin;
    if (cursor < floor) {
        const steps = Math.ceil((floor - cursor) / step);
        cursor += steps * step;
    }
    const hits: string[] = [];
    while (cursor < bounds.end) {
        hits.push(new Date(cursor).toISOString());
        cursor += step;
    }
    return hits;
}

function setTimeHits(definition: ScheduleDefinition, from: Date, year: number, month: number): string[] {
    const bounds = monthBounds(year, month);
    const hits: string[] = [];
    const times = definition.times ?? [];
    for (let cursor = bounds.start; cursor < bounds.end; cursor += 86_400_000) {
        const date = new Date(cursor);
        const dayBit = 1 << date.getUTCDay();
        const monthBit = 1 << date.getUTCMonth();
        if (definition.dayFlags !== undefined && (definition.dayFlags & dayBit) === 0) continue;
        if (definition.monthFlags !== undefined && (definition.monthFlags & monthBit) === 0) continue;
        for (const time of times) {
            const [hour, minute] = time.split(":").map((part) => Number(part));
            const instant = Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), hour || 0, minute || 0);
            if (inMonth(instant, year, month, from.getTime())) hits.push(new Date(instant).toISOString());
        }
    }
    return hits;
}

function cronHits(expression: string, from: Date, year: number, month: number): string[] {
    const match = DAILY_CRON.exec(expression.trim());
    if (!match) return [];
    return setTimeHits({type: "SetTimes", times: [`${match[2]}:${match[1]}`], dayFlags: 127, monthFlags: 4095}, from, year, month);
}

export function previewOccurrences(definition: ScheduleDefinition, from: Date, year: number, month: number): string[] {
    if (definition.enabled === false) return [];
    switch (definition.type) {
        case "Interval":
            return intervalPreview(definition, from, year, month);
        case "OneShot": {
            if (!definition.executeAt) return [];
            const instant = new Date(definition.executeAt).getTime();
            return inMonth(instant, year, month, from.getTime()) ? [new Date(instant).toISOString()] : [];
        }
        case "SetTimes":
            return setTimeHits(definition, from, year, month);
        case "Cron":
            return definition.cronExpression ? cronHits(definition.cronExpression, from, year, month) : [];
    }
}
