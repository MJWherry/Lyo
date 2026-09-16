import type { ReportDefinitionParameterRes } from "lyo-reporting-api-client";
import { chipOf, entriesFrom, LyoChipHues, type LyoChipSpec, type LyoParameterEntry } from "lyo-web-components";
import { formatHue } from "./reportPreview.js";

type ParameterSource = {
    parameters?: readonly ReportDefinitionParameterRes[] | null;
    Parameters?: readonly ReportDefinitionParameterRes[] | null;
};

/** Run-dialog entries for a definition. An omitted include yields an empty list. */
export function runParameterEntries(definition: ParameterSource | null | undefined): LyoParameterEntry[] {
    const params = definition?.parameters ?? definition?.Parameters;
    if (!params) return [];
    return entriesFrom(
        params.map((item) => ({
            key: item.key,
            type: item.type ?? "System.String",
            value: item.value,
            required: item.required,
            description: item.description,
            options: item.options,
            allowedValues: item.allowedValues,
            defaultKind: item.defaultKind === "Expression" ? "Expression" : "Literal",
            defaultTemplate: item.defaultTemplate,
        }))
    );
}

export function formatChip(format: string | null | undefined): LyoChipSpec {
    const hueName = formatHue(format);
    return hueName ? chipOf(format, { hue: LyoChipHues[hueName] }) : chipOf(format);
}
