"use client";

import {LyoButton, LyoMenuItem, LyoSelect, LyoStack, LyoTextField} from "lyo-web-components";
import {actionTypes, type AutomationStep} from "./plan.js";

const locatorKinds = ["CssSelector", "XPath", "Id", "Name", "LinkText", "PartialLinkText", "TagName", "ClassName"];

export function AutomationPlanStepFields({step, onChange}: {step: AutomationStep; onChange: (step: AutomationStep) => void}) {
    const set = (patch: Partial<AutomationStep>) => onChange({...step, ...patch});
    const type = step.type === "downloadUrls" ? "downloadUrlsToDirectory" : step.type;
    return (
        <LyoStack spacing={1}>
            <LyoTextField label="Name" value={step.name ?? ""} onChange={(event) => set({name: event.target.value || null})} />
            {type === "navigate" || type === "openNewTab" || type === "httpRequest" || type === "downloadFile" ? (
                <LyoTextField label="URL" value={step.url ?? ""} onChange={(event) => set({url: event.target.value})} />
            ) : null}
            {type === "httpRequest" ? (
                <>
                    <LyoTextField label="Method" value={step.method ?? "GET"} onChange={(event) => set({method: event.target.value})} />
                    <LyoTextField label="Body template" value={step.bodyTemplate ?? ""} multiline minRows={3} onChange={(event) => set({bodyTemplate: event.target.value})} />
                    <LyoTextField label="Response variable" value={step.responseBodyVariableName ?? ""} onChange={(event) => set({responseBodyVariableName: event.target.value})} />
                    <LyoTextField label="Status variable" value={step.statusCodeVariableName ?? ""} onChange={(event) => set({statusCodeVariableName: event.target.value})} />
                    <PairList label="Header" pairs={step.headers ?? {}} onChange={(headers) => set({headers})} />
                </>
            ) : null}
            {type === "delay" ? (
                <LyoTextField label="Milliseconds" value={String(step.delayMilliseconds ?? 0)} onChange={(event) => set({delayMilliseconds: Number(event.target.value) || 0})} />
            ) : null}
            {type === "findDescendant" ? (
                <LyoTextField label="Parent ref" value={step.parentRefName ?? ""} onChange={(event) => set({parentRefName: event.target.value})} />
            ) : null}
            {type === "findElement" || type === "findAndAct" || type === "findDescendant" || type === "findElementChain" || type === "findAndActChain" || type === "findElementsChain" ? (
                <LyoTextField label="Ref name" value={step.refName ?? ""} onChange={(event) => set({refName: event.target.value})} />
            ) : null}
            {type === "elementAction" ? (
                <LyoTextField label="Element ref" value={step.elementRefName ?? ""} onChange={(event) => set({elementRefName: event.target.value})} />
            ) : null}
            {type === "findElement" || type === "findAndAct" || type === "findDescendant" ? <LocatorFields step={step} onChange={set} /> : null}
            {type === "findElementChain" || type === "findAndActChain" || type === "findElementsChain" ? (
                <ChainFields step={step} onChange={set} />
            ) : null}
            {type === "findAndAct" || type === "findAndActChain" || type === "elementAction" ? <ActionFields step={step} onChange={set} /> : null}
            {type === "extractElementData" || type === "extractElementsListData" ? (
                <>
                    {type === "extractElementsListData" ? (
                        <LyoTextField label="Element list" value={step.elementsListRefName ?? ""} onChange={(event) => set({elementsListRefName: event.target.value})} />
                    ) : (
                        <LyoTextField label="Element ref" value={step.elementRefName ?? ""} onChange={(event) => set({elementRefName: event.target.value})} />
                    )}
                    <LyoTextField label="Variable" value={step.variableName ?? ""} onChange={(event) => set({variableName: event.target.value})} />
                    <LyoTextField label="Extract" value={step.kind ?? "Text"} onChange={(event) => set({kind: event.target.value})} />
                    <LyoTextField label="Attribute" value={step.attributeName ?? ""} onChange={(event) => set({attributeName: event.target.value})} />
                </>
            ) : null}
            {type === "storeLiteral" || type === "storeTemplate" || type === "storePageUrl" || type === "storePageTitle" || type === "captureNetwork" || type === "storeStringList" || type === "writeStringListToFile" || type === "upsertJsonRecords" || type === "extractSources" ? (
                <LyoTextField label="Variable" value={step.variableName ?? ""} onChange={(event) => set({variableName: event.target.value})} />
            ) : null}
            {type === "storeLiteral" ? <LyoTextField label="Value" value={step.value ?? ""} multiline minRows={3} onChange={(event) => set({value: event.target.value})} /> : null}
            {type === "storeTemplate" ? <LyoTextField label="Template" value={step.template ?? ""} multiline minRows={3} onChange={(event) => set({template: event.target.value})} /> : null}
            {type === "storeStringListFromTemplate" ? (
                <>
                    <LyoTextField label="Source list" value={step.sourceVariableName ?? ""} onChange={(event) => set({sourceVariableName: event.target.value})} />
                    <LyoTextField label="Variable" value={step.variableName ?? ""} onChange={(event) => set({variableName: event.target.value})} />
                    <LyoTextField label="Item template" value={step.itemTemplate ?? ""} onChange={(event) => set({itemTemplate: event.target.value})} />
                </>
            ) : null}
            {type === "storeStringList" ? (
                <LyoTextField label="Values" value={(step.values ?? []).join("\n")} multiline minRows={3} onChange={(event) => set({values: event.target.value.split(/\r?\n/).filter((line) => line.length > 0)})} />
            ) : null}
            {type === "captureNetwork" ? (
                <>
                    <LyoTextField label="URL regex" value={step.urlPattern ?? ""} onChange={(event) => set({urlPattern: event.target.value})} />
                    <LyoTextField label="Listen milliseconds" value={String(step.durationMilliseconds ?? 3000)} onChange={(event) => set({durationMilliseconds: Number(event.target.value) || 0})} />
                </>
            ) : null}
            {type === "setViewportSize" ? (
                <>
                    <LyoTextField label="Width" value={String(step.width ?? 0)} onChange={(event) => set({width: Number(event.target.value) || 0})} />
                    <LyoTextField label="Height" value={String(step.height ?? 0)} onChange={(event) => set({height: Number(event.target.value) || 0})} />
                </>
            ) : null}
            {type === "randomizeViewport" ? (
                <>
                    <LyoTextField label="Jitter pixels" value={String(step.jitterPixels ?? 0)} onChange={(event) => set({jitterPixels: Number(event.target.value) || 0})} />
                    {(step.sizes ?? []).map((size, index) => (
                        <LyoStack key={index} direction="row" spacing={1}>
                            <LyoTextField label="Width" value={String(size.width)} onChange={(event) => set({sizes: replaceAt(step.sizes ?? [], index, {...size, width: Number(event.target.value) || 0})})} />
                            <LyoTextField label="Height" value={String(size.height)} onChange={(event) => set({sizes: replaceAt(step.sizes ?? [], index, {...size, height: Number(event.target.value) || 0})})} />
                        </LyoStack>
                    ))}
                    <LyoButton onClick={() => set({sizes: [...(step.sizes ?? []), {width: 1280, height: 720}]})}>Add size</LyoButton>
                </>
            ) : null}
            {type === "switchTabByIndex" ? <LyoTextField label="Tab index" value={String(step.tabIndex ?? 0)} onChange={(event) => set({tabIndex: Number(event.target.value) || 0})} /> : null}
            {type === "switchTabByKey" ? <LyoTextField label="Tab key" value={step.tabKey ?? ""} onChange={(event) => set({tabKey: event.target.value})} /> : null}
            {type === "openNewTab" ? <LyoTextField label="Tab key variable" value={step.tabKeyVariableName ?? ""} onChange={(event) => set({tabKeyVariableName: event.target.value})} /> : null}
            {type === "writeStringListToFile" ? (
                <>
                    <LyoTextField label="File path" value={step.filePath ?? ""} onChange={(event) => set({filePath: event.target.value})} />
                    <LyoTextField label="Append" value={step.append ? "true" : "false"} onChange={(event) => set({append: event.target.value === "true"})} />
                </>
            ) : null}
            {type === "downloadUrlsToDirectory" ? (
                <>
                    <LyoTextField label="URL list variable" value={step.urlListVariableName ?? ""} onChange={(event) => set({type: "downloadUrlsToDirectory", urlListVariableName: event.target.value})} />
                    <LyoTextField label="Directory" value={step.targetDirectory ?? ""} onChange={(event) => set({type: "downloadUrlsToDirectory", targetDirectory: event.target.value})} />
                    <LyoTextField label="File name prefix" value={step.fileNamePrefix ?? ""} onChange={(event) => set({type: "downloadUrlsToDirectory", fileNamePrefix: event.target.value})} />
                    <LyoTextField label="Source step index" value={step.urlListFromCompletedStepIndex == null ? "" : String(step.urlListFromCompletedStepIndex)} onChange={(event) => set({type: "downloadUrlsToDirectory", urlListFromCompletedStepIndex: event.target.value === "" ? null : Number(event.target.value) || 0})} />
                </>
            ) : null}
            {type === "downloadFile" ? (
                <>
                    <LyoTextField label="Target file" value={step.targetFilePath ?? ""} onChange={(event) => set({targetFilePath: event.target.value})} />
                    <LyoTextField label="Saved path variable" value={step.savedFilePathVariableName ?? ""} onChange={(event) => set({savedFilePathVariableName: event.target.value})} />
                </>
            ) : null}
            {type === "extractSources" ? (
                <>
                    <LyoTextField label="Element list" value={step.elementsListRefName ?? ""} onChange={(event) => set({elementsListRefName: event.target.value})} />
                    <LyoTextField label="Attributes" value={(step.attributeNames ?? []).join(", ")} onChange={(event) => set({attributeNames: event.target.value.split(",").map((name) => name.trim()).filter((name) => name.length > 0)})} />
                    <LyoTextField label="Resolve relative URLs" value={step.resolveRelativeUrls === false ? "false" : "true"} onChange={(event) => set({resolveRelativeUrls: event.target.value !== "false"})} />
                    <LyoTextField label="Deduplicate" value={step.deduplicate === false ? "false" : "true"} onChange={(event) => set({deduplicate: event.target.value !== "false"})} />
                    <LyoTextField label="Split comma-separated values" value={step.splitCommaSeparatedValues === false ? "false" : "true"} onChange={(event) => set({splitCommaSeparatedValues: event.target.value !== "false"})} />
                </>
            ) : null}
            {type === "upsertJsonRecords" ? (
                <>
                    <LyoTextField label="Records variable" value={step.recordsJsonVariableName ?? ""} onChange={(event) => set({recordsJsonVariableName: event.target.value})} />
                    <LyoTextField label="Target" value={step.targetName ?? ""} onChange={(event) => set({targetName: event.target.value})} />
                </>
            ) : null}
            {type === "uploadDirectoryToFileStorage" ? (
                <>
                    <LyoTextField label="Source directory" value={step.sourceDirectory ?? ""} onChange={(event) => set({sourceDirectory: event.target.value})} />
                    <LyoTextField label="Destination prefix" value={step.destinationPrefix ?? ""} onChange={(event) => set({destinationPrefix: event.target.value})} />
                    <LyoTextField label="Stored file list variable" value={step.storedFileListVariableName ?? ""} onChange={(event) => set({storedFileListVariableName: event.target.value})} />
                </>
            ) : null}
            {type === "invokeDiMethod" ? (
                <>
                    <LyoTextField label="Service type" value={step.serviceType ?? ""} onChange={(event) => set({serviceType: event.target.value})} />
                    <LyoTextField label="Method" value={step.methodName ?? ""} onChange={(event) => set({methodName: event.target.value})} />
                    <LyoTextField label="Result variable" value={step.resultVariableName ?? ""} onChange={(event) => set({resultVariableName: event.target.value})} />
                    <LyoTextField label="Throw when missing" value={step.throwOnMissingMethod === false ? "false" : "true"} onChange={(event) => set({throwOnMissingMethod: event.target.value !== "false"})} />
                    <PairList label="Argument" pairs={step.arguments ?? {}} onChange={(pairs) => set({arguments: pairs})} />
                </>
            ) : null}
        </LyoStack>
    );
}

