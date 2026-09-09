/** Wire JSON for POST Reporting/ResolveParameterOptions (camelCase). */

export interface ParameterOptionsItem {
    key: string;
    label: string;
}

export interface ParameterOptionsResolveReq {
    optionsJson?: string | null;
    siblingValues?: Record<string, string | null> | null;
}

export interface ParameterOptionsResolveRes {
    items?: ParameterOptionsItem[] | null;
    rows?: Array<Record<string, unknown>> | null;
    error?: string | null;
}
