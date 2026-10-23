"use client";

import {useState} from "react";
import type {AsyncJobApiClient, JobBlackoutWindowRes, JobDefinitionRes, JobScheduleRes} from "lyo-job-api-client";
import {LyoButton, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";
import {blackoutKind, linkBlackout, unlinkBlackout} from "./jobEditors.js";

export function JobSchedulePanel({jobs, definition}: {jobs: AsyncJobApiClient; definition: JobDefinitionRes}) {
    const snackbar = useLyoSnackbar();
    const [selected, setSelected] = useState<JobScheduleRes | null>(definition.jobSchedules?.[0] ?? null);

    const toggle = async (schedule: JobScheduleRes) => {
        await jobs.schedules.patch({keys: [[schedule.id]], properties: {Enabled: !schedule.enabled}});
        snackbar.show(schedule.enabled ? "Schedule disabled." : "Schedule enabled.", "success");
    };

    const runNow = async (schedule: JobScheduleRes) => {
        await jobs.runs.create({
            jobDefinitionId: definition.id,
            jobScheduleId: schedule.id,
            createdBy: "ui",
            allowTriggers: true,
        });
        snackbar.show("Schedule run queued.", "success");
    };

    return (
        <LyoStack spacing={1} sx={{mt: 2}}>
            {(definition.jobSchedules ?? []).map((schedule) => (
                <LyoStack key={schedule.id} direction="row" spacing={1} alignItems="center">
                    <LyoButton size="small" onClick={() => setSelected(schedule)}>
                        {schedule.cronExpression || schedule.description || schedule.id}
                    </LyoButton>
                    <LyoButton size="small" onClick={() => void toggle(schedule)}>
                        {schedule.enabled ? "Disable" : "Enable"}
                    </LyoButton>
                    <LyoButton size="small" onClick={() => void runNow(schedule)}>
                        Run now
                    </LyoButton>
                </LyoStack>
            ))}
            {selected ? <ScheduleDetail jobs={jobs} schedule={selected} /> : null}
        </LyoStack>
    );
}

function ScheduleDetail({jobs, schedule}: {jobs: AsyncJobApiClient; schedule: JobScheduleRes}) {
    const snackbar = useLyoSnackbar();
    const windows = schedule.jobBlackoutCalendar?.blackoutWindows ?? [];
    const [calendarId, setCalendarId] = useState(schedule.jobBlackoutCalendarId ?? "");

    const saveLink = async (id: string | null) => {
        const next = id ? linkBlackout(schedule, id) : unlinkBlackout(schedule);
        await jobs.schedules.patch({
            keys: [[schedule.id]],
            properties: {JobBlackoutCalendarId: next.jobBlackoutCalendarId},
        });
        snackbar.show(id ? "Blackout linked." : "Blackout unlinked.", "success");
    };

    return (
        <LyoStack spacing={1}>
            <span>Cron {schedule.cronExpression ?? "—"} · zone {schedule.timeZoneId ?? "UTC"} · misfire {schedule.misfirePolicy ?? "—"}</span>
            <LyoTextField label="Calendar id" value={calendarId} onChange={(e) => setCalendarId(e.target.value)} />
            <LyoStack direction="row" spacing={1}>
                <LyoButton size="small" onClick={() => void saveLink(calendarId.trim() || null)}>
                    Link calendar
                </LyoButton>
                <LyoButton size="small" onClick={() => void saveLink(null)}>
                    Unlink
                </LyoButton>
            </LyoStack>
            <JobBlackoutEditor jobs={jobs} windows={windows} calendarId={schedule.jobBlackoutCalendarId} />
        </LyoStack>
    );
}

export function JobBlackoutEditor({
    jobs,
    windows,
    calendarId,
}: {
    jobs: AsyncJobApiClient;
    windows: JobBlackoutWindowRes[];
    calendarId?: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const [name, setName] = useState("");
    const [holidaySlug, setHolidaySlug] = useState("");
    const [startDateUtc, setStartDateUtc] = useState("");

    const add = async () => {
        if (!calendarId) return;
        await jobs.blackoutWindows.create({
            jobBlackoutCalendarId: calendarId,
            name: name || "Window",
            holidaySlug: holidaySlug || null,
            startDateUtc: startDateUtc || null,
            enabled: true,
        });
        snackbar.show("Blackout window saved.", "success");
    };

    return (
        <LyoStack spacing={1}>
            {windows.map((window) => (
                <span key={window.id}>
                    {window.name} · {blackoutKind(window)}
                </span>
            ))}
            <LyoTextField label="Window name" value={name} onChange={(e) => setName(e.target.value)} />
            <LyoTextField label="Holiday slug" value={holidaySlug} onChange={(e) => setHolidaySlug(e.target.value)} />
            <LyoTextField label="Start" value={startDateUtc} onChange={(e) => setStartDateUtc(e.target.value)} />
            <LyoButton size="small" onClick={() => void add()} disabled={!calendarId}>
                Add window
            </LyoButton>
        </LyoStack>
    );
}
