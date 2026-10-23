"use client";

import {useEffect, useState} from "react";
import type {AsyncAutomationApiClient, AutomationRunStepRes} from "lyo-automation-api-client";
import {LyoAlert, LyoButton, LyoStack} from "lyo-web-components";
import type {AutomationPlan} from "./plan.js";
import {overallContext, stepTitle} from "./runModel.js";

export type AutomationRunPanelProps = {
    client: AsyncAutomationApiClient;
    plan: AutomationPlan;
};

/** Posts one plan and shows each finished step. It does not list plans. */
export function AutomationRunPanel({client, plan}: AutomationRunPanelProps) {
    const [status, setStatus] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);
    const [steps, setSteps] = useState<AutomationRunStepRes[]>([]);
    const [selected, setSelected] = useState(0);
    const [image, setImage] = useState<string | null>(null);
    const [runId, setRunId] = useState<string | null>(null);
    const step = steps[selected];
    const bag = overallContext({runId: runId ?? "", status: "running", steps});

    useEffect(() => {
        if (!step?.screenshotFileId || !runId) {
            setImage(null);
            return;
        }
        let url: string | null = null;
        let cancelled = false;
        void client.stepScreenshot(runId, step.index).then((blob) => {
            if (cancelled || !blob)
                return;
            url = URL.createObjectURL(blob);
            setImage(url);
        });
        return () => {
            cancelled = true;
            if (url)
                URL.revokeObjectURL(url);
        };
    }, [client, runId, step?.index, step?.screenshotFileId]);

    async function run() {
        setBusy(true);
        setError(null);
        setSteps([]);
        setImage(null);
        setStatus("queued");
        try {
            const accepted = await client.start({plan, headless: true});
            setRunId(accepted.runId);
            for (let attempt = 0; attempt < 120; attempt++) {
                const current = await client.get(accepted.runId);
                setStatus(current.status);
                setSteps(current.steps ?? []);
                if (current.status === "completed" || current.status === "failed" || current.status === "cancelled") {
                    setError(current.error ?? null);
                    break;
                }
                await new Promise((resolve) => setTimeout(resolve, 400));
            }
        } catch (caught) {
            setStatus("failed");
            setError(caught instanceof Error ? caught.message : "Run failed.");
        } finally {
            setBusy(false);
        }
    }

    return (
        <LyoStack spacing={1}>
            <LyoButton disabled={busy} onClick={() => void run()}>Run</LyoButton>
            {status ? <LyoAlert severity={error ? "warning" : "info"}>{status}{error ? ` ${error}` : ""}</LyoAlert> : null}
            <div style={{display: "grid", gridTemplateColumns: "1fr 2fr", gap: 16}}>
                <LyoStack spacing={1}>
                    {steps.map((item, index) => (
                        <LyoButton key={item.index} onClick={() => setSelected(index)}>
                            {index + 1}. {stepTitle(item)} — {item.outcome}
                        </LyoButton>
                    ))}
                </LyoStack>
                <LyoStack spacing={1}>
                    {step ? (
                        <>
                            <p>{step.outcome} · {step.durationMs} ms</p>
                            {step.error ? <LyoAlert severity="error">{step.error}</LyoAlert> : null}
                            {image ? <img src={image} alt="Step screenshot" style={{maxWidth: "100%"}} /> : null}
                            <pre>{JSON.stringify(step.context ?? {}, null, 2)}</pre>
                        </>
                    ) : <p>Select a step to see its screenshot and context.</p>}
                    {bag ? <pre>{JSON.stringify(bag, null, 2)}</pre> : null}
                </LyoStack>
            </div>
        </LyoStack>
    );
}