function LocatorFields({step, onChange}: {step: AutomationStep; onChange: (patch: Partial<AutomationStep>) => void}) {
    return (
        <>
            <LyoSelect label="Locator" value={step.locator?.kind ?? "CssSelector"} onChange={(event) => onChange({locator: {kind: String(event.target.value), value: step.locator?.value ?? ""}})}>
                {locatorKinds.map((kind) => <LyoMenuItem key={kind} value={kind}>{kind}</LyoMenuItem>)}
            </LyoSelect>
            <LyoTextField label="Locator value" value={step.locator?.value ?? ""} onChange={(event) => onChange({locator: {kind: step.locator?.kind ?? "CssSelector", value: event.target.value}})} />
        </>
    );
}

function ActionFields({step, onChange}: {step: AutomationStep; onChange: (patch: Partial<AutomationStep>) => void}) {
    const action = step.action?.type ?? "click";
    return (
        <>
            <LyoSelect label="Action" value={action} onChange={(event) => onChange({action: {...step.action, type: String(event.target.value)}})}>
                {actionTypes.map((item) => <LyoMenuItem key={item} value={item}>{item}</LyoMenuItem>)}
            </LyoSelect>
            {action !== "click" && action !== "clear" && action !== "submit" ? (
                <LyoTextField label="Action value" value={step.action?.text ?? step.action?.value ?? ""} onChange={(event) => onChange({action: {...step.action, type: action, text: event.target.value}})} />
            ) : null}
        </>
    );
}

