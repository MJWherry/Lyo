import {parseArgs} from "node:util";
import type {AsyncConfigApiClient, ConfigBindingRecord, ConfigDefinitionRecord} from "lyo-config-api-client";

export interface CliIo {
    readText(path: string): Promise<string>;
    stdout(line: string): void;
}

export type ConfigCommand = {
    kind: string;
    positionals: string[];
    type?: string;
    id?: string;
    key?: string;
    value?: string;
    encrypted: boolean;
    definitionId?: string;
    revision?: number;
    target?: string;
    baseUrl?: string;
    token?: string;
    apiKey?: string;
};

export function parseArgv(argv: string[]): ConfigCommand {
    const [kind, ...rest] = argv;
    if (!kind) throw new Error("Missing command. Try lyo-config --help.");
    const {values, positionals} = parseArgs({
        args: rest,
        allowPositionals: true,
        options: {
            type: {type: "string"},
            id: {type: "string"},
            key: {type: "string"},
            value: {type: "string"},
            encrypted: {type: "boolean"},
            "definition-id": {type: "string"},
            revision: {type: "string"},
            target: {type: "string"},
            "base-url": {type: "string"},
            token: {type: "string"},
            "api-key": {type: "string"},
        },
    });
    const revision = values.revision === undefined ? undefined : Number(values.revision);
    if (revision !== undefined && Number.isNaN(revision)) throw new Error("--revision must be a number.");
    return {
        kind,
        positionals,
        type: values.type,
        id: values.id,
        key: values.key,
        value: values.value,
        encrypted: values.encrypted === true,
        definitionId: values["definition-id"],
        revision,
        target: values.target,
        baseUrl: values["base-url"],
        token: values.token,
        apiKey: values["api-key"],
    };
}

function requirePair(command: ConfigCommand): {type: string; id: string} {
    if (!command.type || !command.id) throw new Error("--type and --id are required.");
    return {type: command.type, id: command.id};
}

function print(io: CliIo, value: unknown) {
    io.stdout(JSON.stringify(value ?? null, null, 2));
}

export async function execute(command: ConfigCommand, client: AsyncConfigApiClient, io: CliIo): Promise<void> {
    switch (command.kind) {
        case "get": {
            const kind = command.positionals[0];
            const id = command.positionals[1];
            if (!kind || !id) throw new Error("get requires <kind> <id>.");
            const result = await client.resolve(kind, id);
            if (result.outcome === 304) {
                io.stdout("304");
                return;
            }
            print(io, result.record);
            return;
        }
        case "definitions": {
            const response = await client.listDefinitions(command.type);
            print(io, response.data);
            return;
        }
        case "put-definition": {
            const path = command.positionals[0];
            if (!path) throw new Error("put-definition requires a JSON file.");
            const body = JSON.parse(await io.readText(path)) as ConfigDefinitionRecord;
            const response = await client.putDefinition(body);
            print(io, response.data);
            return;
        }
        case "delete-definition": {
            const id = command.positionals[0];
            if (!id) throw new Error("delete-definition requires an id.");
            await client.deleteDefinition(id);
            io.stdout(`Deleted definition ${id}`);
            return;
        }
        case "bindings": {
            const pair = requirePair(command);
            const response = await client.listBindings(pair.type, pair.id);
            print(io, response.data);
            return;
        }
        case "set": {
            if (!command.type || !command.id || !command.key || command.value === undefined)
                throw new Error("set requires --type, --id, --key, and --value.");
            let definitionId = command.definitionId;
            if (!definitionId) {
                const found = await client.getDefinitionByKey(command.type, command.key);
                definitionId = found.data?.id;
                if (!definitionId) throw new Error(`No definition for key '${command.key}'.`);
                if (command.encrypted && found.data && found.data.isEncrypted !== true) {
                    await client.putDefinition({...found.data, isEncrypted: true});
                }
            }
            const binding: ConfigBindingRecord = {
                definitionId,
                key: command.key,
                subjectEntityType: command.type,
                subjectEntityId: command.id,
                value: {typeName: "System.String", json: JSON.stringify(command.value)},
            };
            const response = await client.putBinding(binding);
            print(io, response.data);
            return;
        }
        case "delete-binding": {
            const id = command.positionals[0];
            if (!id) throw new Error("delete-binding requires an id.");
            await client.deleteBinding(id);
            io.stdout(`Deleted binding ${id}`);
            return;
        }
        case "revisions": {
            const kind = command.target ?? command.positionals[0];
            const entityId = command.target ? command.positionals[0] : command.positionals[1];
            if (!kind || !entityId) throw new Error("revisions requires <definition|binding> <id>.");
            if (kind === "definition") print(io, (await client.definitionRevisions(entityId)).data);
            else if (kind === "binding") print(io, (await client.bindingRevisions(entityId)).data);
            else throw new Error("revisions target must be definition or binding.");
            return;
        }
        case "revert": {
            const kind = command.target ?? command.positionals[0];
            const entityId = command.target ? command.positionals[0] : command.positionals[1];
            if (!kind || !entityId || command.revision === undefined) throw new Error("revert requires <definition|binding> <id> --revision N.");
            if (kind === "definition") print(io, (await client.revertDefinition(entityId, command.revision)).data);
            else if (kind === "binding") print(io, (await client.revertBinding(entityId, command.revision)).data);
            else throw new Error("revert target must be definition or binding.");
            return;
        }
        default:
            throw new Error(`Unknown command '${command.kind}'.`);
    }
}
