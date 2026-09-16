"use client";

import {useEffect, useState} from "react";
import {LyoAlert, LyoButton, LyoStack} from "lyo-web-components";
import type {AsyncAuthorizationApiClient, Grant} from "lyo-authorization-api-client";
import {GrantEditDialog} from "./GrantEditDialog.js";

export type GrantPanelProps = {
    client: AsyncAuthorizationApiClient;
    subject?: string;
    allowEdit?: boolean;
    grantedBy?: string;
};

export function GrantPanel({client, subject, allowEdit = false, grantedBy}: GrantPanelProps) {
    const [rows, setRows] = useState<Grant[]>([]);
    const [error, setError] = useState<string | null>(null);
    const [includeRevoked, setIncludeRevoked] = useState(false);
    const [includeExpired, setIncludeExpired] = useState(false);
    const [editing, setEditing] = useState(false);

    const load = async () => {
        const response = await client.list({subject, includeRevoked, includeExpired});
        if (!response.ok) {
            setError("Could not load grants.");
            setRows([]);
            return;
        }
        setError(null);
        setRows(response.data ?? []);
    };

    useEffect(() => {
        void load();
    }, [client, subject, includeRevoked, includeExpired]);

    const revoke = async (id: string) => {
        if (!window.confirm("Revoke this grant?")) return;
        const response = await client.revoke(id);
        if (!response.ok) {
            setError("Revoke failed.");
            return;
        }
        await load();
    };

    return (
        <div className="lyo-grant-panel">
            <LyoStack spacing={1}>
                {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
                <LyoStack direction="row" spacing={1}>
                    <label>
                        <input type="checkbox" checked={includeRevoked} onChange={(event) => setIncludeRevoked(event.target.checked)} /> Revoked
                    </label>
                    <label>
                        <input type="checkbox" checked={includeExpired} onChange={(event) => setIncludeExpired(event.target.checked)} /> Expired
                    </label>
                    {allowEdit ? <LyoButton size="small" onClick={() => setEditing(true)}>Add grant</LyoButton> : null}
                </LyoStack>
                {rows.length === 0 ? <LyoAlert severity="info">No grants.</LyoAlert> : null}
                {rows.map((row) => (
                    <LyoStack key={row.id} direction="row" spacing={1}>
                        <span>{row.action}</span>
                        <span>{row.resourceType}</span>
                        <span>{row.resourceId ?? "any"}</span>
                        <span>{row.tenantId ?? "any"}</span>
                        {allowEdit && !row.revokedAt ? (
                            <LyoButton size="small" onClick={() => void revoke(row.id)}>Revoke</LyoButton>
                        ) : null}
                    </LyoStack>
                ))}
                {allowEdit ? (
                    <GrantEditDialog
                        client={client}
                        open={editing}
                        subject={subject}
                        grantedBy={grantedBy}
                        onClose={(saved) => {
                            setEditing(false);
                            if (saved) void load();
                        }}
                    />
                ) : null}
            </LyoStack>
        </div>
    );
}
