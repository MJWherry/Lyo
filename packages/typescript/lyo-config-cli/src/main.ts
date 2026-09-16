#!/usr/bin/env node
import {readFile} from "node:fs/promises";
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncConfigApiClient} from "lyo-config-api-client";
import {execute, parseArgv} from "./run.js";

const command = parseArgv(process.argv.slice(2));
const baseUrl = command.baseUrl ?? process.env.LYO_API_BASE_URL;
if (!baseUrl) throw new Error("--base-url or LYO_API_BASE_URL is required.");

const token = command.token ?? process.env.LYO_API_TOKEN;
const apiKey = command.apiKey ?? process.env.LYO_CONFIG_API_KEY;
const api = createAsyncApiClient({
    baseUrl,
    transport: fetchTransport,
    ...(token ? {token} : {}),
});
const config = createAsyncConfigApiClient(api, {...(apiKey ? {apiKey} : {})});

await execute(command, config, {
    readText: (path) => readFile(path, "utf8"),
    stdout: (line) => {
        process.stdout.write(`${line}\n`);
    },
});
