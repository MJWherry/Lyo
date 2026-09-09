export function withBearerToken(
    headers: Record<string, string>,
    token?: string
): Record<string, string> {
    if (!token) {
        return headers;
    }

    return {
        ...headers,
        Authorization: `Bearer ${token}`,
    };
}
