"use client";

import {useState} from "react";
import Box from "@mui/material/Box";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import Paper from "@mui/material/Paper";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import {previewTemplate, suggestionsAtCaret} from "./contextPaths.js";

export type LyoFormatterTemplateEditorProps = {
    value: string;
    onChange: (value: string) => void;
    context?: unknown;
    readOnly?: boolean;
};

/** Template box. `{` at the caret lists context keys and nested paths. */
export function LyoFormatterTemplateEditor({value, onChange, context, readOnly}: LyoFormatterTemplateEditorProps) {
    const [caret, setCaret] = useState(value.length);
    const suggestions = suggestionsAtCaret(value, caret, context);
    return (
        <Box sx={{display: "flex", flexDirection: "column", gap: 1}}>
            <TextField
                size="small"
                fullWidth
                multiline
                minRows={4}
                label="Template"
                value={value}
                disabled={readOnly}
                onChange={(event) => onChange(event.target.value)}
                onSelect={(event) => setCaret((event.target as HTMLTextAreaElement).selectionStart ?? value.length)}
            />
            {suggestions.length > 0 ? (
                <Paper variant="outlined">
                    <List dense disablePadding>
                        {suggestions.map((path) => (
                            <ListItemButton key={path} onClick={() => onChange(`${value.slice(0, caret)}${path}}`)}>
                                <code>{path}</code>
                            </ListItemButton>
                        ))}
                    </List>
                </Paper>
            ) : null}
        </Box>
    );
}

export type LyoFormatterPreviewProps = {
    template: string;
    context?: unknown;
    /** Host formatter. Defaults to local `{path}` substitution. */
    format?: (template: string, context: unknown) => string;
};

/** Renders the template through the host callback. */
export function LyoFormatterPreview({template, context, format}: LyoFormatterPreviewProps) {
    const text = format ? format(template, context) : previewTemplate(template, context);
    return (
        <Typography variant="body2" sx={{whiteSpace: "pre-wrap"}}>
            {text || "—"}
        </Typography>
    );
}
