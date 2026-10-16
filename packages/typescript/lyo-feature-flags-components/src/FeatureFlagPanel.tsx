"use client";

import {useEffect, useState} from "react";
import {LyoAlert, LyoStack} from "lyo-web-components";
import type {AsyncFeatureFlagApiClient, FeatureFlagState} from "lyo-feature-flags-api-client";

export type FeatureFlagPanelProps = {
    client: AsyncFeatureFlagApiClient;
    allowEdit?: boolean;
};

export function FeatureFlagPanel({client, allowEdit = false}: FeatureFlagPanelProps) {
    const [rows, setRows] = useState<FeatureFlagState[]>([]);
    const [error, setError] = useState<string | null>(null);

    const load = async () => {
        const response = await client.list();
        if (!response.ok || response.data == null) {
            setError("Could not load feature flags.");
            setRows([]);
            return;
        }
        setError(null);
        setRows(response.data);
    };

    useEffect(() => {
        void load();
    }, [client]);

    const toggle = async (name: string, enabled: boolean) => {
        const response = await client.setEnabled(name, enabled);
        if (!response.ok) {
            setError("Could not update the feature flag.");
            return;
        }
        await load();
    };

    return (
        <div className="lyo-feature-flag-panel">
            <LyoStack spacing={1}>
                {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
                {rows.length === 0 ? <LyoAlert severity="info">No flags.</LyoAlert> : null}
                {rows.map((row) => (
                    <LyoStack key={row.name} direction="row" spacing={1}>
                        <span>{row.name}</span>
                        <span>{row.mode}</span>
                        {allowEdit && (row.mode === "Percentage" || row.mode === "Rules") ? (
                            <span>Switching replaces this flag with on or off.</span>
                        ) : null}
                        {allowEdit ? (
                            <input
                                type="checkbox"
                                checked={row.mode === "On"}
                                aria-label={row.name}
                                onChange={(event) => void toggle(row.name, event.target.checked)}
                            />
                        ) : null}
                    </LyoStack>
                ))}
            </LyoStack>
        </div>
    );
}