function ChainFields({step, onChange}: {step: AutomationStep; onChange: (patch: Partial<AutomationStep>) => void}) {
    const chain = step.chain ?? [];
    return (
        <>
            {chain.map((segment, index) => (
                <LyoStack key={index} direction="row" spacing={1}>
                    <LyoTextField label="Kind" value={segment.kind} onChange={(event) => onChange({chain: replaceAt(chain, index, {...segment, kind: event.target.value})})} />
                    <LyoTextField label="Value" value={segment.value} onChange={(event) => onChange({chain: replaceAt(chain, index, {...segment, value: event.target.value})})} />
                </LyoStack>
            ))}
            <LyoButton onClick={() => onChange({chain: [...chain, {kind: "CssSelector", value: ""}]})}>Add locator</LyoButton>
        </>
    );
}

function PairList({label, pairs, onChange}: {label: string; pairs: Record<string, string>; onChange: (pairs: Record<string, string>) => void}) {
    const rows = Object.entries(pairs);
    return (
        <>
            {rows.map(([key, value]) => (
                <LyoStack key={key} direction="row" spacing={1}>
                    <LyoTextField label={`${label} name`} value={key} onChange={() => undefined} />
                    <LyoTextField label="Value" value={value} onChange={(event) => onChange({...pairs, [key]: event.target.value})} />
                </LyoStack>
            ))}
            <LyoButton onClick={() => onChange({...pairs, "": ""})}>Add {label.toLowerCase()}</LyoButton>
        </>
    );
}

function replaceAt<T>(items: T[], index: number, next: T): T[] {
    return items.map((item, itemIndex) => (itemIndex === index ? next : item));
}
