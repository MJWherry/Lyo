/** Thrown when a presigned provider URL is requested for an encrypted or compressed object. */
export class ProviderObjectBlockedError extends Error {
    constructor(kind: "download" | "upload") {
        super(
            kind === "download"
                ? "Presigned download is only available when the file is neither encrypted nor compressed. Use download."
                : "Presigned upload is only available when the file is neither compressed nor encrypted. Use upload."
        );
        this.name = "ProviderObjectBlockedError";
    }
}

export function allowsProviderObject(file: {isEncrypted?: boolean; isCompressed?: boolean}): boolean {
    return file.isEncrypted !== true && file.isCompressed !== true;
}

export function assertProviderDownload(file: {isEncrypted?: boolean; isCompressed?: boolean}): void {
    if (!allowsProviderObject(file)) throw new ProviderObjectBlockedError("download");
}

export function assertProviderUpload(file: {encrypt?: boolean; compress?: boolean}): void {
    if (file.encrypt === true || file.compress === true) throw new ProviderObjectBlockedError("upload");
}
