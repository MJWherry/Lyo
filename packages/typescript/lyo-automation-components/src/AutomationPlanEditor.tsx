"use client";

import {useState} from "react";
import {JsonEditor, LyoButton, LyoMenuItem, LyoSelect, LyoStack, LyoTextField} from "lyo-web-components";
import {AutomationPlanStepFields} from "./AutomationPlanStepFields.js";
import {createStep, hasForm, insertNavigate, moveStep, removeStep, stepCatalog, stepLabel, type AutomationPlan, type AutomationStep} from "./plan.js";

export type AutomationPlanEditorProps = {
    value: AutomationPlan;
    onChange?: (value: AutomationPlan) => void;
};

/** Controlled plan editor. It does not call HTTP. Steps without a form stay in the document and can be changed in the JSON pane. */
export function AutomationPlanEditor({value, onChange}: AutomationPlanEditorProps) {
    const [addType, setAddType] = useState("navigate");
    const [selected, setSelected] = useState(0);
    const [json, setJson] = useState(() => JSON.stringify(value, null, 2));
    const [jsonError, setJsonError] = useState<string | null>(null);
    const plan = value;
    const step = plan.steps[selected];

    const update = (next: AutomationPlan) => {
        onChange?.(next);
        setJson(JSON.stringify(next, null, 2));
        setJsonError(null);
    };

    const replace = (nextStep: AutomationStep) => {
        const steps = plan.steps.map((item, index) => (index === selected ? nextStep : item));
        update({...plan, steps});
    };

    return (
        <LyoStack spacing={1}>
            <LyoTextField label="Plan name" value={plan.name ?? ""} onChange={(event) => update({...plan, name: event.target.value})} />
            <LyoStack direction="row" spacing={1}>
                <LyoSelect label="Step" value={addType} onChange={(event) => setAddType(String(event.target.value))}>
                    {stepCatalog.map((item) => (
                        <LyoMenuItem key={item.type} value={item.type}>
                            {item.label}
                        </LyoMenuItem>
                    ))}
                </LyoSelect>
                <LyoButton
                    onClick={() => {
                        const next = addType === "navigate" ? insertNavigate(plan, "https://example.com/") : {...plan, steps: [...plan.steps, createStep(addType)]};
                        update(next);
                        setSelected(next.steps.length - 1);
                    }}
                >
                    Add
                </LyoButton>
            </LyoStack>
            {plan.steps.map((item, index) => (
                <LyoButton key={`${item.type}-${index}`} onClick={() => setSelected(index)}>
                    {index + 1}. {stepLabel(item.type)}
                </LyoButton>
            ))}
            {step && hasForm(step) ? <AutomationPlanStepFields step={step} onChange={replace} /> : step ? <p>This step stays in the plan. Edit it in the JSON pane.</p> : null}
            {step ? (
                <LyoStack direction="row" spacing={1}>
                    <LyoButton disabled={selected <= 0} onClick={() => { update(moveStep(plan, selected, selected - 1)); setSelected(selected - 1); }}>
                        Up
                    </LyoButton>
                    <LyoButton
                        disabled={selected >= plan.steps.length - 1}
                        onClick={() => { update(moveStep(plan, selected, selected + 1)); setSelected(selected + 1); }}
                    >
                        Down
                    </LyoButton>
                    <LyoButton
                        onClick={() => {
                            const next = removeStep(plan, selected);
                            update(next);
                            setSelected(Math.max(0, Math.min(selected, next.steps.length - 1)));
                        }}
                    >
                        Remove
                    </LyoButton>
                </LyoStack>
            ) : null}
            <JsonEditor value={json} defaultMode="tree" height={360} onChange={setJson} />
            {jsonError ? <p>{jsonError}</p> : null}
            <LyoButton
                onClick={() => {
                    try {
                        const parsed = JSON.parse(json) as AutomationPlan;
                        if (!Array.isArray(parsed.steps))
                            throw new Error("Plan JSON needs a steps array.");
                        onChange?.(parsed);
                        setJsonError(null);
                    } catch (error) {
                        setJsonError(error instanceof Error ? error.message : "Invalid plan JSON.");
                    }
                }}
            >
                Apply JSON
            </LyoButton>
        </LyoStack>
    );
}
