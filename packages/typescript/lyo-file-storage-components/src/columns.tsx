"use client";

import CompressIcon from "@mui/icons-material/Compress";
import LockIcon from "@mui/icons-material/Lock";
import LockOpenIcon from "@mui/icons-material/LockOpen";
import {LyoChip, createLyoColumn, type LyoColumn} from "lyo-web-components";
import {compressionChip, encryptionChip, fileTypeChip, formatFileSize, type AlgorithmChipIcon} from "./fileStorageUi.js";

type Row = Record<string, unknown>;

function fieldValue(row: Row, field: string): unknown {
    return row[field] ?? row[field.charAt(0)!.toLowerCase() + field.slice(1)];
}

function text(row: Row, field: string): string {
    const value = fieldValue(row, field);
    return value == null ? "" : String(value);
}

function textOrNull(row: Row, field: string): string | null {
    const value = text(row, field).trim();
    return value ? value : null;
}

function algorithmIcon(icon: AlgorithmChipIcon | undefined) {
    if (icon === "lock") return <LockIcon />;
    if (icon === "lockOpen") return <LockOpenIcon />;
    if (icon === "compress") return <CompressIcon />;
    return undefined;
}

export function fileMetadataColumns(): LyoColumn<Row>[] {
    return [
        createLyoColumn({id: "name", field: "OriginalFileName", header: "Name", size: 220, cell: (row) => text(row, "OriginalFileName")}),
        createLyoColumn({
            id: "size",
            field: "OriginalFileSize",
            header: "Original size",
            size: 120,
            cell: (row) => {
                const raw = fieldValue(row, "OriginalFileSize");
                const size = typeof raw === "number" ? raw : Number(raw);
                return Number.isFinite(size) ? formatFileSize(size) : "";
            },
        }),
        createLyoColumn({id: "prefix", field: "PathPrefix", header: "Path prefix", size: 180, cell: (row) => text(row, "PathPrefix")}),
        createLyoColumn({
            id: "type",
            field: "ContentType",
            header: "Type",
            size: 140,
            cell: (row) => <LyoChip spec={fileTypeChip(textOrNull(row, "ContentType"), textOrNull(row, "OriginalFileName"))} />,
        }),
        createLyoColumn({
            id: "encryption",
            field: "DataEncryptionKeyAlgorithm",
            header: "Encryption",
            size: 160,
            cell: (row) => {
                const chip = encryptionChip(textOrNull(row, "DataEncryptionKeyAlgorithm"));
                return <LyoChip spec={chip.spec} icon={algorithmIcon(chip.icon)} />;
            },
        }),
        createLyoColumn({
            id: "compression",
            field: "CompressionAlgorithm",
            header: "Compression",
            size: 150,
            cell: (row) => {
                const chip = compressionChip(textOrNull(row, "CompressionAlgorithm"));
                return <LyoChip spec={chip.spec} icon={algorithmIcon(chip.icon)} />;
            },
        }),
    ];
}
