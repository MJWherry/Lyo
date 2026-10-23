"use client";

import {useState} from "react";
import type {WhereClause} from "lyo-query";
import {defaultGroup} from "lyo-query";
import type {
    StoryAction,
    StoryChoice,
    StoryEffect,
    StoryEffectKind,
    StoryNode,
    StoryPresentationKind,
    StoryReaction,
    StoryScene,
    StoryTrigger,
    StoryVariable,
    StoryVariableType,
    StoryWhere,
} from "lyo-story-api-client";
import {LyoButton, LyoMenuItem, LyoPaper, LyoStack, LyoSwitch, LyoTextField, WhereClauseBuilder} from "lyo-web-components";

const EFFECT_KINDS: StoryEffectKind[] = ["set", "unset", "increment", "gotoNode", "endPlaythrough", "invokeAction"];
const PRESENTATIONS: StoryPresentationKind[] = ["none", "text", "cutscene"];
const VARIABLE_TYPES: StoryVariableType[] = ["boolean", "integer", "string"];
const TRIGGERS: StoryTrigger[] = ["enterNode", "leaveNode", "choiceTaken", "advanced", "variableChanged", "storyStarted"];

function asWhere(value: StoryWhere | null | undefined): WhereClause {
    if (value && (value.$type === "condition" || value.$type === "group")) return value as WhereClause;
    return defaultGroup();
}

function SelectField({label, value, options, onChange, allowEmpty}: {label: string; value: string; options: readonly {value: string; label: string}[]; onChange: (value: string) => void; allowEmpty?: boolean}) {
    return (
        <LyoTextField select size="small" label={label} value={value} onChange={(event) => onChange(event.target.value)}>
            {allowEmpty ? <LyoMenuItem value=""> </LyoMenuItem> : null}
            {options.map((option) => (
                <LyoMenuItem key={option.value} value={option.value}>{option.label}</LyoMenuItem>
            ))}
        </LyoTextField>
    );
}

export function StoryEffectListEditor({effects, variables, nodes, actions, onChange}: {effects: StoryEffect[]; variables: StoryVariable[]; nodes: StoryNode[]; actions: StoryAction[]; onChange: (effects: StoryEffect[]) => void}) {
    const replace = (index: number, patch: Partial<StoryEffect>) => onChange(effects.map((effect, item) => (item === index ? {...effect, ...patch} : effect)));
    const move = (index: number, delta: number) => {
        const target = index + delta;
        if (target < 0 || target >= effects.length) return;
        const next = [...effects];
        const [item] = next.splice(index, 1);
        next.splice(target, 0, item!);
        onChange(next.map((effect, sortOrder) => ({...effect, sortOrder})));
    };

    return (
        <LyoStack spacing={1}>
            {effects.length === 0 ? <span>No response steps.</span> : null}
            {effects.map((effect, index) => (
                <LyoPaper key={index} variant="outlined" sx={{p: 1}}>
                    <LyoStack spacing={1}>
                        <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                            <SelectField label="Step" value={effect.kind} options={EFFECT_KINDS.map((kind) => ({value: kind, label: kind}))} onChange={(kind) => replace(index, {kind: kind as StoryEffectKind})} />
                            <LyoButton size="small" disabled={index === 0} aria-label="Move step up" onClick={() => move(index, -1)}>Up</LyoButton>
                            <LyoButton size="small" disabled={index === effects.length - 1} aria-label="Move step down" onClick={() => move(index, 1)}>Down</LyoButton>
                            <LyoButton size="small" aria-label="Remove step" onClick={() => onChange(effects.filter((_, item) => item !== index))}>Remove</LyoButton>
                        </LyoStack>
                        {effect.kind === "set" || effect.kind === "unset" || effect.kind === "increment" ? (
                            <SelectField label="Variable" value={effect.variableKey ?? ""} options={variables.map((variable) => ({value: variable.key, label: variable.key}))} onChange={(variableKey) => replace(index, {variableKey})} />
                        ) : null}
                        {effect.kind === "set" || effect.kind === "increment" ? (
                            <LyoTextField size="small" label="Value" value={effect.value ?? ""} onChange={(event) => replace(index, {value: event.target.value})} />
                        ) : null}
                        {effect.kind === "gotoNode" ? (
                            <SelectField label="Go to" value={effect.targetNodeKey ?? ""} options={nodes.map((node) => ({value: node.key, label: node.key}))} onChange={(targetNodeKey) => replace(index, {targetNodeKey})} />
                        ) : null}
                        {effect.kind === "invokeAction" ? (
                            <SelectField label="Action" value={effect.invokeActionKey ?? ""} options={actions.map((action) => ({value: action.key, label: action.key}))} onChange={(invokeActionKey) => replace(index, {invokeActionKey})} />
                        ) : null}
                    </LyoStack>
                </LyoPaper>
            ))}
            <LyoButton variant="outlined" size="small" onClick={() => onChange([...effects, {kind: "set", sortOrder: effects.length}])}>Add step</LyoButton>
        </LyoStack>
    );
}

