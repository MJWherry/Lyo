#!/usr/bin/env node
import {spawn} from "node:child_process";
import {readFile} from "node:fs/promises";
import path from "node:path";
import {createAsyncApiClient, fetchTransport} from "lyo-api-client";
import {createAsyncAutomationApiClient} from "lyo-automation-api-client";
import {startPlanPage} from "./pageServer.js";
import {execute, parseArgv, studioDotnetArgs} from "./run.js";
import {findStudioProject} from "./studio.js";

const command = parseArgv(process.argv.slice(2));
if (command.kind === "studio") {
    const project = findStudioProject();
    const plansDir = command.out && command.out.toLowerCase().endsWith(".json") ? path.dirname(path.resolve(command.out)) : path.resolve(command.out ?? "plans");
    const page = await startPlanPage({plansDir});
    process.stdout.write(`Plan page ${page.url}\n`);
    const opener = process.platform === "darwin" ? "open" : "xdg-open";
    spawn(opener, [page.url], {stdio: "ignore", detached: true}).unref();
    const child = spawn("dotnet", studioDotnetArgs(project, command), {stdio: "inherit"});
    const code = await new Promise<number>((resolve) => {
        child.on("exit", (status) => resolve(status ?? 1));
    });
    await page.close();
    process.exit(code);
}

const client = command.kind === "run"
    ? createAsyncAutomationApiClient(createAsyncApiClient({baseUrl: command.api, transport: fetchTransport}))
    : undefined;

await execute(command, {
    readText: (file) => readFile(file, "utf8"),
    stdout: (line) => {
        process.stdout.write(`${line}\n`);
    },
    client,
});
