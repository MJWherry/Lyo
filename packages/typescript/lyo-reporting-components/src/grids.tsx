"use client";

import {useEffect, useMemo, useState, type ReactNode} from "react";
import EditIcon from "@mui/icons-material/Edit";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import VisibilityIcon from "@mui/icons-material/Visibility";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {ReportingRoutes, joinRoutePrefix} from "lyo-reporting-api-client";
import {filterProperty} from "lyo-query";
import {
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoIconButton,
    LyoMenuItem,
    LyoToolbarButton,
    LyoTooltip,
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
    definitionId,
    onGenerated,
}: {
    apiClient: AsyncApiClient;
    reports: AsyncReportingApiClient;
    routePrefix?: string;
    createdBy?: string;
    onDesign?: (id: string) => void;
    definitionId?: string | null;
    onGenerated?: (generationId: string) => void;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [runFor, setRunFor] = useState<string | null>(null);
    const [createOpen, setCreateOpen] = useState(false);
    const [editId, setEditId] = useState<string | null>(definitionId ?? null);
    const toggleActive = async (row: Row) => {
        const id = projectedId(row);
        const active = Boolean(projectedField(row, "IsActive"));
        await reports.definitions.patchById(id, {properties: {IsActive: !active}});
        snackbar.show(active ? "Deactivated." : "Activated.", "success");
    };

    useEffect(() => {
        if (definitionId) setEditId(definitionId);
    }, [definitionId]);

    return (
        <>
            <LyoDataGridProjected<Row>
                apiClient={query}
                gridKey="ReportDefinitionGrid"
                route={joinRoutePrefix(routePrefix, ReportingRoutes.definitions)}
                columns={reportDefinitionColumns({onToggleActive: (row) => void toggleActive(row)})}
                keySelector={(row) => [projectedId(row)]}
                features={LyoDataGridFeatureFlags.All}
                filterPropertyDefinitions={[
                    filterProperty("Name"),
                    filterProperty("IsActive", {type: "Bool"}),
                    filterProperty("DefaultFormat"),
                ]}
                quickSearchProperties={["Name", "Description"]}
                leftControls={
                    <LyoToolbarButton icon={<PlayArrowIcon fontSize="small" />} onClick={() => setCreateOpen(true)}>
                        Run Report
                    </LyoToolbarButton>
                }
                rowQuickActions={(row) => (
                    <>
                        <QuickIcon title="Run" onClick={() => setRunFor(projectedId(row))}>
                            <PlayArrowIcon fontSize="small" />
                        </QuickIcon>
                        <QuickIcon title="Edit" onClick={() => setEditId(projectedId(row))}>
                            <EditIcon fontSize="small" />
                        </QuickIcon>
                    </>
                )}
                rowMenu={(row) => (
                    <>
                        <LyoMenuItem onClick={() => setRunFor(projectedId(row))}>Run</LyoMenuItem>
                        {onDesign ? <LyoMenuItem onClick={() => onDesign(projectedId(row))}>Design</LyoMenuItem> : null}
                        <LyoMenuItem onClick={() => setEditId(projectedId(row))}>Edit</LyoMenuItem>
                        <LyoMenuItem onClick={() => void toggleActive(row)}>Toggle Active</LyoMenuItem>
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
            <RunReportDialog
                open={createOpen || Boolean(runFor)}
                onClose={() => {
                    setCreateOpen(false);
                    setRunFor(null);
                }}
                reports={reports}
                definitionId={runFor}
                createdBy={createdBy}
                onGenerated={onGenerated}
            />
            <ReportDefinitionView
                open={Boolean(editId)}
                onClose={() => setEditId(null)}
                reports={reports}
                definitionId={editId}
                onDesign={onDesign}
                apiClient={apiClient}
            />
        </>
    );
}

export function ReportGenerationGrid({
    apiClient,
    reports,
    routePrefix,
    downloadFile,
    viewFileUrl,
    generationId,
}: {
    apiClient: AsyncApiClient;
    reports: AsyncReportingApiClient;
    routePrefix?: string;
    downloadFile?: (outputFileId: string, fileName?: string | null) => Promise<void>;
    viewFileUrl?: (outputFileId: string) => Promise<string | null>;
    generationId?: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const [detailId, setDetailId] = useState<string | null>(generationId ?? null);

    useEffect(() => {
        if (generationId) setDetailId(generationId);
    }, [generationId]);

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
                rowQuickActions={(row) => (
                    <QuickIcon title="View" onClick={() => setDetailId(projectedId(row))}>
                        <VisibilityIcon fontSize="small" />
                    </QuickIcon>
                )}
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
            <ReportGenerationView
                open={Boolean(detailId)}
                onClose={() => setDetailId(null)}
                reports={reports}
                generationId={detailId}
                downloadFile={downloadFile}
                viewFileUrl={viewFileUrl}
            />
        </>
    );
}

function QuickIcon({title, onClick, children}: {title: string; onClick: () => void; children: ReactNode}) {
    return (
        <LyoTooltip title={title}>
            <LyoIconButton
                size="small"
                aria-label={title}
                onClick={(event) => {
                    event.stopPropagation();
                    onClick();
                }}
            >
                {children}
            </LyoIconButton>
        </LyoTooltip>
    );
}