export function StoryChoiceEditor({choice, fields, variables, nodes, actions, onChange}: {choice: StoryChoice; fields: string[]; variables: StoryVariable[]; nodes: StoryNode[]; actions: StoryAction[]; onChange: (choice: StoryChoice) => void}) {
    return (
        <LyoStack spacing={2}>
            <LyoTextField size="small" label="Choice key" value={choice.key} onChange={(event) => onChange({...choice, key: event.target.value})} />
            <LyoTextField size="small" label="Prompt" value={choice.prompt ?? ""} onChange={(event) => onChange({...choice, prompt: event.target.value})} />
            <strong>Enabled when</strong>
            <WhereClauseBuilder value={asWhere(choice.enabledWhen)} fieldPresets={fields} onChange={(enabledWhen) => onChange({...choice, enabledWhen: enabledWhen as StoryWhere})} />
            <strong>Visible when</strong>
            <WhereClauseBuilder value={asWhere(choice.visibleWhen)} fieldPresets={fields} onChange={(visibleWhen) => onChange({...choice, visibleWhen: visibleWhen as StoryWhere})} />
            <strong>Response</strong>
            <StoryEffectListEditor effects={choice.effects ?? []} variables={variables} nodes={nodes} actions={actions} onChange={(effects) => onChange({...choice, effects})} />
        </LyoStack>
    );
}

export function StoryNodeEditor({node, scenes, onChange}: {node: StoryNode; scenes: StoryScene[]; onChange: (node: StoryNode) => void}) {
    return (
        <LyoStack spacing={2}>
            <LyoTextField size="small" label="Node key" value={node.key} onChange={(event) => onChange({...node, key: event.target.value})} />
            <LyoTextField size="small" label="Title" value={node.title ?? ""} onChange={(event) => onChange({...node, title: event.target.value})} />
            <SelectField
                label="Scene"
                value={node.sceneKey ?? ""}
                allowEmpty
                options={scenes.map((scene) => ({value: scene.key, label: scene.title?.trim() ? scene.title : scene.key}))}
                onChange={(sceneKey) => onChange({...node, sceneKey: sceneKey || null})}
            />
            <SelectField label="Presentation" value={node.presentationKind ?? "none"} options={PRESENTATIONS.map((kind) => ({value: kind, label: kind}))} onChange={(presentationKind) => onChange({...node, presentationKind: presentationKind as StoryPresentationKind})} />
            {node.presentationKind === "text" ? (
                <>
                    <LyoTextField size="small" label="Body" value={node.body ?? ""} multiline minRows={4} onChange={(event) => onChange({...node, body: event.target.value})} />
                    <LyoTextField size="small" label="Content type" value={node.contentType ?? ""} onChange={(event) => onChange({...node, contentType: event.target.value})} />
                </>
            ) : null}
            {node.presentationKind === "cutscene" ? (
                <LyoTextField size="small" label="Media reference" value={node.mediaRef ?? ""} onChange={(event) => onChange({...node, mediaRef: event.target.value})} />
            ) : null}
            <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                <LyoSwitch checked={Boolean(node.isEnding)} onChange={(_, checked) => onChange({...node, isEnding: checked})} />
                <span>Ending</span>
            </LyoStack>
        </LyoStack>
    );
}

export function StoryVariableEditor({variables, onChange}: {variables: StoryVariable[]; onChange: (variables: StoryVariable[]) => void}) {
    const replace = (index: number, patch: Partial<StoryVariable>) => onChange(variables.map((variable, item) => (item === index ? {...variable, ...patch} : variable)));
    return (
        <LyoStack spacing={1}>
            {variables.map((variable, index) => (
                <div className="story-variable-row" key={index}>
                    <LyoTextField size="small" label="Key" value={variable.key} onChange={(event) => replace(index, {key: event.target.value})} />
                    <LyoTextField size="small" label="Name" value={variable.name ?? ""} onChange={(event) => replace(index, {name: event.target.value})} />
                    <SelectField label="Type" value={variable.type} options={VARIABLE_TYPES.map((type) => ({value: type, label: type}))} onChange={(type) => replace(index, {type: type as StoryVariableType})} />
                    <LyoTextField size="small" label="Default" value={variable.defaultValue ?? ""} onChange={(event) => replace(index, {defaultValue: event.target.value})} />
                    <LyoButton size="small" aria-label="Remove variable" onClick={() => onChange(variables.filter((_, item) => item !== index))}>Remove</LyoButton>
                </div>
            ))}
            <LyoButton variant="outlined" size="small" onClick={() => onChange([...variables, {key: `flag_${variables.length + 1}`, type: "boolean", defaultValue: "false"}])}>Add variable</LyoButton>
        </LyoStack>
    );
}

