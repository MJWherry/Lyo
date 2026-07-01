export class ApiClientError extends Error {
    public readonly status?: number;
    public readonly details?: unknown;

    constructor(message: string, status?: number, details?: unknown) {
        super(message);
        this.name = "ApiClientError";
        this.status = status;
        this.details = details;
    }
}

export function toApiClientError(
    status: number,
    payload: unknown
): ApiClientError {
    if (
        payload &&
        typeof payload === "object" &&
        "title" in payload &&
        typeof (payload as { title?: unknown }).title === "string"
    ) {
        const title = (payload as { title: string }).title;
        return new ApiClientError(`${status} ${title}`, status, payload);
    }

    return new ApiClientError(`Request failed with status ${status}`, status, payload);
}
