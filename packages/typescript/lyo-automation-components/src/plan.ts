/** One step. Fields besides `type` depend on the step. Unknown types stay in the document. */
export type AutomationStep = {
    type: string;
    name?: string | null;
    url?: string;
    refName?: string;
    variableName?: string;
    value?: string;
    template?: string;
    delayMilliseconds?: number;
    method?: string;
    locator?: {kind: string; value: string};
    action?: {type: string; text?: string; value?: string; index?: number};
    kind?: string;
    attributeName?: string;
    attributeNames?: string[];
    elementRefName?: string;
    elementsListRefName?: string;
    urlPattern?: string;
    durationMilliseconds?: number;
    values?: string[];
    urlListVariableName?: string;
    targetDirectory?: string;
    parentRefName?: string;
    chain?: Array<{kind: string; value: string}>;
    width?: number;
    height?: number;
    jitterPixels?: number;
    sizes?: Array<{width: number; height: number}>;
    tabIndex?: number;
    tabKey?: string;
    tabKeyVariableName?: string;
    fileNamePrefix?: string | null;
    urlListFromCompletedStepIndex?: number | null;
    append?: boolean;
    filePath?: string;
    sourceVariableName?: string;
    itemTemplate?: string;
    headers?: Record<string, string> | null;
    bodyTemplate?: string | null;
    responseBodyVariableName?: string | null;
    statusCodeVariableName?: string | null;
    targetFilePath?: string;
    savedFilePathVariableName?: string | null;
    resolveRelativeUrls?: boolean;
    deduplicate?: boolean;
    splitCommaSeparatedValues?: boolean;
    recordsJsonVariableName?: string;
    targetName?: string;
    sourceDirectory?: string;
    destinationPrefix?: string;
    storedFileListVariableName?: string | null;
    serviceType?: string;
    methodName?: string;
    arguments?: Record<string, string> | null;
    throwOnMissingMethod?: boolean;
    resultVariableName?: string | null;
};

/** Ordered automation plan. Same JSON the C# runner accepts. */
export type AutomationPlan = {
    name?: string | null;
    steps: AutomationStep[];
};

/** Steps the editor draws as a form. `type` is the JSON discriminator the runner accepts. */
export const stepCatalog = [
    {type: "navigate", label: "Navigate"},
    {type: "findElement", label: "Find element"},
    {type: "findDescendant", label: "Find descendant"},
    {type: "elementAction", label: "Element action"},
    {type: "findAndAct", label: "Find and act"},
    {type: "findElementChain", label: "Find element chain"},
    {type: "findAndActChain", label: "Find and act chain"},
    {type: "delay", label: "Delay"},
    {type: "captureNetwork", label: "Capture network"},
    {type: "reload", label: "Reload"},
    {type: "setViewportSize", label: "Set viewport"},
    {type: "randomizeViewport", label: "Randomize viewport"},
    {type: "switchTabByIndex", label: "Switch tab by index"},
    {type: "switchTabByKey", label: "Switch tab by key"},
    {type: "openNewTab", label: "Open new tab"},
    {type: "closeCurrentTab", label: "Close current tab"},
    {type: "findElementsChain", label: "Find elements chain"},
    {type: "extractElementData", label: "Extract element"},
    {type: "extractElementsListData", label: "Extract element list"},
    {type: "writeStringListToFile", label: "Write string list"},
    {type: "downloadUrlsToDirectory", label: "Download URLs"},
    {type: "storeLiteral", label: "Store literal"},
    {type: "storeStringList", label: "Store string list"},
    {type: "storeTemplate", label: "Store template"},
    {type: "storeStringListFromTemplate", label: "Store list from template"},
    {type: "storePageUrl", label: "Page URL"},
    {type: "storePageTitle", label: "Page title"},
    {type: "httpRequest", label: "HTTP request"},
    {type: "downloadFile", label: "Download file"},
    {type: "extractSources", label: "Extract sources"},
    {type: "upsertJsonRecords", label: "Upsert JSON records"},
    {type: "uploadDirectoryToFileStorage", label: "Upload directory"},
    {type: "invokeDiMethod", label: "Invoke method"},
] as const;

export const actionTypes = ["click", "inputText", "sendKeys", "clear", "submit", "selectByText", "selectByValue", "selectByIndex", "dropdown"] as const;

export const formStepTypes = stepCatalog.map((item) => item.type);

const FORM_TYPES = new Set<string>([...formStepTypes, "downloadUrls"]);

/** Readable name for a step type. Unknown types stay as the raw discriminator. */
export function stepLabel(type: string): string {
    return stepCatalog.find((item) => item.type === type)?.label ?? type;
}

/** A named plan with no steps. */
export function createEmptyPlan(name: string): AutomationPlan {
    return {name, steps: []};
}

