import { registerLyoType, type LyoTypeInfo } from "lyo-web-components";

export const FORMATTER_TYPE_NAME = "Lyo.Formatter.Template";

/** Puts the SmartFormat template type in the shared catalog. Idempotent. */
export function registerFormatterType(): LyoTypeInfo {
    return registerLyoType({
        shortName: "formatter",
        fullName: FORMATTER_TYPE_NAME,
        description: "SmartFormat template (DateTime, Guid, and other values via {tokens})",
        category: "Text",
        editorKind: "Formatter",
        defaultJson: '""',
        isClr: false,
        aliases: ["Format", "SmartFormat", "template", "Formatter"],
    });
}

registerFormatterType();
