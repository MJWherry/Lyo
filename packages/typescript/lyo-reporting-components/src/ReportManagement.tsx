"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncReportingApiClient} from "lyo-reporting-api-client";
import {createAsyncReportingApiClient} from "lyo-reporting-api-client";
import {LyoTab, LyoTabs} from "lyo-web-components";
import {ReportDefinitionGrid, ReportGenerationGrid} from "./grids.js";

export type ReportManagementProps = {
    apiClient: AsyncApiClient;
    reports?: AsyncReportingApiClient;
    routePrefix?: string;
    createdBy?: string;
    downloadFile?: (outputFileId: string, fileName?: string | null) => Promise<void>;
    viewFileUrl?: (outputFileId: string) => Promise<string | null>;
    onDesign?: (definitionId: string) => void;
    initialTab?: "definitions" | "generations";
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
}: ReportManagementProps) {
    const reports = useMemo(
        () => reportsProp ?? createAsyncReportingApiClient(apiClient, {routePrefix}),
        [apiClient, reportsProp, routePrefix]
    );
    const [tab, setTab] = useState(initialTab === "generations" ? 1 : 0);

    return (
        <div className="lyo-reporting-management">
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Definitions" />
                <LyoTab label="Generations" />
            </LyoTabs>
            <div style={{marginTop: 16}}>
                {tab === 0 ? (
                    <ReportDefinitionGrid
                        apiClient={apiClient}
                        reports={reports}
                        routePrefix={routePrefix}
                        createdBy={createdBy}
                        onDesign={onDesign}
                    />
                ) : (
                    <ReportGenerationGrid
                        apiClient={apiClient}
                        reports={reports}
                        routePrefix={routePrefix}
                        downloadFile={downloadFile}
                        viewFileUrl={viewFileUrl}
                    />
                )}
            </div>
        </div>
    );
}
