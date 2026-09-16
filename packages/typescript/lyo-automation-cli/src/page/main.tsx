import {useEffect, useState} from "react";
import {createRoot} from "react-dom/client";
import {ThemeProvider} from "@mui/material/styles";
import {createLyoTheme} from "lyo-web-components";
import {AutomationPlanEditor, AutomationPlanLibrary, type AutomationPlan, type AutomationPlanSummary} from "lyo-automation-components";

function App() {
    const [plans, setPlans] = useState<AutomationPlanSummary[]>([]);
    const [fileName, setFileName] = useState<string | null>(null);
    const [plan, setPlan] = useState<AutomationPlan | null>(null);

    async function refresh() {
        const response = await fetch("/api/plans");
        const body = (await response.json()) as {plans: AutomationPlanSummary[]};
        setPlans(body.plans);
    }

    useEffect(() => {
        void refresh();
    }, []);

    return (
        <main style={{maxWidth: 960, margin: "24px auto", padding: "0 16px"}}>
            <AutomationPlanLibrary
                plans={plans}
                onCreate={async (name) => {
                    await fetch("/api/plans", {method: "POST", headers: {"Content-Type": "application/json"}, body: JSON.stringify({name})});
                    await refresh();
                }}
                onOpen={async (next) => {
                    const response = await fetch(`/api/plans/${encodeURIComponent(next)}`);
                    setPlan((await response.json()) as AutomationPlan);
                    setFileName(next);
                }}
            />
            {plan && fileName ? (
                <AutomationPlanEditor
                    key={fileName}
                    value={plan}
                    onChange={(next) => {
                        setPlan(next);
                        void fetch(`/api/plans/${encodeURIComponent(fileName)}`, {
                            method: "PUT",
                            headers: {"Content-Type": "application/json"},
                            body: JSON.stringify(next),
                        });
                    }}
                />
            ) : null}
        </main>
    );
}

createRoot(document.getElementById("root")!).render(
    <ThemeProvider theme={createLyoTheme("light")}>
        <App />
    </ThemeProvider>,
);
