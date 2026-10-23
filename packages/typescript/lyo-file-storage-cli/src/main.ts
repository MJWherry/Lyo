#!/usr/bin/env node
import {readFile, writeFile} from "node:fs/promises";
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncFileStorageApiClient} from "lyo-file-storage-api-client";
import {execute, parseArgv, type FileCliClient} from "./run.js";

const command = parseArgv(process.argv.slice(2));
const baseUrl = command.baseUrl ?? process.env.LYO_API_BASE_URL;
if (!baseUrl) throw new Error("--base-url or LYO_API_BASE_URL is required.");

const token = command.token ?? process.env.LYO_API_TOKEN;
const api = createAsyncApiClient({
    baseUrl,
    transport: fetchTransport,
    ...(token ? {token} : {}),
});
const files = createAsyncFileStorageApiClient(api, {routePrefix: command.routePrefix});
const client: FileCliClient = files;

await execute(command, client, {
    readFile: async (path) => new Blob([await readFile(path)]),
    writeFile: async (path, body) => {
        await writeFile(path, Buffer.from(await body.arrayBuffer()));
    },
    stdout: (line) => {
        process.stdout.write(`${line}\n`);
    },
});
