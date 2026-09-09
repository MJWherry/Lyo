import type {ReportFormat, ReportGenerationStatus} from "../enums.js";

export interface ReportDefinitionParameterReq {
    reportDefinitionId: string;
    key: string;
    description?: string | null;
    type?: string;
    value?: string | null;
    encryptedValue?: string | null;
    required?: boolean;
    validationRegex?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    allowedValues?: string | null;
    options?: string | null;
    defaultKind?: string;
    defaultTemplate?: string | null;
}

export interface ReportDefinitionParameterRes {
    id: string;
    reportDefinitionId: string;
    key: string;
    description?: string | null;
    type: string;
    value?: string | null;
    encryptedValue?: string | null;
    required: boolean;
    validationRegex?: string | null;
    minLength?: number | null;
    maxLength?: number | null;
    allowedValues?: string | null;
    options?: string | null;
    createdTimestamp?: string;
    updatedTimestamp?: string | null;
    defaultKind?: string;
    defaultTemplate?: string | null;
}

export interface ReportDefinitionReq {
    name: string;
    description?: string | null;
    reportDataJson: string;
    tags?: string | null;
    isActive?: boolean;
    defaultFormat?: ReportFormat | null;
    defaultFileName?: string | null;
    defaultPathPrefix?: string | null;
    generationProfileKey?: string | null;
    createParameters?: ReportDefinitionParameterReq[];
}

export interface ReportDefinitionRes {
    id: string;
    name: string;
    description?: string | null;
    reportDataJson: string;
    tags?: string | null;
    isActive: boolean;
    defaultFormat?: ReportFormat | null;
    defaultFileName?: string | null;
    defaultPathPrefix?: string | null;
    generationProfileKey?: string | null;
    createdBy?: string | null;
    createdTimestamp: string;
    updatedTimestamp?: string | null;
    parameters?: ReportDefinitionParameterRes[] | null;
}

export interface ReportGenerationParameterReq {
    key: string;
    description?: string | null;
    type?: string;
    value?: string | null;
    encryptedValue?: string | null;
}

export interface ReportGenerationParameterRes {
    id: string;
    reportGenerationId: string;
    key: string;
    type: string;
    value?: string | null;
    description?: string | null;
    encryptedValue?: string | null;
}

export interface GenerateReportReq {
    reportDefinitionId?: string | null;
    reportDataJson?: string | null;
    overrideReportDataJson?: string | null;
    format?: ReportFormat | null;
    parameters?: ReportGenerationParameterReq[];
    fileName?: string | null;
    pathPrefix?: string | null;
    createdBy?: string | null;
    includeReportData?: boolean;
}

export interface ReportGenerationRes {
    id: string;
    reportDefinitionId?: string | null;
    reportDataJson?: string | null;
    format: ReportFormat;
    status: ReportGenerationStatus;
    outputFileId?: string | null;
    originalFileName?: string | null;
    contentType?: string | null;
    errorMessage?: string | null;
    pathPrefix?: string | null;
    createdBy: string;
    createdTimestamp: string;
    startedTimestamp?: string | null;
    finishedTimestamp?: string | null;
    parameters?: ReportGenerationParameterRes[] | null;
}

export const REPORT_DEFINITION_GRID_SELECT = ["Id", "Name", "Tags", "IsActive", "DefaultFormat", "CreatedTimestamp"] as const;

export const REPORT_GENERATION_GRID_SELECT = [
    "Id",
    "ReportDefinitionId",
    "Format",
    "Status",
    "OriginalFileName",
    "CreatedTimestamp",
    "FinishedTimestamp",
] as const;

export const REPORT_DEFINITION_DETAIL_INCLUDES = ["Parameters"] as const;
