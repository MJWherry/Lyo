import type {ResolvedConfigItemRecord} from "lyo-config-api-client";

export type ConfigValueSource = "Binding" | "Default" | "Missing";

/** Binding wins, then the definition default, otherwise the value is missing. */
export function resolveSource(item: Pick<ResolvedConfigItemRecord, "binding" | "definition">): ConfigValueSource {
    if (item.binding != null) return "Binding";
    if (item.definition.defaultValue != null) return "Default";
    return "Missing";
}

/** Short label for a resolved-value source chip. */
export function sourceLabel(source: ConfigValueSource): string {
    switch (source) {
        case "Binding":
            return "Overridden";
        case "Default":
            return "Using default";
        case "Missing":
            return "Missing";
    }
}

export function sourceChip(item: Pick<ResolvedConfigItemRecord, "binding" | "definition">): string {
    return sourceLabel(resolveSource(item));
}
