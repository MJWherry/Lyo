"use client";

import { useMemo, useState } from "react";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import type { LyoValueEditorProps } from "lyo-web-components";
import { contextPaths, previewTemplate, suggestionsAtCaret } from "./contextPaths.js";
import { registerFormatterType } from "./register.js";

registerFormatterType();

function templateText(json: string | null | undefined): string {
    if (!json || json === "null") return "";
    try {
        const parsed = JSON.parse(json) as unknown;
        return typeof parsed === "string" ? parsed : json;
    } catch {
        return json;
    }
}

/** Template field for `Lyo.Formatter.Template`: `{` suggestions, a token list, and a sample-context preview. */
export function LyoFormatterValueEditor({ json, onJsonChange, label, readOnly, required, context }: LyoValueEditorProps) {
    const text = templateText(json);
    const [caret, setCaret] = useState(text.length);
    const [showTokens, setShowTokens] = useState(false);
    const [showPreview, setShowPreview] = useState(false);
    const paths = useMemo(() => contextPaths(context), [context]);
    const suggestions = suggestionsAtCaret(text, caret, context);

    const write = (next: string) => onJsonChange(next.trim() ? JSON.stringify(next) : null);

    const insert = (path: string) => {
        const before = text.slice(0, caret);
        const open = before.lastIndexOf("{");
        if (open < 0) {
            write(`${text}{${path}}`);
            return;
        }
        const after = text.slice(caret);
        const close = after.indexOf("}");
        const tail = close >= 0 ? after.slice(close + 1) : after;
        write(`${text.slice(0, open)}{${path}}${tail}`);
        setCaret(open + path.length + 2);
    };

    return (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
            <TextField
                size="small"
                fullWidth
                multiline
                minRows={3}
                label={label}
                value={text}
                required={required}
                disabled={readOnly}
                helperText="SmartFormat template. Unresolved {tokens} stay until run."
                onChange={(event) => write(event.target.value)}
                onSelect={(event) => setCaret((event.target as HTMLTextAreaElement).selectionStart ?? text.length)}
            />
            {suggestions.length > 0 ? (
                <Paper variant="outlined" sx={{ maxHeight: 180, overflow: "auto" }}>
                    <List dense disablePadding>
                        {suggestions.map((path) => (
                            <ListItemButton key={path} disabled={readOnly} onClick={() => insert(path)}>
                                <code>{path}</code>
                            </ListItemButton>
                        ))}
                    </List>
                </Paper>
            ) : null}
            <Box sx={{ display: "flex", gap: 1 }}>
                <Button size="small" onClick={() => setShowPreview((open) => !open)}>
                    {showPreview ? "Hide preview" : "Preview"}
                </Button>
                <Button size="small" onClick={() => setShowTokens((open) => !open)}>
                    {`Available tokens (${paths.length})`}
                </Button>
            </Box>
            {showPreview ? (
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                    {previewTemplate(text, context) || "—"}
                </Typography>
            ) : null}
            {showTokens ? (
                <Paper variant="outlined" sx={{ p: 1, maxHeight: 200, overflow: "auto" }}>
                    <Typography variant="caption" color="text.secondary">
                        Click a token to append it. Values are examples from the current context.
                    </Typography>
                    <List dense disablePadding>
                        {paths.map((path) => (
                            <ListItemButton key={path} disabled={readOnly} onClick={() => write(`${text}{${path}}`)}>
                                <code>{`{${path}}`}</code>
                            </ListItemButton>
                        ))}
                    </List>
                </Paper>
            ) : null}
        </Box>
    );
}
