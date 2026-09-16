"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {
    HOST_PERSISTS_COPY,
    createAsyncImportApiClient,
    emptyPreview,
    issueListText,
    showPreview,
    tryAddField,
    type AsyncImportApiClient,
    type ImportFormat,
    type ImportJsonMode,
    type ImportPreviewState,
    type ImportResponse,
} from "lyo-import-api-client";
import {LyoAlert, LyoButton, LyoMenuItem, LyoSelect, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";

const FORMATS: ImportFormat[] = ["Json", "Csv", "Xlsx", "Yaml", "Xml", "Toml"];
const JSON_MODES: ImportJsonMode[] = ["Array", "Wrapped", "Ndjson"];

export type ImportWorkbenchProps = {
    apiClient: AsyncApiClient;
    imports?: AsyncImportApiClient;
    onImported?: (response: ImportResponse) => void;
};

export function ImportWorkbench({apiClient, imports, onImported}: ImportWorkbenchProps) {
    const client = useMemo(() => imports ?? createAsyncImportApiClient(apiClient), [apiClient, imports]);
    const snackbar = useLyoSnackbar();
    const [format, setFormat] = useState<ImportFormat>("Json");
    const [jsonMode, setJsonMode] = useState<ImportJsonMode>("Array");
    const [rowsPath, setRowsPath] = useState("");
    const [content, setContent] = useState("");
    const [source, setSource] = useState("");
    const [target, setTarget] = useState("");
    const [required, setRequired] = useState(false);
    const [state, setState] = useState<ImportPreviewState>(emptyPreview());

    const addField = () => {
        const next = tryAddField(state, {source, target, required});
        setState(next);
        if (!next.blockedReason) {
            setSource("");
            setTarget("");
            setRequired(false);
        }
    };

    const upload = async () => {
        const response = await client.postImport({
            format,
            ...(format === "Json" ? {jsonMode, ...(rowsPath ? {rowsPath} : {})} : {}),
            content,
            fields: state.fields,
        });
        const body = response.data;
        let rows: unknown[] = [];
        if (body?.rowsJson) {
            const parsed = JSON.parse(body.rowsJson) as unknown;
            rows = Array.isArray(parsed) ? parsed : [];
        }
        const shown = showPreview(rows, body?.issues ?? [], (row) => JSON.stringify(row));
        setState({...state, ...shown});
        if (body) onImported?.(body);
        snackbar.show(HOST_PERSISTS_COPY);
    };

    return (
        <div className="lyo-import-workbench">
            <LyoStack spacing={1}>
                <h2>Import</h2>
                <p>{HOST_PERSISTS_COPY}</p>
                <LyoSelect label="Format" value={format} onChange={(event) => setFormat(String(event.target.value) as ImportFormat)}>
                    {FORMATS.map((item) => (
                        <LyoMenuItem key={item} value={item}>
                            {item}
                        </LyoMenuItem>
                    ))}
                </LyoSelect>
                {format === "Json" ? (
                    <LyoSelect label="JSON shape" value={jsonMode} onChange={(event) => setJsonMode(String(event.target.value) as ImportJsonMode)}>
                        {JSON_MODES.map((item) => (
                            <LyoMenuItem key={item} value={item}>
                                {item}
                            </LyoMenuItem>
                        ))}
                    </LyoSelect>
                ) : null}
                <LyoTextField label="Rows path" value={rowsPath} onChange={(event) => setRowsPath(event.target.value)} />
                <LyoTextField label="Content" value={content} onChange={(event) => setContent(event.target.value)} multiline />
                <LyoTextField label="Source" value={source} onChange={(event) => setSource(event.target.value)} />
                <LyoTextField label="Target property" value={target} onChange={(event) => setTarget(event.target.value)} />
                <LyoButton onClick={addField}>Add mapping</LyoButton>
                {state.fields.map((field) => (
                    <span key={`${field.source}-${field.target}`}>
                        {field.source} → {field.target}
                        {field.required ? " (required)" : ""}
                    </span>
                ))}
                {state.blockedReason ? <LyoAlert severity="warning">{state.blockedReason}</LyoAlert> : null}
                <LyoButton onClick={() => void upload()}>Preview</LyoButton>
                {state.preview.map((row) => (
                    <span key={row}>{row}</span>
                ))}
                <pre>{issueListText(state.issues)}</pre>
            </LyoStack>
        </div>
    );
}
