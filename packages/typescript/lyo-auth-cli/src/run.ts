import {parseArgs} from "node:util";
import type {AsyncAuthApiClient} from "lyo-auth-api-client";

export interface CliIo {
    stdout(line: string): void;
}

export type AuthCommand = {
    kind: string;
    positionals: string[];
    name?: string;
    scopes?: string;
    baseUrl?: string;
    token?: string;
};

export function parseArgv(argv: string[]): AuthCommand {
    const [kind, ...rest] = argv;
    if (!kind) throw new Error("Missing command. Try lyo-auth --help.");
    const {values, positionals} = parseArgs({
        args: rest,
        allowPositionals: true,
        options: {
            name: {type: "string"},
            scopes: {type: "string"},
            "base-url": {type: "string"},
            token: {type: "string"},
        },
    });
    return {
        kind,
        positionals,
        name: values.name,
        scopes: values.scopes,
        baseUrl: values["base-url"],
        token: values.token,
    };
}

export async function execute(command: AuthCommand, client: AsyncAuthApiClient, io: CliIo): Promise<void> {
    if (command.kind === "me") {
        io.stdout(JSON.stringify((await client.me()).data ?? null));
        return;
    }
    if (command.kind !== "tokens") throw new Error(`Unknown command '${command.kind}'.`);
    const action = command.positionals[0];
    if (action === "list") {
        const rows = (await client.tokens.list()).data ?? [];
        for (const row of rows) io.stdout(JSON.stringify(row));
        return;
    }
    if (action === "create") {
        if (!command.name) throw new Error("tokens create requires --name.");
        const created = await client.tokens.create({
            displayName: command.name,
            scopes: command.scopes?.split(",").map((scope) => scope.trim()).filter(Boolean),
        });
        io.stdout(created.data?.plaintext ?? "");
        return;
    }
    if (action === "revoke") {
        const id = command.positionals[1];
        if (!id) throw new Error("tokens revoke requires an id.");
        await client.tokens.revoke(id);
        io.stdout(`Revoked ${id}`);
        return;
    }
    throw new Error("tokens requires list, create, or revoke.");
}
