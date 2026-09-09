"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {ReportingRoutes, joinRoutePrefix} from "lyo-reporting-api-client";
import {filterProperty} from "lyo-query";
import {
    LyoButton,
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoMenuItem,
    asLyoQueryClient,
    useLyoSnackbar,
} from "lyo-web-components";
import {reportDefinitionColumns, reportGenerationColumns} from "./columns.js";
import {canDownloadGeneration, projectedField, projectedId, triggerBlobDownload} from "./reportColors.js";
import {ReportDefinitionView, ReportGenerationView, RunReportDialog} from "./dialogs.js";

type Row = Record<string, unknown>;

export function ReportDefinitionGrid({
    apiClient,
    reports,
    routePrefix,
    createdBy,
    onDesign,
}: {
    apiClient: AsyncApiClient;
    reports: AsyncReportingApiClient;
    routePrefix?: string;
    createdBy?: string;
    onDesign?: (id: string) => void;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [runFor, setRunFor] = useState<string | null>(null);
    const [editId, setEditId] = useState<string | null>(null);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="ReportDefinitionGrid"
                route={joinRoutePrefix(routePrefix, ReportingRoutes.definitions)}
                columns={reportDefinitionColumns()}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[
                    filterProperty("Name"),
                    filterProperty("IsActive", {type: "Bool"}),
                    filterProperty("DefaultFormat"),
                ]}
                quickSearchProperties={["Name", "Description"]}
                leftControls={
                    <LyoButton size="small" variant="outlined" onClick={() => snackbar.show("Select a row, then Run.", "info")}>
                        Run Report
                    </LyoButton>
                }
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setRunFor(projectedId(row))}>Run</LyoMenuItem>
                        {onDesign ? <LyoMenuItem onClick={() => onDesign(projectedId(row))}>Design</LyoMenuItem> : null}
                        <LyoMenuItem onClick={() => setEditId(projectedId(row))}>Edit</LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                const id = projectedId(row);
                                const active = Boolean(projectedField(row, "IsActive"));
                                await reports.definitions.patch(id, {properties: {IsActive: !active}});
                                snackbar.show(active ? "Deactivated." : "Activated.", "success");
                            }}
                        >
                            Toggle Active
                        </LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                await reports.definitions.deleteById(projectedId(row));
                                snackbar.show("Deleted.", "success");
                            }}
                        >
                            Delete
                        </LyoMenuItem>
                    </>
                )}
            />
            <RunReportDialog open={Boolean(runFor)} onClose={() => setRunFor(null)} reports={reports} definitionId={runFor} createdBy={createdBy} />
            <ReportDefinitionView open={Boolean(editId)} onClose={() => setEditId(null)} reports={reports} definitionId={editId} />
        </>
    );
}

export function ReportGenerationGrid({
    apiClient,
    reports,
    routePrefix,
    downloadFile,
    viewFileUrl,
}: {
    apiClient: AsyncApiClient;
    reports: AsyncReportingApiClient;
    routePrefix?: string;
    downloadFile?: (outputFileId: string, fileName?: string | null) => Promise<void>;
    viewFileUrl?: (outputFileId: string) => Promise<string | null>;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [detailId, setDetailId] = useState<string | null>(null);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="ReportGenerationGrid"
                route={joinRoutePrefix(routePrefix, ReportingRoutes.generations)}
                columns={reportGenerationColumns()}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[filterProperty("Status"), filterProperty("Format")]}
                quickSearchProperties={["ReportDefinition.Name", "OriginalFileName"]}
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setDetailId(projectedId(row))}>View</LyoMenuItem>
                        <LyoMenuItem
                            disabled={!canDownloadGeneration(row)}
                            onClick={async () => {
                                const outputId = String(projectedField(row, "OutputFileId") ?? "");
                                const name = projectedField(row, "OriginalFileName");
                                if (downloadFile && outputId) {
                                    await downloadFile(outputId, name == null ? null : String(name));
                                    return;
                                }
                                const file = await reports.generations.download(projectedId(row));
                                triggerBlobDownload(file.blob, file.fileName);
                            }}
                        >
                            Download
                        </LyoMenuItem>
                        <LyoMenuItem
                            disabled={!canDownloadGeneration(row)}
                            onClick={async () => {
                                const outputId = String(projectedField(row, "OutputFileId") ?? "");
                                if (viewFileUrl && outputId) {
                                    const url = await viewFileUrl(outputId);
                                    if (url) window.open(url, "_blank");
                                    return;
                                }
                                snackbar.show("No view URL callback configured.", "info");
                            }}
                        >
                            Open output
                        </LyoMenuItem>
                        <LyoMenuItem
                            onClick={async () => {
                                await reports.generations.deleteById(projectedId(row));
                                snackbar.show("Deleted.", "success");
                            }}
                        >
                            Delete
                        </LyoMenuItem>
                    </>
                )}
            />
            <ReportGenerationView open={Boolean(detailId)} onClose={() => setDetailId(null)} reports={reports} generationId={detailId} />
        </>
    );
}
