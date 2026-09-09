export const ReportingRoutes = {
    route: "Reporting",
    definitions: "Reporting/Definition",
    definitionsQuery: "Reporting/Definition/QueryConcrete",
    definitionParameters: "Reporting/Definition/Parameter",
    definitionParametersQuery: "Reporting/Definition/Parameter/QueryConcrete",
    generations: "Reporting/Generation",
    generationsQuery: "Reporting/Generation/QueryConcrete",
    generationsGenerate: "Reporting/Generation/Generate",
    resolveParameterOptions: "Reporting/ResolveParameterOptions",
    generationsDownloadSuffix: "Download",
    generationsRerunSuffix: "Rerun",
} as const;

export function reportingGenerationDownloadPath(id: string): string {
    return `${ReportingRoutes.generations}/${id}/${ReportingRoutes.generationsDownloadSuffix}`;
}

export function reportingGenerationRerunPath(id: string): string {
    return `${ReportingRoutes.generations}/${id}/${ReportingRoutes.generationsRerunSuffix}`;
}

export function joinRoutePrefix(routePrefix: string | undefined, relativePath: string): string {
    const relative = relativePath.replace(/^\/+/, "");
    const prefix = (routePrefix ?? "").replace(/\/+$/, "").replace(/^\/+/, "");
    return prefix ? `/${prefix}/${relative}` : `/${relative}`;
}
