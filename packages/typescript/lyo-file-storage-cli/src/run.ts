import {parseArgs} from "node:util";
import type {ExportDownload} from "lyo-api-client";
import type {FileStorageFolderListResponse, FileStoreResult} from "lyo-file-storage-api-client";

export interface FileCliClient {
    saveStream(request: {
        file: Blob;
        fileName?: string;
        pathPrefix?: string;
        compress?: boolean;
        encrypt?: boolean;
        keyId?: string;
    }): Promise<{data?: FileStoreResult}>;
    download(fileId: string): Promise<ExportDownload>;
    getMetadata(fileId: string): Promise<{data?: FileStoreResult}>;
    presignedRead(fileId: string): Promise<{data?: {url?: string}}>;
    beginDirectUpload(request: {
        declaredMaxSizeBytes: number;
        originalFileName?: string;
        pathPrefix?: string;
    }): Promise<{data?: {fileId?: string; presignedPutUrl?: string}}>;
    completeDirectUpload(fileId: string): Promise<unknown>;
    listFolder(query?: {pathPrefix?: string}): Promise<{data?: FileStorageFolderListResponse}>;
    health(): Promise<{data?: {isHealthy?: boolean; message?: string | null}}>;
    listKeyIds(): Promise<{data?: string[]}>;
    copy(request: {sourceFileId: string; pathPrefix?: string}): Promise<unknown>;
    move(request: {fileId: string; pathPrefix?: string}): Promise<unknown>;
    rename(request: {fileId: string; originalFileName: string}): Promise<unknown>;
    delete(fileId: string): Promise<unknown>;
    downloadArchive(ids: readonly string[], fileName?: string): Promise<ExportDownload>;
    createAccessLink(fileId: string): Promise<{data?: {downloadUrl?: string}}>;
}

export interface CliIo {
    readFile(path: string): Promise<Blob>;
    writeFile(path: string, body: Blob): Promise<void>;
    stdout(line: string): void;
}

export type FileCommand = {
    kind: string;
    positionals: string[];
    prefix?: string;
    encrypt: boolean;
    compress: boolean;
    keyId?: string;
    complete?: string;
    output?: string;
    name?: string;
    baseUrl?: string;
    token?: string;
    routePrefix?: string;
};

export function parseArgv(argv: string[]): FileCommand {
    const [kind, ...rest] = argv;
    if (!kind) throw new Error("Missing command. Try lyo-files --help.");
    const {values, positionals} = parseArgs({
        args: rest,
        allowPositionals: true,
        options: {
            prefix: {type: "string"},
            encrypt: {type: "boolean"},
            compress: {type: "boolean"},
            "key-id": {type: "string"},
            complete: {type: "string"},
            output: {type: "string", short: "o"},
            name: {type: "string"},
            "base-url": {type: "string"},
            token: {type: "string"},
            "route-prefix": {type: "string"},
        },
    });
    return {
        kind,
        positionals,
        prefix: values.prefix,
        encrypt: values.encrypt === true,
        compress: values.compress === true,
        keyId: values["key-id"],
        complete: values.complete,
        output: values.output,
        name: values.name,
        baseUrl: values["base-url"],
        token: values.token,
        routePrefix: values["route-prefix"],
    };
}

function fileNameOf(path: string): string {
    const parts = path.split(/[/\\]/);
    return parts[parts.length - 1] || "file.bin";
}

export async function execute(command: FileCommand, client: FileCliClient, io: CliIo): Promise<void> {
    switch (command.kind) {
        case "upload": {
            const path = command.positionals[0];
            if (!path) throw new Error("upload requires a file path.");
            const file = await io.readFile(path);
            await client.saveStream({
                file,
                fileName: fileNameOf(path),
                pathPrefix: command.prefix,
                compress: command.compress,
                encrypt: command.encrypt,
                keyId: command.keyId,
            });
            io.stdout(`Uploaded ${fileNameOf(path)}`);
            return;
        }
        case "presign-put": {
            if (command.complete) {
                await client.completeDirectUpload(command.complete);
                io.stdout(`Completed ${command.complete}`);
                return;
            }
            const path = command.positionals[0];
            if (!path) throw new Error("presign-put requires a file path.");
            const file = await io.readFile(path);
            const response = await client.beginDirectUpload({
                declaredMaxSizeBytes: file.size,
                originalFileName: fileNameOf(path),
                pathPrefix: command.prefix,
            });
            io.stdout(response.data?.presignedPutUrl ?? "");
            io.stdout(response.data?.fileId ?? "");
            return;
        }
        case "download": {
            const id = command.positionals[0];
            if (!id) throw new Error("download requires a file id.");
            const saved = await client.download(id);
            await io.writeFile(command.output ?? saved.fileName ?? id, saved.blob);
            return;
        }
        case "presign-get": {
            const id = command.positionals[0];
            if (!id) throw new Error("presign-get requires a file id.");
            const response = await client.presignedRead(id);
            io.stdout(response.data?.url ?? "");
            return;
        }
        case "ls": {
            const response = await client.listFolder({pathPrefix: command.prefix});
            for (const entry of response.data?.entries ?? []) io.stdout(`${entry.isDirectory ? "d" : "f"} ${entry.name}`);
            return;
        }
        case "health": {
            const response = await client.health();
            io.stdout(response.data?.isHealthy ? "healthy" : response.data?.message ?? "unhealthy");
            return;
        }
        case "keys": {
            const response = await client.listKeyIds();
            for (const id of response.data ?? []) io.stdout(id);
            return;
        }
        case "metadata": {
            const id = command.positionals[0];
            if (!id) throw new Error("metadata requires a file id.");
            const response = await client.getMetadata(id);
            io.stdout(JSON.stringify(response.data ?? null));
            return;
        }
        case "move": {
            const id = command.positionals[0];
            if (!id) throw new Error("move requires a file id.");
            await client.move({fileId: id, pathPrefix: command.prefix});
            return;
        }
        case "copy": {
            const id = command.positionals[0];
            if (!id) throw new Error("copy requires a file id.");
            await client.copy({sourceFileId: id, pathPrefix: command.prefix});
            return;
        }
        case "rename": {
            const id = command.positionals[0];
            if (!id || !command.name) throw new Error("rename requires a file id and --name.");
            await client.rename({fileId: id, originalFileName: command.name});
            return;
        }
        case "delete": {
            const id = command.positionals[0];
            if (!id) throw new Error("delete requires a file id.");
            await client.delete(id);
            return;
        }
        case "archive": {
            if (command.positionals.length === 0) throw new Error("archive requires at least one file id.");
            const saved = await client.downloadArchive(command.positionals, command.output);
            await io.writeFile(command.output ?? saved.fileName ?? "files.zip", saved.blob);
            return;
        }
        case "link": {
            const id = command.positionals[0];
            if (!id) throw new Error("link requires a file id.");
            const response = await client.createAccessLink(id);
            io.stdout(response.data?.downloadUrl ?? "");
            return;
        }
        default:
            throw new Error(`Unknown command '${command.kind}'.`);
    }
}
