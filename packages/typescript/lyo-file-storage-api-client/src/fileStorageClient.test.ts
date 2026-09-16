import {describe, expect, it} from "vitest";
import {createApiClient, createAsyncApiClient, type ApiResponse, type ApiTransport, type AsyncApiTransport} from "lyo-api-client";
import {createAsyncFileStorageApiClient, createFileStorageApiClient} from "./fileStorageClient.js";
import {FileStorageRoutes, joinRoutePrefix} from "./routes.js";
import type {FileStoreResult} from "./models.js";

const fileId = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

function ok<T>(data: T, extra?: Partial<ApiResponse<T>>): ApiResponse<T> {
    return {status: 200, ok: true, data, ...extra};
}

function plaintext(): FileStoreResult {
    return {id: fileId, isEncrypted: false, isCompressed: false, originalFileName: "a.bin"};
}

describe("joinRoutePrefix_joinsOptionalPrefix", () => {
    it("joins FileStorage/files/folder and an api prefix", () => {
        expect(joinRoutePrefix(undefined, "FileStorage/files/folder")).toBe("/FileStorage/files/folder");
        expect(joinRoutePrefix("api", "FileStorage/files/folder")).toBe("/api/FileStorage/files/folder");
    });
});

describe("createAsyncFileStorageApiClient_hitsRoutes", () => {
    it("calls each file-storage route with the expected verb and body", async () => {
        const calls: Array<{method: string; url: string; body?: string; responseType?: string}> = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push({method: req.method, url: req.url, body: req.body, responseType: req.responseType});
            if (req.responseType === "blob") {
                return ok(undefined, {blob: new Blob(["bytes"]), headers: {"content-disposition": 'attachment; filename="a.bin"'}});
            }
            if (req.url.includes("presigned-read")) return ok({url: "https://provider.example/get"});
            if (req.url.includes("direct-upload/begin")) {
                return ok({fileId, presignedPutUrl: "https://provider.example/put", urlExpiresUtc: "2026-01-01T00:00:00Z", storageLocation: "s3"});
            }
            return ok({id: fileId});
        };
        const files = createAsyncFileStorageApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        await files.health();
        await files.listKeyIds();
        await files.listFolder({pathPrefix: "docs", maxKeys: 10});
        await files.listStorageKeys({prefix: "docs", maxKeys: 5});
        await files.saveBytes({data: "YQ==", originalFileName: "a.bin", pathPrefix: "docs"});
        await files.saveStream({file: new Blob(["hi"]), fileName: "a.bin", compress: false, encrypt: true, keyId: "k1", pathPrefix: "docs"});
        await files.getMetadata(fileId);
        await files.download(fileId);
        await files.downloadArchive([fileId], "bundle.zip");
        await files.copy({sourceFileId: fileId, pathPrefix: "other"});
        await files.move({fileId, pathPrefix: "other"});
        await files.rename({fileId, originalFileName: "b.bin"});
        await files.delete(fileId);
        await files.createAccessLink(fileId, {maxDownloads: 1});
        await files.presignedRead(fileId);
        await files.beginDirectUpload({declaredMaxSizeBytes: 10, originalFileName: "a.bin"});
        await files.completeDirectUpload(fileId, {expectedByteLength: 2});
        await files.putDirectUpload(fileId, new Blob(["hi"]));
        await files.beginMultipart({originalFileName: "a.bin", partSizeBytes: 1024});
        await files.partUrl(fileId, 1);
        await files.completeMultipart({sessionId: fileId, parts: [{partNumber: 1, eTagOrBlockId: "etag"}]});
        await files.abortMultipart(fileId);
        await files.migrateDeks({sourceKeyId: "k1"});
        await files.rotateDeks({fileIds: [fileId], targetKeyId: "k2"});
        await files.downloadPhysical("obj/key");
        await files.deletePhysical("obj/key");
        await files.movePhysical({physicalKey: "obj/key", destinationPathPrefix: "next"});
        await files.copyPhysical({physicalKey: "obj/key", destinationPathPrefix: "next"});
        await files.renamePhysical({physicalKey: "obj/key", newFileName: "b.bin"});
        await files.queryConcrete({Amount: 10});
        await files.queryProject({Amount: 10, Select: ["Id"]});

        const url = (i: number) => calls[i]!.url;
        expect(calls[0]).toMatchObject({method: "GET", url: "http://x/FileStorage/health"});
        expect(calls[1]).toMatchObject({method: "GET", url: "http://x/FileStorage/key-ids"});
        expect(url(2)).toContain("/FileStorage/files/folder");
        expect(url(2)).toContain("pathPrefix=docs");
        expect(url(3)).toContain("/FileStorage/diagnostics/storage-keys");
        expect(calls[4]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/save"});
        expect(JSON.parse(calls[4]!.body!)).toMatchObject({data: "YQ==", pathPrefix: "docs"});
        expect(calls[5]).toMatchObject({method: "POST"});
        expect(url(5)).toContain("/FileStorage/files/save-stream");
        expect(url(5)).toContain("encrypt=true");
        expect(url(5)).toContain("keyId=k1");
        expect(url(5)).toContain("pathPrefix=docs");
        expect(calls[5]!.body).toBeInstanceOf(FormData);
        expect(calls[6]).toMatchObject({method: "GET", url: `http://x/FileStorage/files/${fileId}/metadata`});
        expect(calls[7]).toMatchObject({method: "GET", url: `http://x/FileStorage/files/${fileId}/download`, responseType: "blob"});
        expect(url(8)).toContain("/FileStorage/files/archive");
        expect(url(8)).toContain(`id=${fileId}`);
        expect(calls[8]!.responseType).toBe("blob");
        expect(calls[9]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/copy"});
        expect(calls[10]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/move"});
        expect(calls[11]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/rename"});
        expect(calls[12]).toMatchObject({method: "DELETE", url: `http://x/FileStorage/files/${fileId}`});
        expect(calls[13]).toMatchObject({method: "POST", url: `http://x/FileStorage/files/${fileId}/access-links`});
        expect(calls[14]).toMatchObject({method: "GET", url: `http://x/FileStorage/files/${fileId}/presigned-read`});
        expect(calls[15]).toMatchObject({method: "POST", url: "http://x/FileStorage/direct-upload/begin"});
        expect(calls[16]).toMatchObject({method: "POST", url: `http://x/FileStorage/direct-upload/${fileId}/complete`});
        expect(calls[17]).toMatchObject({method: "PUT", url: `http://x/FileStorage/direct-upload/${fileId}/put`});
        expect(calls[18]).toMatchObject({method: "POST", url: "http://x/FileStorage/multipart/begin"});
        expect(url(19)).toContain(`/FileStorage/multipart/${fileId}/part-url`);
        expect(url(19)).toContain("partNumber=1");
        expect(calls[20]).toMatchObject({method: "POST", url: "http://x/FileStorage/multipart/complete"});
        expect(calls[21]).toMatchObject({method: "POST", url: `http://x/FileStorage/multipart/${fileId}/abort`});
        expect(calls[22]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/migrate-deks"});
        expect(calls[23]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/rotate-deks"});
        expect(url(24)).toContain("/FileStorage/files/physical/download");
        expect(calls[25]).toMatchObject({method: "DELETE"});
        expect(url(25)).toContain("/FileStorage/files/physical");
        expect(calls[26]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/physical/move"});
        expect(calls[27]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/physical/copy"});
        expect(calls[28]).toMatchObject({method: "POST", url: "http://x/FileStorage/files/physical/rename"});
        expect(calls[29]).toMatchObject({method: "POST", url: `http://x/${FileStorageRoutes.fileMetadata}/QueryConcrete`});
        expect(calls[30]).toMatchObject({method: "POST", url: `http://x/${FileStorageRoutes.fileMetadata}/QueryProject`});
    });
});

describe("capabilities_getsTheCapabilitiesRoute", () => {
    it("gets FileStorage/capabilities on the async and sync clients", async () => {
        const calls: string[] = [];
        const body = {presignedRead: true, directUpload: false, multipart: true, presignedMultipartParts: false, copy: true, move: true, physical: true, encryption: false, archive: true, accessLinks: true};
        const transport: AsyncApiTransport = async (req) => {
            calls.push(req.url);
            return ok(body);
        };
        const asyncFiles = createAsyncFileStorageApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const response = await asyncFiles.capabilities();
        expect(response.data?.presignedMultipartParts).toBe(false);
        expect(response.data?.presignedRead).toBe(true);

        const syncTransport: ApiTransport = (req) => {
            calls.push(req.url);
            return ok(body);
        };
        const syncFiles = createFileStorageApiClient(createApiClient({baseUrl: "http://x", transport: syncTransport}));
        expect(syncFiles.capabilities().data?.encryption).toBe(false);
        expect(calls).toEqual(["http://x/FileStorage/capabilities", "http://x/FileStorage/capabilities"]);
    });
});

describe("presignedUrls_refuseEncryptedOrCompressed", () => {
    it("does not call the transport for a provider download or upload when the file is encrypted or compressed", async () => {
        const calls: string[] = [];
        const transport: AsyncApiTransport = async (req) => {
            calls.push(req.url);
            return ok({url: "https://provider.example/get", fileId, presignedPutUrl: "https://provider.example/put"});
        };
        const files = createAsyncFileStorageApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));

        await expect(files.presignedDownloadUrl({...plaintext(), isEncrypted: true})).rejects.toThrow(/download/i);
        await expect(files.presignedDownloadUrl({...plaintext(), isCompressed: true})).rejects.toThrow(/download/i);
        await expect(files.presignedUploadUrl({declaredMaxSizeBytes: 4, encrypt: true})).rejects.toThrow(/upload/i);
        await expect(files.presignedUploadUrl({declaredMaxSizeBytes: 4, compress: true})).rejects.toThrow(/upload/i);
        expect(calls).toEqual([]);

        const downloadUrl = await files.presignedDownloadUrl(plaintext());
        const upload = await files.presignedUploadUrl({declaredMaxSizeBytes: 4, originalFileName: "a.bin"});
        expect(downloadUrl).toBe("https://provider.example/get");
        expect(upload.presignedPutUrl).toBe("https://provider.example/put");
        expect(upload.fileId).toBe(fileId);
        expect(calls[0]).toBe(`http://x/FileStorage/files/${fileId}/presigned-read`);
        expect(calls[1]).toBe("http://x/FileStorage/direct-upload/begin");
    });
});

describe("download_usesApiNotPresignedRead", () => {
    it("returns the blob from the API download route", async () => {
        const transport: AsyncApiTransport = async (req) => {
            expect(req.url).toBe(`http://x/FileStorage/files/${fileId}/download`);
            expect(req.responseType).toBe("blob");
            return ok(undefined, {blob: new Blob(["plain"]), headers: {"content-disposition": 'attachment; filename="a.bin"', "content-type": "application/octet-stream"}});
        };
        const files = createAsyncFileStorageApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const saved = await files.download(fileId);
        expect(saved.fileName).toBe("a.bin");
        expect(saved.blob).toBeInstanceOf(Blob);
    });
});

describe("createFileStorageApiClient_syncDownload", () => {
    it("hits the same download route synchronously", () => {
        const transport: ApiTransport = (req) => {
            expect(req.method).toBe("GET");
            expect(req.url).toBe(`http://x/FileStorage/files/${fileId}/download`);
            return ok(undefined, {blob: new Blob(["plain"]), headers: {"content-disposition": 'attachment; filename="a.bin"'}});
        };
        const files = createFileStorageApiClient(createApiClient({baseUrl: "http://x", transport}));
        expect(files.download(fileId).fileName).toBe("a.bin");
    });
});

describe("integrity_and_content_digest", () => {
    it("gets the integrity route and reads Content-Digest from a download", async () => {
        const digest = "sha-256=:AQIDBA==:";
        const transport: AsyncApiTransport = async (req) => {
            if (req.url.endsWith("/integrity")) {
                return ok({hashAlgorithm: "sha256", originalFileHash: "AQIDBA==", originalFileSize: 4});
            }
            return ok(undefined, {
                blob: new Blob(["plain"]),
                headers: {"content-digest": digest, "content-disposition": 'attachment; filename="a.bin"'},
            });
        };
        const files = createAsyncFileStorageApiClient(createAsyncApiClient({baseUrl: "http://x", transport}));
        const integrity = await files.getIntegrity(fileId);
        const saved = await files.download(fileId);
        expect(integrity.data).toMatchObject({hashAlgorithm: "sha256", originalFileHash: "AQIDBA==", originalFileSize: 4});
        expect(saved.contentDigest).toBe(digest);
    });
});
