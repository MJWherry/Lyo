import {parseArgs} from "node:util";
import type {AsyncImportApiClient, ImportFormat, ImportJsonMode, ImportResponse} from "lyo-import-api-client";

const FORMATS = ["Json", "Csv", "Xlsx", "Yaml", "Xml", "Toml"] as const;

export interface CliIo {
    readFile(path: string): Promise<Uint8Array>;
    stdout(line: string): void;
}

export type ImportCommand = {
    file?: string;
    format: ImportFormat;
    jsonMode?: ImportJsonMode;
    rowsPath?: string;
    sheetName?: string;
    baseUrl?: string;
    token?: string;
};

export function parseArgv(argv: string[]): ImportCommand {
    const {values, positionals} = parseArgs({
        args: argv,
        allowPositionals: true,
        options: {
            format: {type: "string"},
            "json-mode": {type: "string"},
            "rows-path": {type: "string"},
            sheet: {type: "string"},
            "base-url": {type: "string"},
            token: {type: "string"},
        },
    });
    const format = values.format ?? "Json";
    if (!FORMATS.includes(format as ImportFormat)) throw new Error(`Unknown format '${format}'.`);
    return {
        file: positionals[0],
        format: format as ImportFormat,
        jsonMode: values["json-mode"] as ImportJsonMode | undefined,
        rowsPath: values["rows-path"],
        sheetName: values.sheet,
        baseUrl: values["base-url"],
        token: values.token,
    };
}

function bytesToBase64(bytes: Uint8Array): string {
    let binary = "";
    for (const byte of bytes) binary += String.fromCharCode(byte);
    return btoa(binary);
}

export async function execute(command: ImportCommand, client: AsyncImportApiClient, io: CliIo): Promise<ImportResponse | undefined> {
    if (!command.file) throw new Error("A file path is required.");
    const bytes = await io.readFile(command.file);
    const text = new TextDecoder().decode(bytes);
    const response = await client.postImport({
        format: command.format,
        ...(command.jsonMode ? {jsonMode: command.jsonMode} : {}),
        ...(command.rowsPath ? {rowsPath: command.rowsPath} : {}),
        ...(command.sheetName ? {sheetName: command.sheetName} : {}),
        ...(command.format === "Xlsx" ? {contentBytes: bytesToBase64(bytes)} : {content: text}),
    });
    const body = response.data;
    io.stdout(body?.rowsJson ?? "[]");
    for (const issue of body?.issues ?? []) io.stdout(`${issue.index}: ${issue.message}`);
    return body;
}
