"use client";

import {useState} from "react";
import Dialog from "@mui/material/Dialog";
import DialogActions from "@mui/material/DialogActions";
import DialogContent from "@mui/material/DialogContent";
import DialogTitle from "@mui/material/DialogTitle";
import {LyoButton, LyoStack, LyoTextField} from "lyo-web-components";
import {grantFormToWrite, type AsyncAuthorizationApiClient, type GrantFormInput} from "lyo-authorization-api-client";

export type GrantEditDialogProps = {
    client: AsyncAuthorizationApiClient;
    open: boolean;
    subject?: string;
    grantedBy?: string;
    onClose: (saved: boolean) => void;
};

export function GrantEditDialog({client, open, subject, grantedBy, onClose}: GrantEditDialogProps) {
    const [form, setForm] = useState<GrantFormInput>({subject: subject ?? "", action: "", resourceType: "note", resourceId: "", tenantId: "", expiresAt: ""});
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);
    const set = (key: keyof GrantFormInput) => (event: {target: {value: string}}) => setForm((current) => ({...current, [key]: event.target.value}));

    const save = async () => {
        setError(null);
        let write;
        try {
            write = grantFormToWrite({...form, subject: subject || form.subject, grantedBy});
        } catch (ex) {
            setError(ex instanceof Error ? ex.message : "Invalid grant");
            return;
        }
        setBusy(true);
        try {
            const response = await client.save(write);
            if (!response.ok) {
                setError(response.status === 409 ? "That grant already exists." : "Save failed.");
                return;
            }
            onClose(true);
        } finally {
            setBusy(false);
        }
    };

    return (
        <Dialog open={open} onClose={() => onClose(false)}>
            <DialogTitle>Add grant</DialogTitle>
            <DialogContent>
                <LyoStack spacing={1}>
                    {error ? <span>{error}</span> : null}
                    <LyoTextField label="Subject" value={subject || form.subject} disabled={Boolean(subject)} onChange={set("subject")} />
                    <LyoTextField label="Action" value={form.action} onChange={set("action")} />
                    <LyoTextField label="Resource type" value={form.resourceType} onChange={set("resourceType")} />
                    <LyoTextField label="Resource id" helperText="Blank covers every id" value={form.resourceId ?? ""} onChange={set("resourceId")} />
                    <LyoTextField label="Tenant" helperText="Blank covers every tenant" value={form.tenantId ?? ""} onChange={set("tenantId")} />
                    <LyoTextField label="Expires" helperText="UTC" type="datetime-local" value={form.expiresAt ?? ""} onChange={set("expiresAt")} />
                </LyoStack>
            </DialogContent>
            <DialogActions>
                <LyoButton onClick={() => onClose(false)}>Cancel</LyoButton>
                <LyoButton disabled={busy} onClick={() => void save()}>Save</LyoButton>
            </DialogActions>
        </Dialog>
    );
}