/** One file-name segment ending in `.json`. Directory pieces in `name` are dropped. */
export function planFileName(name: string): string {
    const parts = name.split(/[/\\]/).filter((part) => part.length > 0 && part !== "." && part !== "..");
    const segment = parts[parts.length - 1] ?? "plan";
    const stem = segment.replace(/\.json$/i, "").replace(/[^A-Za-z0-9._ -]+/g, "").trim() || "plan";
    return `${stem}.json`;
}

/** True when the editor has fields for this step. Other steps stay in the plan and are edited as JSON. */
export function hasForm(step: AutomationStep): boolean {
    return FORM_TYPES.has(step.type);
}

/** Appends a navigate step. */
export function insertNavigate(plan: AutomationPlan, url: string): AutomationPlan {
    return {...plan, steps: [...plan.steps, {type: "navigate", url}]};
}

/** Moves the step at `fromIndex` so it lands at `toIndex`. */
export function moveStep(plan: AutomationPlan, fromIndex: number, toIndex: number): AutomationPlan {
    const steps = [...plan.steps];
    const [step] = steps.splice(fromIndex, 1);
    if (!step)
        return plan;
    steps.splice(toIndex, 0, step);
    return {...plan, steps};
}

/** Drops the step at `index`. Other steps stay in order. */
export function removeStep(plan: AutomationPlan, index: number): AutomationPlan {
    return {...plan, steps: plan.steps.filter((_, item) => item !== index)};
}

/** Parses plan JSON and writes it back. A step type the form does not edit, such as `httpRequest`, is kept. */
export function roundTrip(json: string): string {
    const plan = JSON.parse(json) as AutomationPlan;
    return JSON.stringify(plan);
}

/** A new step of `type` with empty fields the form can fill in. */
export function createStep(type: string): AutomationStep {
    switch (type) {
        case "navigate":
            return {type, url: "https://example.com/"};
        case "reload":
            return {type};
        case "delay":
            return {type, delayMilliseconds: 500};
        case "findElement":
            return {type, refName: "element", locator: {kind: "CssSelector", value: "#main"}};
        case "findAndAct":
            return {type, refName: "element", locator: {kind: "CssSelector", value: "button"}, action: {type: "click"}};
        case "extractElementData":
            return {type, elementRefName: "element", variableName: "value", kind: "Text"};
        case "storeLiteral":
            return {type, variableName: "value", value: ""};
        case "storeTemplate":
            return {type, variableName: "summary", template: "{strings.value}"};
        case "storePageUrl":
            return {type, variableName: "pageUrl"};
        case "storePageTitle":
            return {type, variableName: "pageTitle"};
        case "captureNetwork":
            return {type, variableName: "urls", urlPattern: "", durationMilliseconds: 3000};
        case "storeStringList":
            return {type, variableName: "urls", values: []};
        case "extractSources":
            return {type, elementsListRefName: "elements", variableName: "images", attributeNames: ["src", "data-src", "data-lazy-src", "srcset"]};
        case "downloadUrls":
        case "downloadUrlsToDirectory":
            return {type: "downloadUrlsToDirectory", urlListVariableName: "urls", targetDirectory: "downloads"};
        case "findDescendant":
            return {type, parentRefName: "parent", refName: "child", locator: {kind: "CssSelector", value: ".item"}};
        case "elementAction":
            return {type, elementRefName: "element", action: {type: "click"}};
        case "findElementChain":
            return {type, refName: "element", chain: [{kind: "CssSelector", value: "#main"}]};
        case "findAndActChain":
            return {type, refName: "element", chain: [{kind: "CssSelector", value: "button"}], action: {type: "click"}};
        case "findElementsChain":
            return {type, refName: "elements", chain: [{kind: "CssSelector", value: "a"}]};
        case "setViewportSize":
            return {type, width: 1280, height: 720};
        case "randomizeViewport":
            return {type, sizes: [{width: 1280, height: 720}], jitterPixels: 0};
        case "switchTabByIndex":
            return {type, tabIndex: 0};
        case "switchTabByKey":
            return {type, tabKey: ""};
        case "openNewTab":
            return {type, url: "https://example.com/", tabKeyVariableName: "tabKey"};
        case "closeCurrentTab":
            return {type};
        case "extractElementsListData":
            return {type, elementsListRefName: "elements", variableName: "values", kind: "Text"};
        case "writeStringListToFile":
            return {type, variableName: "urls", filePath: "urls.txt", append: false};
        case "storeStringListFromTemplate":
            return {type, sourceVariableName: "urls", variableName: "lines", itemTemplate: "{item}"};
        case "httpRequest":
            return {type, method: "GET", url: "https://example.com/"};
        case "downloadFile":
            return {type, url: "https://example.com/file", targetFilePath: "file.bin"};
        case "upsertJsonRecords":
            return {type, recordsJsonVariableName: "records", targetName: "target"};
        case "uploadDirectoryToFileStorage":
            return {type, sourceDirectory: "downloads", destinationPrefix: "automation/uploads"};
        case "invokeDiMethod":
            return {type, serviceType: "Service", methodName: "Run", throwOnMissingMethod: true};
        default:
            return {type};
    }
}
