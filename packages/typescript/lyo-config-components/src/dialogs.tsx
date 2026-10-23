"use client";

import {useState} from "react";
import type {AsyncConfigApiClient, ConfigBindingRecord, ConfigDefinitionRecord} from "lyo-config-api-client";
import {LyoDialog, LyoForm, LyoFormGrid, LyoFormInput, LyoTextField, LyoTypeValueInput, useLyoSnackbar} from "lyo-web-components";

export function DefinitionDialog({
    open,
    config,
    initial,
    onClose,
    onSaved,
}: {
    open: boolean;
    config: AsyncConfigApiClient;
    initial?: ConfigDefinitionRecord;
    onClose: () => void;
    onSaved: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [draft, setDraft] = useState<ConfigDefinitionRecord>(
        initial ?? {subjectEntityType: "App", key: "", forValueType: "System.String", isEncrypted: false, isRequired: false}
    );

    const save = async () => {
        await config.putDefinition({
            ...draft,
            defaultValue: draft.defaultValue ?? null,
            isEncrypted: draft.isEncrypted === true,
        });
        snackbar.show("Definition saved.", "success");
        onSaved();
        onClose();
    };

    return (
        <LyoDialog open={open} onClose={onClose} title={draft.id ? "Edit definition" : "New definition"} onSave={() => void save()} size="Medium">
            <LyoForm model={draft} showActions={false}>
                <LyoFormGrid>
                    <LyoFormInput propertyName="subjectEntityType" label="Entity type" value={draft.subjectEntityType} allowEditing onChange={(value) => setDraft({...draft, subjectEntityType: String(value ?? "")})} />
                    <LyoFormInput propertyName="key" label="Key" value={draft.key} allowEditing onChange={(value) => setDraft({...draft, key: String(value ?? "")})} />
                    <LyoFormInput propertyName="forValueType" label="Value type" value={draft.forValueType} allowEditing onChange={(value) => setDraft({...draft, forValueType: String(value ?? "")})} />
                    <LyoFormInput propertyName="description" label="Description" value={draft.description ?? ""} allowEditing onChange={(value) => setDraft({...draft, description: String(value ?? "")})} />
                    <LyoFormInput propertyName="isRequired" label="Required" kind="bool" value={draft.isRequired === true} allowEditing onChange={(value) => setDraft({...draft, isRequired: value === true})} />
                    <LyoFormInput propertyName="isEncrypted" label="Encrypted" kind="bool" value={draft.isEncrypted === true} allowEditing onChange={(value) => setDraft({...draft, isEncrypted: value === true})} />
                    <LyoTypeValueInput
                        typeName={draft.forValueType}
                        json={draft.defaultValue?.json}
                        isEncrypted={draft.isEncrypted === true}
                        allowUnset
                        label="Default"
                        onJsonChange={(json) =>
                            setDraft({
                                ...draft,
                                defaultValue: json == null ? null : {typeName: draft.forValueType, json},
                            })
                        }
                    />
                </LyoFormGrid>
            </LyoForm>
        </LyoDialog>
    );
}

export function BindingDialog({
    open,
    config,
    initial,
    encrypted,
    onClose,
    onSaved,
}: {
    open: boolean;
    config: AsyncConfigApiClient;
    initial: ConfigBindingRecord;
    encrypted?: boolean;
    onClose: () => void;
    onSaved: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [valueJson, setValueJson] = useState(initial.value.json);
    const typeName = initial.value.typeName;

    const save = async () => {
        await config.putBinding({...initial, value: {typeName, json: valueJson}});
        snackbar.show("Binding saved.", "success");
        onSaved();
        onClose();
    };

    return (
        <LyoDialog open={open} onClose={onClose} title="Binding" onSave={() => void save()} size="Medium">
            <LyoTextField label="Key" value={initial.key} disabled fullWidth />
            <LyoTypeValueInput typeName={typeName} json={valueJson} isEncrypted={encrypted === true} label="Value" onJsonChange={(json) => setValueJson(json ?? "null")} />
        </LyoDialog>
    );
}
