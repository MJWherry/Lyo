export const AuthRoutes = {
    user: "Auth/User",
    token: "Auth/Token",
    claim: "Auth/Claim",
    scope: "Auth/Scope",
    linkedIdentity: "Auth/LinkedIdentity",
    event: "Auth/Event",
} as const;

export function loginUrl(provider: string, returnUrl?: string, prefix = "/auth"): string {
    const path = `${prefix.replace(/\/+$/, "")}/login/${encodeURIComponent(provider)}`;
    if (!returnUrl) return path;
    return `${path}?returnUrl=${encodeURIComponent(returnUrl)}`;
}
