import {createLyoColumn, type LyoColumn} from "lyo-web-components";

export function definitionColumns(): LyoColumn<Record<string, unknown>>[] {
    return [
        createLyoColumn({id: "key", field: "Key", header: "Key", quickSearch: true}),
        createLyoColumn({id: "type", field: "ForValueType", header: "Type"}),
        createLyoColumn({id: "required", field: "IsRequired", header: "Required"}),
        createLyoColumn({id: "encrypted", field: "IsEncrypted", header: "Encrypted"}),
    ];
}
