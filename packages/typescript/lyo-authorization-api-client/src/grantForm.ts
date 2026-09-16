export type Grant = {
    id: string;
    subject: string;
    action: string;
    resourceType: string;
    resourceId?: string | null;
    tenantId?: string | null;
    grantedBy?: string | null;
    expiresAt?: string | null;
    revokedAt?: string | null;
    createdTimestamp?: string;
};

export type GrantWrite = {
    subject: string;
    action: string;
    resourceType: string;
    resourceId?: string | null;
    tenantId?: string | null;
    grantedBy?: string | null;
    expiresAt?: string | null;
};

export type GrantCheck = {
    subject: string;
    action: string;
    resourceType: string;
    resourceId?: string | null;
    tenantId?: string | null;
};

export type GrantListQuery = {
    subject?: string;
    includeRevoked?: boolean;
    includeExpired?: boolean;
    take?: number;
};

export type GrantFormInput = {
    subject: string;
    action: string;
    resourceType: string;
    resourceId?: string | null;
    tenantId?: string | null;
    grantedBy?: string | null;
    expiresAt?: string | null;
};

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function grantFormToWrite(input: GrantFormInput): GrantWrite {
    return {
        subject: input.subject.trim(),
        action: input.action.trim(),
        resourceType: input.resourceType.trim(),
        resourceId: resourceId(input.resourceId),
        tenantId: tenantId(input.tenantId),
        grantedBy: blank(input.grantedBy) ? null : input.grantedBy!.trim(),
        expiresAt: expiry(input.expiresAt),
    };
}

export function grantFormToCheck(input: GrantFormInput): GrantCheck {
    const write = grantFormToWrite(input);
    return {
        subject: write.subject,
        action: write.action,
        resourceType: write.resourceType,
        resourceId: write.resourceId,
        tenantId: write.tenantId,
    };
}

function resourceId(value?: string | null): string | null {
    if (value == null || value.length === 0) return null;
    if (value.trim().length === 0) throw new Error("A whitespace resource id is not a wildcard.");
    const trimmed = value.trim();
    return guidPattern.test(trimmed) ? trimmed.toLowerCase() : trimmed;
}

function tenantId(value?: string | null): string | null {
    if (blank(value)) return null;
    const trimmed = value!.trim();
    if (!guidPattern.test(trimmed)) throw new Error("Tenant id must be a guid.");
    return trimmed.toLowerCase();
}

function expiry(value?: string | null): string | null {
    if (blank(value)) return null;
    const text = value!.trim();
    const wall = text.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2})(?::(\d{2}))?/);
    if (!wall) throw new Error("Expiry must be a date time.");
    return `${wall[1]}:${wall[2] ?? "00"}.000Z`;
}

function blank(value?: string | null): boolean {
    return value == null || value.trim().length === 0;
}
