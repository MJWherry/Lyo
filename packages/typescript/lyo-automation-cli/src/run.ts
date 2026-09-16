import type {AsyncAutomationApiClient} from "lyo-automation-api-client";
import {createPlanFile, listPlanFiles, plansDirectory, showPlanFile} from "./plans.js";

export type Command =
    | {kind: "studio"; url?: string; out?: string; name?: string; plan?: string}
    | {kind: "plans-list"; dir: string}
    | {kind: "plans-create"; dir: string; name: string}
    | {kind: "plans-show"; dir: string; name: string}
    | {kind: "run"; file: string; api: string};

export type CommandIo = {
    readText: (file: string) => Promise<string>;
    stdout: (line: string) => void;
    client?: AsyncAutomationApiClient;
};

function flag(argv: string[], name: string): string | undefined {
    const index = argv.indexOf(name);
    const value = index >= 0 ? argv[index + 1] : undefined;
    return value && !value.startsWith("--") ? value : undefined;
}

/** `studio`, `plans list|create|show`, and `run <file> --api <baseUrl>`. */
export function parseArgv(argv: string[]): Command {
    const [command, sub, ...rest] = argv;
    if (command === "studio") {
        return {kind: "studio", url: flag(argv, "--url"), out: flag(argv, "--out"), name: flag(argv, "--name"), plan: flag(argv, "--plan")};
    }
    if (command === "plans" && sub === "list")
        return {kind: "plans-list", dir: plansDirectory(flag(rest, "--dir"))};
    if (command === "plans" && sub === "create" && rest[0] && !rest[0].startsWith("--"))
        return {kind: "plans-create", dir: plansDirectory(flag(rest, "--dir")), name: rest[0]};
    if (command === "plans" && sub === "show" && rest[0] && !rest[0].startsWith("--"))
        return {kind: "plans-show", dir: plansDirectory(flag(rest, "--dir")), name: rest[0]};
    if (command === "run" && sub && !sub.startsWith("--")) {
        const api = flag(argv, "--api");
        if (!api)
            throw new Error("run requires --api <baseUrl>.");
        return {kind: "run", file: sub, api};
    }
    throw new Error("Use studio, plans list|create|show, or run <file> --api <baseUrl>.");
}

/** Arguments for `dotnet run` of the studio. `--plan` is forwarded. */
export function studioDotnetArgs(project: string, flags: {url?: string; out?: string; name?: string; plan?: string}): string[] {
    const args = ["run", "--project", project, "--"];
    if (flags.url)
        args.push("--url", flags.url);
    if (flags.out)
        args.push("--out", flags.out);
    if (flags.name)
        args.push("--name", flags.name);
    if (flags.plan)
        args.push("--plan", flags.plan);
    return args;
}

/** Local plan commands and a remote run. Studio is started by the bin, not here. */
export async function execute(command: Command, io: CommandIo): Promise<void> {
    if (command.kind === "plans-list") {
        for (const plan of listPlanFiles(command.dir))
            io.stdout(`${plan.fileName}\t${plan.name}`);
        return;
    }
    if (command.kind === "plans-create") {
        io.stdout(createPlanFile(command.dir, command.name));
        return;
    }
    if (command.kind === "plans-show") {
        io.stdout(showPlanFile(command.dir, command.name).trimEnd());
        return;
    }
    if (command.kind === "run") {
        if (!io.client)
            throw new Error("A run client is required.");
        const plan = JSON.parse(await io.readText(command.file)) as unknown;
        const accepted = await io.client.start({plan, headless: true});
        const settled = await io.client.waitUntilSettled(accepted.runId, 0);
        io.stdout(settled.status);
        if (settled.error)
            io.stdout(settled.error);
        return;
    }
    throw new Error("studio is started from the bin.");
}
