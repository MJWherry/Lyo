"use client";

import {useState} from "react";
import {LyoAlert, LyoButton, LyoStack, LyoTextField} from "lyo-web-components";
import {grantFormToCheck, type AsyncAuthorizationApiClient, type GrantFormInput} from "lyo-authorization-api-client";

export type GrantCheckFormProps = {
    client: AsyncAuthorizationApiClient;
    subject?: string;
};

export function GrantCheckForm({client, subject}: GrantCheckFormProps) {
    const [form, setForm] = useState<GrantFormInput>({subject: subject ?? "", action: "", resourceType: "", resourceId: "", tenantId: ""});
    const [result, setResult] = useState<"allow" | "deny" | null>(null);
    const [error, setError] = useState<string | null>(null);
    const set = (key: keyof GrantFormInput) => (event: {target: {value: string}}) => setForm((current) => ({...current, [key]: event.target.value}));

    const check = async () => {
        setError(null);
        setResult(null);
        let body;
        try {
            body = grantFormToCheck({...form, subject: subject || form.subject});
        } catch (ex) {
            setError(ex instanceof Error ? ex.message : "Invalid check");
            return;
        }
        const response = await client.check(body);
        if (!response.ok || response.data == null) {
            setError("Check failed.");
            return;
        }
        setResult(response.data.allowed ? "allow" : "deny");
    };

    return (
        <LyoStack spacing={1}>
            <span>Check access</span>
            {error ? <LyoAlert severity="error">{error}</LyoAlert> : null}
            {result ? <LyoAlert severity={result === "allow" ? "success" : "info"}>{result === "allow" ? "Allow" : "Deny"}</LyoAlert> : null}
            <LyoTextField label="Subject" value={subject || form.subject} disabled={Boolean(subject)} onChange={set("subject")} />
            <LyoTextField label="Action" value={form.action} onChange={set("action")} />
            <LyoTextField label="Resource type" value={form.resourceType} onChange={set("resourceType")} />
            <LyoTextField label="Resource id" helperText="Blank matches only a wildcard grant" value={form.resourceId ?? ""} onChange={set("resourceId")} />
            <LyoTextField label="Tenant" helperText="Blank matches only a wildcard grant" value={form.tenantId ?? ""} onChange={set("tenantId")} />
            <LyoButton onClick={() => void check()}>Check</LyoButton>
        </LyoStack>
    );
}
