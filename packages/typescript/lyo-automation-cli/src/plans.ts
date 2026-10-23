import {mkdirSync, readdirSync, readFileSync, writeFileSync} from "node:fs";
import path from "node:path";
import {createEmptyPlan, planFileName, type AutomationPlan} from "lyo-automation-components/plan";

export type PlanFileSummary = {
    name: string;
    fileName: string;
};

/** Directory for plan files. A missing value is `./plans`. */
export function plansDirectory(dir?: string): string {
    return path.resolve(dir && dir.length > 0 ? dir : "plans");
}

function jsonFile(fileName: string): string {
    if (fileName !== path.basename(fileName) || !fileName.toLowerCase().endsWith(".json"))
        throw new Error("Plan file must be one .json name.");
    return fileName;
}

/** Writes an empty named plan. The file name is one `.json` segment inside `dir`. */
export function createPlanFile(dir: string, name: string): string {
    const fileName = planFileName(name);
    mkdirSync(dir, {recursive: true});
    const target = path.join(dir, fileName);
    writeFileSync(target, `${JSON.stringify(createEmptyPlan(name), null, 2)}\n`);
    return fileName;
}

/** Plan files in `dir`. A missing directory is an empty list. */
export function listPlanFiles(dir: string): PlanFileSummary[] {
    let names: string[] = [];
    try {
        names = readdirSync(dir);
    } catch {
        return [];
    }
    return names.filter((fileName) => fileName.toLowerCase().endsWith(".json")).map((fileName) => {
        let name = fileName.replace(/\.json$/i, "");
        try {
            const plan = JSON.parse(readFileSync(path.join(dir, fileName), "utf8")) as AutomationPlan;
            if (typeof plan.name === "string" && plan.name.length > 0)
                name = plan.name;
        } catch {
            /* A file that is not a plan still shows up under its file name. */
        }
        return {name, fileName};
    });
}

/** The plan document for `name`, which may omit `.json`. */
export function showPlanFile(dir: string, name: string): string {
    return readFileSync(path.join(dir, planFileName(name)), "utf8");
}

/** Replaces a plan file. Invalid JSON throws and leaves the file as it was. */
export function writePlanFile(dir: string, fileName: string, json: string): void {
    const safe = jsonFile(fileName);
    const plan = JSON.parse(json) as AutomationPlan;
    if (!plan || !Array.isArray(plan.steps))
        throw new Error("Plan JSON needs a steps array.");
    writeFileSync(path.join(dir, safe), json.endsWith("\n") ? json : `${json}\n`);
}
