"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncJobApiClient, JobWorkflowStepReq} from "lyo-job-api-client";
import {createAsyncJobApiClient} from "lyo-job-api-client";
import {LyoButton, LyoStack, LyoTab, LyoTabs, LyoTextField, WhereClauseBuilder, clientStore, useLyoSnackbar} from "lyo-web-components";
import type {WhereClause} from "lyo-query";
import {defaultGroup} from "lyo-query";
import {
    layoutSteps,
    linksFromSteps,
    loadDraft,
    saveDraft,
    selectStep,
    type WorkflowDraft,
    type WorkflowStepDraft,
} from "./workflowGraph.js";
import {JobWorkflowRunDetail, JobWorkflowRunGrid} from "./workflowRuns.js";

const STORAGE_KEY = "job-workflow-builder";

export function JobWorkflowBuilderWorkbench({
    apiClient,
    jobs: jobsProp,
    routePrefix,
}: {
    apiClient: AsyncApiClient;
    jobs?: AsyncJobApiClient;
    routePrefix?: string;
}) {
    const jobs = useMemo(() => jobsProp ?? createAsyncJobApiClient(apiClient, {routePrefix}), [apiClient, jobsProp, routePrefix]);
    const snackbar = useLyoSnackbar();
    const [tab, setTab] = useState(0);
    const [draft, setDraft] = useState<WorkflowDraft>({steps: [], links: [], selectedStepId: null});
    const [json, setJson] = useState("");
    const [openRunId, setOpenRunId] = useState<string | null>(null);

    useEffect(() => {
        const saved = clientStore.get<string>(STORAGE_KEY);
        if (saved) setDraft(loadDraft(saved));
    }, []);

    useEffect(() => {
        const handle = window.setTimeout(() => clientStore.set(STORAGE_KEY, saveDraft(draft)), 450);
        return () => window.clearTimeout(handle);
    }, [draft]);

    const nodes = layoutSteps(draft.steps);
    const selected = selectStep(nodes, draft.selectedStepId);
    const selectedStep = draft.steps.find((step) => step.id === draft.selectedStepId) ?? null;

    const addStep = () => {
        const id = globalThis.crypto?.randomUUID?.() ?? `step-${draft.steps.length + 1}`;
        const step: WorkflowStepDraft = {id, stepName: `Step ${draft.steps.length + 1}`, dependsOn: [], failurePolicy: "stop"};
        setDraft({...draft, steps: [...draft.steps, step], links: linksFromSteps([...draft.steps, step]), selectedStepId: id});
    };

    const updateSelected = (patch: Partial<WorkflowStepDraft>) => {
        const steps = draft.steps.map((step) => (step.id === draft.selectedStepId ? {...step, ...patch} : step));
        setDraft({...draft, steps, links: linksFromSteps(steps)});
    };

    const saveWorkflow = async () => {
        const steps: JobWorkflowStepReq[] = draft.steps.map((step, index) => ({
            jobDefinitionId: step.jobDefinitionId ?? "00000000-0000-0000-0000-000000000000",
            stepName: step.stepName,
            stepOrder: index,
            dependsOnStepIds: step.dependsOn.join(","),
            failurePolicy: step.failurePolicy === "continue" ? "continue" : "stop",
            parametersJson: step.parametersJson,
            enabled: true,
        }));
        await jobs.workflows.create({name: "Workflow", enabled: true, createSteps: steps});
        snackbar.show("Workflow saved.", "success");
    };

    return (
        <LyoStack spacing={2}>
            <LyoTabs value={tab} onChange={(_, value) => setTab(Number(value))}>
                <LyoTab label="Visual" />
                <LyoTab label="Request JSON" />
                <LyoTab label="Runs" />
            </LyoTabs>
            {tab === 0 ? (
                <LyoStack spacing={2}>
                    <div style={{position: "relative", minHeight: 240}}>
                        {nodes.map((node) => (
                            <button
                                key={node.id}
                                type="button"
                                onClick={() => setDraft({...draft, selectedStepId: node.id})}
                                style={{
                                    position: "absolute",
                                    left: node.x,
                                    top: node.y,
                                    width: 200,
                                    height: 64,
                                    border: node.id === selected?.id ? "2px solid currentColor" : "1px solid currentColor",
                                }}
                            >
                                {node.name}
                            </button>
                        ))}
                    </div>
                    <LyoButton onClick={addStep}>Add step</LyoButton>
                    {selectedStep ? (
                        <StepEditor step={selectedStep} steps={draft.steps} onChange={updateSelected} />
                    ) : null}
                    <LyoButton onClick={() => void saveWorkflow()}>Save</LyoButton>
                </LyoStack>
            ) : null}
            {tab === 1 ? (
                <LyoStack spacing={1}>
                    <LyoTextField
                        multiline
                        minRows={8}
                        value={json || saveDraft(draft)}
                        onChange={(e) => setJson(e.target.value)}
                    />
                    <LyoButton
                        onClick={() => {
                            setDraft(loadDraft(json || saveDraft(draft)));
                            snackbar.show("Draft loaded.", "success");
                        }}
                    >
                        Apply JSON
                    </LyoButton>
                </LyoStack>
            ) : null}
            {tab === 2 ? (
                <>
                    <JobWorkflowRunGrid apiClient={apiClient} jobs={jobs} routePrefix={routePrefix} onOpen={setOpenRunId} />
                    <JobWorkflowRunDetail open={Boolean(openRunId)} onClose={() => setOpenRunId(null)} jobs={jobs} runId={openRunId} />
                </>
            ) : null}
        </LyoStack>
    );
}

function StepEditor({
    step,
    steps,
    onChange,
}: {
    step: WorkflowStepDraft;
    steps: WorkflowStepDraft[];
    onChange: (patch: Partial<WorkflowStepDraft>) => void;
}) {
    const [condition, setCondition] = useState<WhereClause>(defaultGroup("State"));
    return (
        <LyoStack spacing={1}>
            <LyoTextField label="Name" value={step.stepName} onChange={(e) => onChange({stepName: e.target.value})} />
            <LyoTextField
                label="Depends on"
                value={step.dependsOn.join(",")}
                onChange={(e) => onChange({dependsOn: e.target.value.split(",").map((item) => item.trim()).filter(Boolean)})}
                helperText={steps
                    .filter((item) => item.id !== step.id)
                    .map((item) => item.stepName)
                    .join(", ")}
            />
            <LyoTextField label="Failure policy" value={step.failurePolicy ?? "stop"} onChange={(e) => onChange({failurePolicy: e.target.value})} />
            <LyoTextField
                label="Parameter overrides"
                value={step.parametersJson ?? ""}
                onChange={(e) => onChange({parametersJson: e.target.value})}
            />
            <WhereClauseBuilder value={condition} onChange={setCondition} />
        </LyoStack>
    );
}
