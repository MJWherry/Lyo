"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {createAsyncReportingApiClient} from "lyo-reporting-api-client";
import {LyoTab, LyoTabs} from "lyo-web-components";
import {ReportDefinitionGrid, ReportGenerationGrid} from "./grids.js";
import {ReportDesignWorkbench} from "./ReportDesign.js";

export type ReportManagementProps = {
    apiClient: AsyncApiClient;
    reports?: AsyncReportingApiClient;
    routePrefix?: string;
    createdBy?: string;
    downloadFile?: (outputFileId: string, fileName?: string | null) => Promise<void>;
    viewFileUrl?: (outputFileId: string) => Promise<string | null>;
    onDesign?: (definitionId: string) => void;
    initialTab?: "definitions" | "generations";
    definitionId?: string | null;
    generationId?: string | null;
};

export function ReportManagement({
    apiClient,
    reports: reportsProp,
    routePrefix,
    createdBy = "ui",
    downloadFile,
    viewFileUrl,
    onDesign,
    initialTab,
    definitionId,
    generationId,
}: ReportManagementProps) {
    const reports = useMemo(
        () => reportsProp ?? createAsyncReportingApiClient(apiClient, {routePrefix}),
        [apiClient, reportsProp, routePrefix]
    );
    const [tab, setTab] = useState(generationId ? 1 : definitionId ? 0 : initialTab === "generations" ? 1 : 0);
    const [openGenerationId, setOpenGenerationId] = useState<string | null>(generationId ?? null);
    const [designId, setDesignId] = useState<string | null>(definitionId ?? null);

    useEffect(() => {
        if (generationId) {
            setTab(1);
            setOpenGenerationId(generationId);
        } else if (definitionId) setTab(0);
    }, [definitionId, generationId]);

    return (
        <div className="lyo-reporting-management">
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Definitions" />
                <LyoTab label="Generations" />
                <LyoTab label="Design" />
            </LyoTabs>
            <div style={{marginTop: 16}}>
                {tab === 0 ? (
                    <ReportDefinitionGrid
                        apiClient={apiClient}
                        reports={reports}
                        routePrefix={routePrefix}
                        createdBy={createdBy}
                        onDesign={(id) => {
                            setDesignId(id);
                            setTab(2);
                            onDesign?.(id);
                        }}
                        definitionId={definitionId}
                        onGenerated={(id) => {
                            setOpenGenerationId(id);
                            setTab(1);
                        }}
                    />
                ) : tab === 1 ? (
                    <ReportGenerationGrid
                        apiClient={apiClient}
                        reports={reports}
                        routePrefix={routePrefix}
                        downloadFile={downloadFile}
                        viewFileUrl={viewFileUrl}
                        generationId={openGenerationId}
                    />
                ) : (
                    <ReportDesignWorkbench apiClient={apiClient} reports={reports} routePrefix={routePrefix} definitionId={designId} />
                )}
            </div>
        </div>
    );
}