export function StoryActionEditor({actions, variables, nodes, onChange}: {actions: StoryAction[]; variables: StoryVariable[]; nodes: StoryNode[]; onChange: (actions: StoryAction[]) => void}) {
    const [index, setIndex] = useState(0);
    const action = actions[index];
    const replace = (patch: Partial<StoryAction>) => onChange(actions.map((item, itemIndex) => (itemIndex === index ? {...item, ...patch} : item)));
    return (
        <LyoStack spacing={2}>
            <LyoButton variant="outlined" size="small" onClick={() => { onChange([...actions, {key: `action_${actions.length + 1}`, effects: []}]); setIndex(actions.length); }}>Add action</LyoButton>
            {actions.length === 0 ? <span>No named actions.</span> : (
                <>
                    <SelectField label="Action" value={String(index)} options={actions.map((item, itemIndex) => ({value: String(itemIndex), label: item.key}))} onChange={(value) => setIndex(Number(value))} />
                    {action ? (
                        <>
                            <LyoTextField size="small" label="Key" value={action.key} onChange={(event) => replace({key: event.target.value})} />
                            <LyoTextField size="small" label="Name" value={action.name ?? ""} onChange={(event) => replace({name: event.target.value})} />
                            <StoryEffectListEditor effects={action.effects ?? []} variables={variables} nodes={nodes} actions={actions} onChange={(effects) => replace({effects})} />
                            <LyoButton color="error" size="small" onClick={() => { onChange(actions.filter((_, item) => item !== index)); setIndex(0); }}>Remove action</LyoButton>
                        </>
                    ) : null}
                </>
            )}
        </LyoStack>
    );
}

export function StoryReactionEditor({reactions, fields, variables, nodes, actions, onChange}: {reactions: StoryReaction[]; fields: string[]; variables: StoryVariable[]; nodes: StoryNode[]; actions: StoryAction[]; onChange: (reactions: StoryReaction[]) => void}) {
    const [index, setIndex] = useState(0);
    const reaction = reactions[index];
    const replace = (patch: Partial<StoryReaction>) => onChange(reactions.map((item, itemIndex) => (itemIndex === index ? {...item, ...patch} : item)));
    return (
        <LyoStack spacing={2}>
            <LyoButton variant="outlined" size="small" onClick={() => { onChange([...reactions, {key: `reaction_${reactions.length + 1}`, trigger: "enterNode", effects: []}]); setIndex(reactions.length); }}>Add reaction</LyoButton>
            {reactions.length === 0 ? <span>No reactions.</span> : (
                <>
                    <SelectField label="Reaction" value={String(index)} options={reactions.map((item, itemIndex) => ({value: String(itemIndex), label: item.key}))} onChange={(value) => setIndex(Number(value))} />
                    {reaction ? (
                        <>
                            <LyoTextField size="small" label="Key" value={reaction.key} onChange={(event) => replace({key: event.target.value})} />
                            <SelectField label="Trigger" value={reaction.trigger} options={TRIGGERS.map((trigger) => ({value: trigger, label: trigger}))} onChange={(trigger) => replace({trigger: trigger as StoryTrigger})} />
                            <SelectField label="Scope node" value={reaction.scopeNodeKey ?? ""} allowEmpty options={nodes.map((node) => ({value: node.key, label: node.key}))} onChange={(scopeNodeKey) => replace({scopeNodeKey: scopeNodeKey || null})} />
                            <SelectField label="Scope variable" value={reaction.scopeVariableKey ?? ""} allowEmpty options={variables.map((variable) => ({value: variable.key, label: variable.key}))} onChange={(scopeVariableKey) => replace({scopeVariableKey: scopeVariableKey || null})} />
                            <LyoStack direction="row" spacing={1} sx={{alignItems: "center"}}>
                                <LyoSwitch checked={Boolean(reaction.once)} onChange={(_, checked) => replace({once: checked})} />
                                <span>Once per playthrough</span>
                            </LyoStack>
                            <strong>When</strong>
                            <WhereClauseBuilder value={asWhere(reaction.when)} fieldPresets={fields} onChange={(when) => replace({when: when as StoryWhere})} />
                            <StoryEffectListEditor effects={reaction.effects ?? []} variables={variables} nodes={nodes} actions={actions} onChange={(effects) => replace({effects})} />
                            <LyoButton color="error" size="small" onClick={() => { onChange(reactions.filter((_, item) => item !== index)); setIndex(0); }}>Remove reaction</LyoButton>
                        </>
                    ) : null}
                </>
            )}
        </LyoStack>
    );
}
