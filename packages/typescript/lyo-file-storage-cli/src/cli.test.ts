import {describe, expect, it} from "vitest";
import type {FileStoreResult} from "lyo-file-storage-api-client";
import {execute, parseArgv} from "./run.js";
import type {FileCliClient} from "./run.js";

const fileId = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";

function fakeClient(metadata: FileStoreResult): {client: FileCliClient; calls: string[]} {
    const calls: string[] = [];
    const client: FileCliClient = {
        saveStream: async () => {
            calls.push("saveStream");
            return {data: {id: fileId}};
        },
        download: async () => {
            calls.push("download");
            return {blob: new Blob(["plain"]), fileName: "a.bin", contentType: "application/octet-stream"};
        },
        getMetadata: async () => {
            calls.push("getMetadata");
            return {data: metadata};
        },
        presignedRead: async () => {
            calls.push("presignedRead");
            return {data: {url: "https://provider.example/get"}};
        },
        beginDirectUpload: async () => {
            calls.push("beginDirectUpload");
            return {data: {fileId, presignedPutUrl: "https://provider.example/put"}};
        },
        completeDirectUpload: async () => {
            calls.push("completeDirectUpload");
            return {data: {id: fileId}};
        },
        listFolder: async () => {
            calls.push("listFolder");
            return {data: {entries: [], truncated: false}};
        },
    };
    return {client, calls};
}

const io = {
    readFile: async () => new Blob(["hi"]),
    writeFile: async () => undefined,
    stdout: () => undefined,
};

describe("lyo-files argv", () => {
    it("uploads through the service when encrypt is set", async () => {
        const parsed = parseArgv(["upload", "./a.bin", "--prefix", "docs", "--encrypt", "--key-id", "k1"]);
        const {client, calls} = fakeClient({id: fileId, isEncrypted: false, isCompressed: false});
        await execute(parsed, client, io);
        expect(calls).toEqual(["saveStream"]);
        expect(calls).not.toContain("beginDirectUpload");
    });

    it("requests a presigned upload URL and does not upload the bytes", async () => {
        const lines: string[] = [];
        const parsed = parseArgv(["presign-put", "./a.bin", "--encrypt"]);
        const {client, calls} = fakeClient({id: fileId, isEncrypted: false, isCompressed: false});
        await execute(parsed, client, {...io, stdout: (line) => lines.push(line)});
        expect(calls).toEqual(["beginDirectUpload"]);
        expect(lines.join("\n")).toContain("https://provider.example/put");
        expect(lines.join("\n")).toContain(fileId);
    });

    it("downloads through the API even when the file is plaintext", async () => {
        const parsed = parseArgv(["download", fileId]);
        const {client, calls} = fakeClient({id: fileId, isEncrypted: false, isCompressed: false});
        await execute(parsed, client, io);
        expect(calls).toEqual(["download"]);
    });

    it("prints a presigned download URL without downloading", async () => {
        const lines: string[] = [];
        const parsed = parseArgv(["presign-get", fileId]);
        const {client, calls} = fakeClient({id: fileId, isEncrypted: true, isCompressed: true});
        await execute(parsed, client, {...io, stdout: (line) => lines.push(line)});
        expect(calls).toEqual(["presignedRead"]);
        expect(lines.join("\n")).toContain("https://provider.example/get");
    });

    it("lists a folder prefix", async () => {
        const parsed = parseArgv(["ls", "--prefix", "docs"]);
        const {client, calls} = fakeClient({id: fileId, isEncrypted: false, isCompressed: false});
        await execute(parsed, client, io);
        expect(calls).toEqual(["listFolder"]);
    });
});
