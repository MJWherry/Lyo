"use client";

import {useState} from "react";
import {LyoButton, LyoStack, LyoTextField} from "lyo-web-components";

/** One plan the host already loaded. The library does not read disk. */
export type AutomationPlanSummary = {
    name: string;
    fileName: string;
};

export type AutomationPlanLibraryProps = {
    plans: AutomationPlanSummary[];
    onCreate?: (name: string) => void;
    onOpen?: (fileName: string) => void;
};

/** Lists plans and asks the host to create or open one. It does not call HTTP or touch a directory. */
export function AutomationPlanLibrary({plans, onCreate, onOpen}: AutomationPlanLibraryProps) {
    const [name, setName] = useState("");

    return (
        <LyoStack spacing={1}>
            <LyoStack direction="row" spacing={1}>
                <LyoTextField label="New plan" value={name} onChange={(event) => setName(event.target.value)} />
                <LyoButton
                    onClick={() => {
                        const next = name.trim();
                        if (!next)
                            return;
                        onCreate?.(next);
                        setName("");
                    }}
                >
                    Create
                </LyoButton>
            </LyoStack>
            {plans.map((plan) => (
                <LyoButton key={plan.fileName} onClick={() => onOpen?.(plan.fileName)}>
                    {plan.name}
                </LyoButton>
            ))}
        </LyoStack>
    );
}
