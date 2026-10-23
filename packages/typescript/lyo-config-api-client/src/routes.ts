export const ConfigQueryRoutes = {
    definition: "Config/Definition",
    binding: "Config/Binding",
} as const;

export function configPrefix(prefix = "/api/config"): string {
    const trimmed = prefix.replace(/\/+$/, "");
    return trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
}
