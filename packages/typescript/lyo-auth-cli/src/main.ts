#!/usr/bin/env node
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncAuthApiClient} from "lyo-auth-api-client";
import {execute, parseArgv} from "./run.js";

const command = parseArgv(process.argv.slice(2));
const baseUrl = command.baseUrl ?? process.env.LYO_API_BASE_URL;
if (!baseUrl) throw new Error("--base-url or LYO_API_BASE_URL is required.");

const token = command.token ?? process.env.LYO_API_TOKEN;
const api = createAsyncApiClient({
    baseUrl,
    transport: fetchTransport,
    ...(token ? {token} : {}),
});

await execute(command, createAsyncAuthApiClient(api), {
    stdout: (line) => {
        process.stdout.write(`${line}\n`);
    },
});
