"use client";

import {useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {
    ConfigQueryRoutes,
    createAsyncConfigApiClient,
    type AsyncConfigApiClient,
    type ConfigBindingRevisionRecord,
    type ConfigDefinitionRecord,
    type ConfigDefinitionRevisionRecord,
    type ResolvedConfigRecord,
} from "lyo-config-api-client";
import {
    LyoButton,
    LyoChip,
    LyoDataGridFeatureFlags,
    LyoDataGridProjected,
    LyoMenuItem,
    LyoStack,
    LyoTabSet,
    LyoTextField,
    asLyoQueryClient,
    useLyoSnackbar,
} from "lyo-web-components";
import {definitionColumns} from "./columns.js";
import {sourceChip} from "./configUi.js";
import {BindingDialog, DefinitionDialog} from "./dialogs.js";

export type ConfigManagementProps = {
    apiClient: AsyncApiClient;
    config?: AsyncConfigApiClient;
    initialSubjectEntityType?: string;
    initialSubjectEntityId?: string;
};

type HistoryKind = {kind: "definition"; id: string} | {kind: "binding"; id: string} | null;

export function ConfigManagement({
    apiClient,
    config,
    initialSubjectEntityType = "App",
    initialSubjectEntityId = "",
}: ConfigManagementProps) {
    const client = useMemo(() => config ?? createAsyncConfigApiClient(apiClient), [apiClient, config]);
    const snackbar = useLyoSnackbar();
    const [entityType, setEntityType] = useState(initialSubjectEntityType);
    const [entityId, setEntityId] = useState(initialSubjectEntityId);
    const [applied, setApplied] = useState({type: initialSubjectEntityType, id: initialSubjectEntityId});
    const [reloadToken, setReloadToken] = useState(0);
    const [editor, setEditor] = useState<ConfigDefinitionRecord | null | undefined>(undefined);
    const [resolved, setResolved] = useState<ResolvedConfigRecord | null>(null);
    const [definitionHistory, setDefinitionHistory] = useState<ConfigDefinitionRevisionRecord[]>([]);
    const [valueHistory, setValueHistory] = useState<ConfigBindingRevisionRecord[]>([]);
    const [historyTarget, setHistoryTarget] = useState<HistoryKind>(null);
    const [binding, setBinding] = useState<Parameters<typeof BindingDialog>[0]["initial"] | null>(null);

    const query = useMemo(() => asLyoQueryClient(apiClient), [apiClient]);

    const loadResolved = async () => {
        if (!applied.id) {
            snackbar.show("Entity id is required.", "warning");
            return;
        }
        const response = await client.resolved(applied.type, applied.id);
        setResolved(response.data ?? null);
    };

    const openDefinitionHistory = async (id: string) => {
        setHistoryTarget({kind: "definition", id});
        const response = await client.definitionRevisions(id);
        setDefinitionHistory(response.data ?? []);
    };

    const openValueHistory = async (id: string) => {
        setHistoryTarget({kind: "binding", id});
        const response = await client.bindingRevisions(id);
        setValueHistory(response.data ?? []);
    };

    const deleteDefinition = async (id: string) => {
        await client.deleteDefinition(id);
        snackbar.show("Definition deleted.", "success");
        setReloadToken((n) => n + 1);
    };

    return (
        <div className="lyo-config-management">
            <LyoStack spacing={2}>
                <LyoStack direction="row" spacing={1}>
                    <LyoTextField label="Entity type" value={entityType} onChange={(event) => setEntityType(event.target.value)} />
                    <LyoTextField label="Entity id" value={entityId} onChange={(event) => setEntityId(event.target.value)} />
                    <LyoButton onClick={() => setApplied({type: entityType, id: entityId})}>Apply</LyoButton>
                </LyoStack>
                <LyoTabSet
                    persistKey="config-management"
                    tabs={[
                        {
                            id: "definitions",
                            text: "Definitions",
                            content: (
                                <LyoStack spacing={1}>
                                    <LyoButton onClick={() => setEditor({subjectEntityType: applied.type, key: "", forValueType: "System.String"})}>New definition</LyoButton>
                                    <LyoDataGridProjected<Record<string, unknown>>
                                        apiClient={query}
                                        gridKey="ConfigDefinitions"
                                        route={ConfigQueryRoutes.definition}
                                        columns={definitionColumns()}
                                        keySelector={(row) => [row.Id ?? row.id]}
                                        reloadToken={reloadToken}
                                        quickSearchProperties={["Key"]}
                                        features={LyoDataGridFeatureFlags.Filterable | LyoDataGridFeatureFlags.Searchable | LyoDataGridFeatureFlags.BulkMenu}
                                        beforeQuery={(req) => {
                                            const entity = {
                                                $type: "condition",
                                                Field: "SubjectEntityType",
                                                Comparison: "Equals",
                                                Value: applied.type,
                                            };
                                            const existing = req.whereClause;
                                            return {
                                                ...req,
                                                whereClause: existing
                                                    ? {$type: "group", Operator: "And", Children: [entity, existing]}
                                                    : entity,
                                            };
                                        }}
                                        rowMenu={(row) => (
                                            <>
                                                <LyoMenuItem onClick={() => void openDefinitionHistory(String(row.Id ?? row.id))}>History</LyoMenuItem>
                                                <LyoMenuItem onClick={() => void deleteDefinition(String(row.Id ?? row.id))}>Delete</LyoMenuItem>
                                            </>
                                        )}
                                    />
                                </LyoStack>
                            ),
                        },
                        {
                            id: "resolved",
                            text: "Resolved",
                            content: (
                                <LyoStack spacing={1}>
                                    <LyoButton onClick={() => void loadResolved()}>Load</LyoButton>
                                    {(resolved?.items ?? []).map((item) => (
                                        <LyoStack key={item.definition.key} direction="row" spacing={1}>
                                            <span>{item.definition.key}</span>
                                            <LyoChip label={sourceChip(item)} />
                                            {item.binding?.id ? (
                                                <LyoButton size="small" onClick={() => void openValueHistory(item.binding!.id!)}>
                                                    History
                                                </LyoButton>
                                            ) : null}
                                            <LyoButton
                                                size="small"
                                                onClick={() =>
                                                    item.definition.id
                                                        ? setBinding({
                                                              definitionId: item.definition.id,
                                                              key: item.definition.key,
                                                              subjectEntityType: applied.type,
                                                              subjectEntityId: applied.id,
                                                              value: item.binding?.value ?? item.definition.defaultValue ?? {typeName: item.definition.forValueType, json: "null"},
                                                              ...(item.binding?.id ? {id: item.binding.id} : {}),
                                                          })
                                                        : undefined
                                                }
                                            >
                                                Edit
                                            </LyoButton>
                                        </LyoStack>
                                    ))}
                                </LyoStack>
                            ),
                        },
                        {
                            id: "definition-history",
                            text: "Definition history",
                            content: (
                                <LyoStack spacing={1}>
                                    {definitionHistory.map((row) => (
                                        <LyoStack key={`${row.definitionId}-${row.revision}`} direction="row" spacing={1}>
                                            <span>Revision {row.revision}</span>
                                            <LyoButton
                                                size="small"
                                                onClick={() => {
                                                    if (!historyTarget || historyTarget.kind !== "definition" || row.revision === undefined) return;
                                                    void client.revertDefinition(historyTarget.id, row.revision).then(() => snackbar.show("Definition reverted.", "success"));
                                                }}
                                            >
                                                Revert
                                            </LyoButton>
                                        </LyoStack>
                                    ))}
                                </LyoStack>
                            ),
                        },
                        {
                            id: "value-history",
                            text: "Value history",
                            content: (
                                <LyoStack spacing={1}>
                                    {valueHistory.map((row) => (
                                        <LyoStack key={`${row.bindingId}-${row.revision}`} direction="row" spacing={1}>
                                            <span>Revision {row.revision}</span>
                                            <LyoButton
                                                size="small"
                                                onClick={() => {
                                                    if (!historyTarget || historyTarget.kind !== "binding" || row.revision === undefined) return;
                                                    void client.revertBinding(historyTarget.id, row.revision).then(() => snackbar.show("Binding reverted.", "success"));
                                                }}
                                            >
                                                Revert
                                            </LyoButton>
                                        </LyoStack>
                                    ))}
                                </LyoStack>
                            ),
                        },
                    ]}
                />
            </LyoStack>
            {editor !== undefined ? (
                <DefinitionDialog
                    open
                    config={client}
                    {...(editor ? {initial: editor} : {})}
                    onClose={() => setEditor(undefined)}
                    onSaved={() => setReloadToken((n) => n + 1)}
                />
            ) : null}
            {binding ? (
                <BindingDialog
                    open
                    config={client}
                    initial={binding}
                    encrypted={resolved?.items.find((item) => item.definition.key === binding.key)?.definition.isEncrypted}
                    onClose={() => setBinding(null)}
                    onSaved={() => void loadResolved()}
                />
            ) : null}
        </div>
    );
}
