import {existsSync} from "node:fs";
import path from "node:path";

/** The studio project, walking up from `start` until the Lyo repo root. */
export function findStudioProject(start = process.cwd()): string {
    let dir = start;
    for (let depth = 0; depth < 8; depth++) {
        const candidate = path.join(dir, "Lyo.Net/Apps/Web/Automation/Lyo.Web.Automation.Studio/Lyo.Web.Automation.Studio.csproj");
        if (existsSync(candidate))
            return candidate;
        const parent = path.dirname(dir);
        if (parent === dir)
            break;
        dir = parent;
    }
    throw new Error("Lyo.Web.Automation.Studio.csproj was not found. Run this from the Lyo repo.");
}
