"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {asLyoQueryClient} from "../client/LyoQueryClient.js";
import {LyoDataGridFeatureFlags, createLyoColumn} from "../data-grid/types.js";
import {LyoDataGridProjected} from "../data-grid/LyoDataGrid.js";
import {LyoButton} from "../primitives/primitives.js";
import {LyoStack} from "../primitives/primitives.js";
import {useLyoSnackbar} from "../provider/LyoSnackbar.js";
import {clearCache} from "./cache.js";

export type CacheGridProps = {
    apiClient: AsyncApiClient;
    route?: string;
};

/** Projected cache rows plus clear. */
export function CacheGrid({apiClient, route = "Cache"}: CacheGridProps) {
    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);
    const snackbar = useLyoSnackbar();
    const [reloadToken, setReloadToken] = useState(0);
    return (
        <LyoStack spacing={1}>
            <LyoButton
                onClick={() =>
                    void clearCache(apiClient, route).then(() => {
                        snackbar.show("Cache cleared.");
                        setReloadToken((n) => n + 1);
                    })
                }
            >
                Clear
            </LyoButton>
            <LyoDataGridProjected<Record<string, unknown>>
                apiClient={query}
                gridKey="CacheGrid"
                route={route}
                reloadToken={reloadToken}
                columns={[
                    createLyoColumn({id: "type", field: "Type", header: "Type"}),
                    createLyoColumn({id: "name", field: "Name", header: "Name", quickSearch: true}),
                    createLyoColumn({id: "size", field: "SizeBytes", header: "Size"}),
                ]}
                keySelector={(row) => [row.Type ?? row.type, row.Name ?? row.name]}
                quickSearchProperties={["Name"]}
                features={LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.BulkMenu | LyoDataGridFeatureFlags.BulkDelete}
            />
        </LyoStack>
    );
}
