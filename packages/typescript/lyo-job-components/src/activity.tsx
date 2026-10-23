"use client";

import {useEffect, useState} from "react";
import type {AsyncJobApiClient, JobDefinitionLatestRunsRes, JobDefinitionStatsRes} from "lyo-job-api-client";
import {LyoStack} from "lyo-web-components";

export function JobActivity({jobs, definitionId}: {jobs: AsyncJobApiClient; definitionId: string}) {
    const [stats, setStats] = useState<JobDefinitionStatsRes | null>(null);
    const [latest, setLatest] = useState<JobDefinitionLatestRunsRes | null>(null);
    const [next, setNext] = useState<string[]>([]);

    useEffect(() => {
        let cancelled = false;
        void Promise.all([
            jobs.definitions.stats(definitionId, 7),
            jobs.definitions.latestRuns([definitionId]),
            jobs.definitions.nextRuns(definitionId, 5),
        ]).then(([statsRes, latestRes, nextRes]) => {
            if (cancelled) return;
            setStats(statsRes.data ?? null);
            setLatest(latestRes.data?.[0] ?? null);
            setNext(nextRes.data ?? []);
        });
        return () => {
            cancelled = true;
        };
    }, [definitionId, jobs]);

    return (
        <LyoStack spacing={1} sx={{mt: 2}}>
            <span>Runs {stats?.totalRuns ?? 0} · success {stats?.successCount ?? 0} · failure {stats?.failureCount ?? 0}</span>
            <span>Queued {stats?.queuedCount ?? 0} · running {stats?.runningCount ?? 0}</span>
            <span>Last run {latest?.lastRun?.id ?? "—"}</span>
            <span>Upcoming {next.join(", ") || "—"}</span>
        </LyoStack>
    );
}
