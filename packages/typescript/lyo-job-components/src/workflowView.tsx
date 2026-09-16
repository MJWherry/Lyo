"use client";

import {useEffect, useState} from "react";
import type {AsyncJobApiClient, JobWorkflowRes, JobWorkflowStepRes} from "lyo-job-api-client";
import {emptyConcreteQuery} from "lyo-job-api-client";
import {LyoButton, LyoDialog, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";

export function JobWorkflowView({
    open,
    onClose,
    jobs,
    workflowId,
}: {
    open: boolean;
    onClose: () => void;
    jobs: AsyncJobApiClient;
    workflowId: string | null;
}) {
    const snackbar = useLyoSnackbar();
    const [workflow, setWorkflow] = useState<JobWorkflowRes | null>(null);
    const [idempotencyKey, setIdempotencyKey] = useState("");

    useEffect(() => {
        if (!open || !workflowId) return;
        void jobs.workflows
            .queryConcrete({...emptyConcreteQuery(1), Keys: [[workflowId]], Include: ["JobWorkflowSteps"]})
            .then((res) => setWorkflow(res.data?.items?.[0] ?? null));
    }, [jobs, open, workflowId]);

    const steps = [...(workflow?.steps ?? [])].sort((a, b) => a.stepOrder - b.stepOrder);

    const start = async () => {
        if (!workflowId) return;
        await jobs.workflows.start(workflowId, idempotencyKey.trim() ? {idempotencyKey: idempotencyKey.trim()} : undefined);
        snackbar.show("Workflow started.", "success");
    };

    return (
        <LyoDialog open={open} onClose={onClose} title={workflow?.name ?? "Workflow"} onSave={start} saveText="Run now">
            <LyoStack spacing={1}>
                {steps.map((step) => (
                    <StepLine key={step.id} step={step} />
                ))}
                <LyoTextField label="Idempotency key" value={idempotencyKey} onChange={(e) => setIdempotencyKey(e.target.value)} />
            </LyoStack>
        </LyoDialog>
    );
}

function StepLine({step}: {step: JobWorkflowStepRes}) {
    return (
        <span>
            {step.stepOrder}. {step.stepName}
            {step.dependsOnStepIds ? ` ← ${step.dependsOnStepIds}` : ""}
        </span>
    );
}

export function JobWorkerInstanceView({
    open,
    onClose,
    instance,
}: {
    open: boolean;
    onClose: () => void;
    instance: Record<string, unknown> | null;
}) {
    const metadata = instance?.Metadata ?? instance?.metadata;
    return (
        <LyoDialog open={open} onClose={onClose} title="Worker">
            <LyoStack spacing={1}>
                <span>{String(instance?.MachineName ?? instance?.machineName ?? "")}</span>
                <pre style={{whiteSpace: "pre-wrap", fontSize: 12}}>{JSON.stringify(metadata ?? {}, null, 2)}</pre>
            </LyoStack>
        </LyoDialog>
    );
}

