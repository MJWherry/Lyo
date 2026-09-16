"use client";

import {useState} from "react";
import type {AsyncAutomationApiClient} from "lyo-automation-api-client";
import {LyoButton, LyoDataGridFeatureFlags, LyoDataGridProjected, LyoStack, LyoTabSet, createLyoColumn, projectedId, type LyoQueryClient} from "lyo-web-components";
import {AutomationPlanEditor} from "./AutomationPlanEditor.js";
import {AutomationRunPanel} from "./AutomationRunPanel.js";
import type {AutomationPlan} from "./plan.js";
import {runnablePlan} from "./runModel.js";

export type AutomationPlanHostProps = {
    client: AsyncAutomationApiClient;
    queryClient: LyoQueryClient;
};

type PlanRow = {Id?: string; Name?: string; StepCount?: number; UpdatedUtc?: string};

/** Selection bar, then Manage, Editor, and Runner. This is the only module that imports both. */
export function AutomationPlanHost({client, queryClient}: AutomationPlanHostProps) {
    const [plan, setPlan] = useState<AutomationPlan | null>(null);
    const [source, setSource] = useState("none");
    const [error, setError] = useState<string | null>(null);
    const [tab, setTab] = useState("manage");

    async function openRow(row: PlanRow, run: boolean) {
        const id = projectedId(row as Record<string, unknown>);
        if (!id)
            return;
        const loaded = await client.getPlanById(id) as AutomationPlan;
        setPlan(loaded);
        setSource("database");
        setTab(run ? "runner" : "editor");
    }

    async function loadFile(file: File) {
        const parsed = JSON.parse(await file.text()) as AutomationPlan;
        setPlan(runnablePlan(parsed));
        setSource("file");
        setTab("editor");
    }

    async function save() {
        if (!plan?.name) {
            setError("Name the plan before saving.");
            return;
        }
        setError(null);
        await client.createPlan(plan.name);
        const saved = await client.savePlan(plan.name, plan) as AutomationPlan;
        setPlan(saved);
        setSource("database");
    }

    return (
        <LyoStack spacing={2}>
            {error ? <p>{error}</p> : null}
            <LyoStack direction="row" spacing={1}>
                <span>{plan?.name ?? "No plan selected"}</span>
                <span>{source}</span>
                <input type="file" accept="application/json,.json" onChange={(event) => {
                    const file = event.target.files?.[0];
                    if (file)
                        void loadFile(file);
                }} />
                <LyoButton disabled={!plan} onClick={() => void save()}>Save</LyoButton>
            </LyoStack>
            <LyoTabSet
                key={tab}
                queryParameter={null}
                defaultTab={tab}
                tabs={[
                    {
                        id: "manage",
                        text: "Manage",
                        content: (
                            <LyoDataGridProjected<PlanRow>
                                apiClient={queryClient}
                                gridKey="automation-plans"
                                route="Automation/Plans"
                                features={LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable}
                                columns={[
                                    createLyoColumn<PlanRow>({id: "name", field: "Name", header: "Name"}),
                                    createLyoColumn<PlanRow>({id: "steps", field: "StepCount", header: "Steps"}),
                                    createLyoColumn<PlanRow>({id: "updated", field: "UpdatedUtc", header: "Updated"}),
                                ]}
                                keySelector={(row) => [projectedId(row as Record<string, unknown>)]}
                                rowQuickActions={(row) => (
                                    <>
                                        <LyoButton onClick={() => void openRow(row, false)}>Open</LyoButton>
                                        <LyoButton onClick={() => void openRow(row, true)}>Run</LyoButton>
                                    </>
                                )}
                            />
                        ),
                    },
                    {
                        id: "editor",
                        text: "Editor",
                        content: plan ? <AutomationPlanEditor value={plan} onChange={setPlan} /> : <p>Open a plan from the grid or load a file.</p>,
                    },
                    {
                        id: "runner",
                        text: "Runner",
                        content: plan ? <AutomationRunPanel client={client} plan={plan} /> : <p>Open a plan from the grid or load a file.</p>,
                    },
                ]}
            />
        </LyoStack>
    );
}
