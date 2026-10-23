export function isFeatureEnabled(names: readonly string[], name: string): boolean {
    for (const item of names) {
        if (equalsOrdinalIgnoreCase(item, name))
            return true;
    }

    return false;
}

function equalsOrdinalIgnoreCase(left: string, right: string): boolean {
    if (left.length !== right.length)
        return false;

    for (let i = 0; i < left.length; i++) {
        if (fold(left.charCodeAt(i)) !== fold(right.charCodeAt(i)))
            return false;
    }

    return true;
}

function fold(code: number): number {
    return code >= 65 && code <= 90 ? code + 32 : code;
}
