"use client";

import {useMemo, useState} from "react";
import {LyoCalendar, LyoMenuItem, LyoSelect, LyoStack, LyoTextField, forMonth, type TimeFrame} from "lyo-web-components";
import {previewOccurrences, type ScheduleDefinition, type ScheduleType} from "./schedule.js";

const TYPES: ScheduleType[] = ["SetTimes", "Interval", "OneShot", "Cron"];

export type ScheduleEditorProps = {
    value?: ScheduleDefinition;
    onChange?: (value: ScheduleDefinition) => void;
};

/** Edits a schedule definition and previews hits on `LyoCalendar`. No HTTP. */
export function ScheduleEditor({value, onChange}: ScheduleEditorProps) {
    const [draft, setDraft] = useState<ScheduleDefinition>(value ?? {type: "Interval", intervalMinutes: 1440, enabled: true});
    const [year, setYear] = useState(2026);
    const [month, setMonth] = useState(3);
    const update = (next: ScheduleDefinition) => {
        setDraft(next);
        onChange?.(next);
    };
    const calendar = useMemo(() => {
        const from = new Date(Date.UTC(year, month - 1, 1));
        const frames: TimeFrame[] = previewOccurrences(draft, from, year, month).map((instant) => ({
            key: instant,
            title: draft.type,
            start: instant,
            end: instant,
        }));
        return forMonth(year, month, draft.timeZone ?? "UTC", frames);
    }, [draft, month, year]);

    return (
        <LyoStack spacing={1}>
            <LyoSelect label="Type" value={draft.type} onChange={(event) => update({...draft, type: String(event.target.value) as ScheduleType})}>
                {TYPES.map((type) => (
                    <LyoMenuItem key={type} value={type}>
                        {type}
                    </LyoMenuItem>
                ))}
            </LyoSelect>
            {draft.type === "Interval" ? (
                <LyoTextField
                    label="Interval minutes"
                    type="number"
                    value={String(draft.intervalMinutes ?? 1440)}
                    onChange={(event) => update({...draft, intervalMinutes: Number(event.target.value)})}
                />
            ) : null}
            {draft.type === "OneShot" || draft.type === "Interval" ? (
                <LyoTextField label="Execute at" value={draft.executeAt ?? ""} onChange={(event) => update({...draft, executeAt: event.target.value})} />
            ) : null}
            {draft.type === "SetTimes" ? (
                <LyoTextField
                    label="Times"
                    value={(draft.times ?? []).join(", ")}
                    onChange={(event) => update({...draft, times: event.target.value.split(",").map((part) => part.trim()).filter(Boolean)})}
                />
            ) : null}
            {draft.type === "Cron" ? (
                <LyoTextField label="Cron" value={draft.cronExpression ?? ""} onChange={(event) => update({...draft, cronExpression: event.target.value})} />
            ) : null}
            <LyoStack direction="row" spacing={1}>
                <LyoTextField label="Year" value={String(year)} onChange={(event) => setYear(Number(event.target.value) || year)} />
                <LyoTextField label="Month" value={String(month)} onChange={(event) => setMonth(Number(event.target.value) || month)} />
            </LyoStack>
            <LyoCalendar calendar={calendar} />
        </LyoStack>
    );
}
