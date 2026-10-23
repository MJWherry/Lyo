export interface AuthUser {
    id?: string;
    userName?: string | null;
    displayName?: string | null;
    email?: string | null;
}

export interface AuthSession {
    user?: AuthUser | null;
    expiresAt?: string | null;
    scopes?: string[];
    claims?: Record<string, unknown>;
}

export interface TokenDisplay {
    id: string;
    kind?: string;
    ring?: string;
    displayName?: string;
    scopes?: string[];
    createdAt?: string;
    expiresAt?: string | null;
    lastUsedAt?: string | null;
    revokedAt?: string | null;
    revokedReason?: string | null;
    secret?: string;
    secretHash?: string;
    plaintext?: string;
}

export interface CreateTokenRequest {
    displayName: string;
    scopes?: string[];
    lifetimeSeconds?: number;
    metadata?: Record<string, unknown>;
    kind?: string;
}

export interface CreateTokenResponse {
    plaintext: string;
    record: TokenDisplay;
}

/** Drops secret material so a list or log line cannot reprint a token. */
export function tokenForList<T extends {secret?: string; secretHash?: string; plaintext?: string}>(token: T): Omit<T, "secret" | "secretHash" | "plaintext"> {
    const {secret: _secret, secretHash: _hash, plaintext: _plaintext, ...rest} = token;
    return rest;
}
