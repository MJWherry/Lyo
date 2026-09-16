import type {FileStorageFolderEntry, FileStoreResult} from "lyo-file-storage-api-client";
import type {WhereClause} from "lyo-query";
import {chipOf, LyoChipHueList, LyoChipHues, type LyoChipColor, type LyoChipSpec} from "lyo-web-components";

export type PresenceLabel = "None" | "Store" | "Physical" | "Both";

export interface FileTreeNode {
    name: string;
    isDirectory: boolean;
    pathPrefix: string | null;
    fileId: string | null;
    presence: PresenceLabel;
    originalFileName: string | null;
    originalFileSize: number;
    physicalKey: string | null;
}

export function presenceLabel(presence: string | number | null | undefined): PresenceLabel {
    if (presence === 1 || presence === "1" || presence === "store" || presence === "Store") return "Store";
    if (presence === 2 || presence === "2" || presence === "physical" || presence === "Physical") return "Physical";
    if (presence === 3 || presence === "3" || presence === "both" || presence === "Both") return "Both";
    return "None";
}

export function joinPathPrefix(parent: string | null | undefined, name: string): string {
    const base = (parent ?? "").replace(/\/+$/, "");
    const child = name.replace(/^\/+/, "");
    return base ? `${base}/${child}` : child;
}

export type FileGlyph = "folder" | "pdf" | "table" | "image" | "document" | "code" | "audio" | "video" | "archive" | "file";

const GLYPH_EXTENSIONS: Record<Exclude<FileGlyph, "folder" | "file">, readonly string[]> = {
    pdf: [".pdf"],
    table: [".csv", ".xls", ".xlsx"],
    image: [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"],
    document: [".doc", ".docx", ".txt", ".rtf"],
    code: [".cs", ".ts", ".tsx", ".js", ".jsx", ".json", ".xml", ".yaml", ".yml", ".html", ".htm", ".css", ".py", ".java", ".go", ".rs"],
    audio: [".mp3", ".wav"],
    video: [".mp4"],
    archive: [".zip", ".gz", ".tgz", ".7z"],
};

/** Row identity. Files use the catalog id so the same filename can appear twice. Folders stay on their path. A backend-only object uses its physical key. */
export function treeNodeKey(node: FileTreeNode): string {
    if (!node.isDirectory && node.fileId) return `f:${node.fileId}`;
    if (!node.isDirectory && node.physicalKey) return `p:${node.physicalKey}`;
    return `${node.isDirectory ? "d" : "f"}:${node.pathPrefix ?? ""}/${node.name}`;
}

/** Icon kind for a tree row. Directories are folders; files follow the extension, same idea as `FileStorageTreeIcons`. */
export function fileGlyph(name: string, isDirectory: boolean): FileGlyph {
    if (isDirectory) return "folder";
    const ext = extensionOf(name);
    if (!ext) return "file";
    for (const [glyph, extensions] of Object.entries(GLYPH_EXTENSIONS) as [Exclude<FileGlyph, "folder" | "file">, readonly string[]][]) {
        if (extensions.includes(ext)) return glyph;
    }
    return "file";
}

/**
 * Rows that match `query`, plus folders whose already-loaded children match.
 * An empty query returns every node. Unloaded folders match on their own name only.
 */
export function visibleTreeNodes(
    nodes: readonly FileTreeNode[],
    query: string,
    children: Readonly<Record<string, readonly FileTreeNode[]>>
): FileTreeNode[] {
    const needle = query.trim().toLowerCase();
    if (!needle) return [...nodes];
    return nodes.filter((node) => {
        const label = `${node.name} ${node.originalFileName ?? ""}`.toLowerCase();
        if (label.includes(needle)) return true;
        if (!node.isDirectory) return false;
        const kids = children[treeNodeKey(node)];
        return kids ? visibleTreeNodes(kids, needle, children).length > 0 : false;
    });
}

export function folderEntriesToNodes(entries: readonly FileStorageFolderEntry[], parentPrefix?: string | null): FileTreeNode[] {
    const nodes = entries.map((entry) => ({
        name: entry.name,
        isDirectory: entry.isDirectory,
        pathPrefix: entry.pathPrefix ?? (entry.isDirectory ? joinPathPrefix(parentPrefix, entry.name) : parentPrefix ?? null),
        fileId: entry.fileId ?? null,
        presence: presenceLabel(entry.presence),
        originalFileName: entry.originalFileName ?? null,
        originalFileSize: entry.originalFileSize ?? 0,
        physicalKey: entry.physicalKey ?? null,
    }));
    return [...nodes.filter((node) => node.isDirectory), ...nodes.filter((node) => !node.isDirectory)];
}

/** QueryProject filter for rows that are not tombstones. */
export function activeFilesWhere(): WhereClause {
    return {
        $type: "group",
        Operator: "And",
        Children: [
            {$type: "condition", Field: "DeletedAt", Comparison: "Equals", Value: null},
            {$type: "condition", Field: "Availability", Comparison: "NotEquals", Value: "Deleted"},
        ],
    };
}

export function showPresignedDownload(file: {isEncrypted?: boolean; isCompressed?: boolean}): boolean {
    return file.isEncrypted !== true && file.isCompressed !== true;
}

export function showPresignedUpload(flags: {compress?: boolean; encrypt?: boolean}): boolean {
    return flags.compress !== true && flags.encrypt !== true;
}

const SIZE_UNITS = [
    {abbr: "B", mult: 1},
    {abbr: "KB", mult: 1024},
    {abbr: "MB", mult: 1024 ** 2},
    {abbr: "GB", mult: 1024 ** 3},
    {abbr: "TB", mult: 1024 ** 4},
    {abbr: "PB", mult: 1024 ** 5},
    {abbr: "EB", mult: 1024 ** 6},
] as const;

const FILE_TYPES: readonly {name: string; mimes: readonly string[]; exts: readonly string[]}[] = [
    {name: "PDF", mimes: ["application/pdf"], exts: [".pdf"]},
    {name: "DOC", mimes: ["application/msword"], exts: [".doc"]},
    {name: "DOCX", mimes: ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"], exts: [".docx"]},
    {name: "XLS", mimes: ["application/vnd.ms-excel"], exts: [".xls"]},
    {name: "XLSX", mimes: ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"], exts: [".xlsx"]},
    {name: "CSV", mimes: ["text/csv"], exts: [".csv"]},
    {name: "TXT", mimes: ["text/plain"], exts: [".txt"]},
    {name: "JSON", mimes: ["application/json"], exts: [".json"]},
    {name: "XML", mimes: ["application/xml", "text/xml"], exts: [".xml"]},
    {name: "YAML", mimes: ["application/yaml", "text/yaml", "text/x-yaml"], exts: [".yaml", ".yml"]},
    {name: "HTML", mimes: ["text/html"], exts: [".html", ".htm"]},
    {name: "JavaScript", mimes: ["text/javascript", "application/javascript"], exts: [".js", ".mjs"]},
    {name: "PNG", mimes: ["image/png"], exts: [".png"]},
    {name: "JPEG", mimes: ["image/jpeg"], exts: [".jpg", ".jpeg"]},
    {name: "GIF", mimes: ["image/gif"], exts: [".gif"]},
    {name: "SVG", mimes: ["image/svg+xml"], exts: [".svg"]},
    {name: "WEBP", mimes: ["image/webp"], exts: [".webp"]},
    {name: "ZIP", mimes: ["application/zip"], exts: [".zip"]},
    {name: "GZ", mimes: ["application/gzip"], exts: [".gz"]},
    {name: "MP3", mimes: ["audio/mpeg"], exts: [".mp3"]},
    {name: "MP4", mimes: ["video/mp4"], exts: [".mp4"]},
    {name: "WAV", mimes: ["audio/wav"], exts: [".wav"]},
];

export type PresenceChipIcon = "both" | "catalog" | "physical";

export interface PresenceChip {
    label: string;
    hint: string;
    color: LyoChipColor;
    icon: PresenceChipIcon;
}

export interface MetadataRow {
    label: string;
    value: string;
}

export type AlgorithmChipIcon = "lock" | "lockOpen" | "compress";

export interface AlgorithmChip {
    spec: LyoChipSpec;
    icon?: AlgorithmChipIcon;
}

/** Short backend name used in presence chips. Matches `FileStorageBackend.DisplayName`. */
export function backendDisplayName(kind: string | number | null | undefined): string {
    const text = String(kind ?? "").replace(/[\s_-]/g, "").toLowerCase();
    if (text === "1" || text === "local") return "Local";
    if (text === "2" || text === "awss3" || text === "s3") return "S3";
    if (text === "3" || text === "azureblob") return "Azure Blob";
    if (text === "4" || text === "other") return "object store";
    return "storage";
}

/** Filled presence chip for a file row. Directories and unknown presence have no chip. */
export function presenceChip(presence: PresenceLabel, backendKind: string | number | null | undefined): PresenceChip | null {
    const kind = backendDisplayName(backendKind);
    switch (presence) {
        case "Both":
            return {label: "Both", hint: `In the catalog and on ${kind}`, color: "success", icon: "both"};
        case "Store":
            return {label: "Catalog only", hint: `In the catalog; no object on ${kind}`, color: "error", icon: "catalog"};
        case "Physical":
            return {label: `${kind} only`, hint: `On ${kind}; not in the catalog`, color: "info", icon: "physical"};
        default:
            return null;
    }
}

/** Binary size label matching `FileSizeUnitInfo.FormatBestFit`, for example `39.49 KB`. */
export function formatFileSize(bytes: number | null | undefined): string {
    const size = bytes == null || !Number.isFinite(bytes) ? 0 : bytes;
    const unit = size <= 0 ? SIZE_UNITS[0] : [...SIZE_UNITS].reverse().find((item) => size >= item.mult) ?? SIZE_UNITS[0];
    if (unit.abbr === "B") return `${Math.max(size, 0)} B`;
    const scaled = Math.round((size / unit.mult) * 100) / 100;
    return `${trimFraction(scaled.toFixed(2))} ${unit.abbr}`;
}

/** Label/value rows for `FileStoreMetadataTable`. */
export function metadataRows(metadata: FileStoreResult): MetadataRow[] {
    return [
        {label: "Id", value: metadata.id},
        {label: "Status", value: metadata.deletedAt ? `Deleted (${formatUtcTimestamp(metadata.deletedAt)})` : "Active"},
        {label: "Original name", value: metadata.originalFileName ?? "-"},
        {label: "Original size", value: formatFileSize(metadata.originalFileSize ?? 0)},
        {label: "Stored source", value: metadata.sourceFileName ?? "-"},
        {label: "Stored size", value: formatFileSize(metadata.sourceFileSize ?? 0)},
        {label: "Compressed", value: metadata.isCompressed ? metadata.compressionAlgorithm || "Yes" : "No"},
        {
            label: "Encrypted",
            value: metadata.isEncrypted ? `${metadata.dataEncryptionKeyId ?? ""} / ${metadata.dataEncryptionKeyVersion ?? ""}` : "No",
        },
        {label: "Path prefix", value: metadata.pathPrefix ?? "-"},
        {label: "Timestamp", value: formatUtcTimestamp(metadata.timestamp)},
        {label: "Hash algorithm", value: metadata.hashAlgorithm ?? "-"},
        {label: "Original hash", value: formatHash(metadata.originalFileHash)},
        {label: "Stored hash", value: formatHash(metadata.sourceFileHash)},
        {label: "Compressed size", value: formatOptionalSize(metadata.compressedFileSize)},
        {label: "Compressed hash", value: formatHash(metadata.compressedFileHash)},
        {label: "Data key algorithm", value: metadata.dataEncryptionKeyAlgorithm ?? "-"},
        {label: "Key encryption algorithm", value: metadata.keyEncryptionKeyAlgorithm ?? "-"},
        {label: "Encrypted size", value: formatOptionalSize(metadata.encryptedFileSize)},
        {label: "Encrypted hash", value: formatHash(metadata.encryptedFileHash)},
        {label: "KEK salt", value: formatHash(metadata.keyEncryptionKeySalt)},
        {label: "Metadata", value: formatMetadata(metadata.metadata)},
    ];
}

/** File-type chip from MIME, then filename. Unknown types keep the raw media type. */
export function fileTypeChip(contentType: string | null | undefined, fileName?: string | null): LyoChipSpec {
    const mime = mediaTypeOnly(contentType);
    const name = resolveFileTypeName(mime, fileName);
    if (!name) return chipOf(mime || "File");
    return chipOf(name, {hue: hueForTypeName(name)});
}

/** DEK algorithm chip. A blank algorithm is `None`. */
export function encryptionChip(algorithm: string | null | undefined): AlgorithmChip {
    if (!algorithm?.trim()) return {spec: chipOf("None"), icon: "lockOpen"};
    const hue = encryptionHue(normalizeAlgorithm(algorithm));
    return {spec: chipOf(algorithm, hue == null ? {} : {hue}), icon: "lock"};
}

/** Compression algorithm chip. A blank algorithm is `None`. */
export function compressionChip(algorithm: string | null | undefined): AlgorithmChip {
    if (!algorithm?.trim()) return {spec: chipOf("None")};
    const hue = compressionHue(normalizeAlgorithm(algorithm));
    return {spec: chipOf(algorithm, hue == null ? {} : {hue}), icon: "compress"};
}

function encryptionHue(algorithm: string): number | undefined {
    switch (algorithm) {
        case "AESGCM":
        case "AESGCMRSA":
        case "AESCCM":
        case "AESSIV":
            return LyoChipHues.Blue;
        case "CHACHA20POLY1305":
        case "XCHACHA20POLY1305":
            return LyoChipHues.Teal;
        case "RSA":
            return LyoChipHues.Violet;
        default:
            return undefined;
    }
}

function compressionHue(algorithm: string): number | undefined {
    switch (algorithm) {
        case "BROTLI":
            return LyoChipHues.Lime;
        case "GZIP":
            return LyoChipHues.Amber;
        case "DEFLATE":
        case "ZLIB":
            return LyoChipHues.Cyan;
        case "LZ4":
            return LyoChipHues.Coral;
        default:
            return undefined;
    }
}

function normalizeAlgorithm(value: string): string {
    return value.replace(/[-_]/g, "").toUpperCase();
}

function mediaTypeOnly(contentType: string | null | undefined): string {
    if (!contentType?.trim()) return "";
    const semi = contentType.indexOf(";");
    return (semi < 0 ? contentType : contentType.slice(0, semi)).trim();
}

function resolveFileTypeName(mime: string, fileName: string | null | undefined): string | null {
    const media = mime.toLowerCase();
    if (media) {
        const byMime = FILE_TYPES.find((type) => type.mimes.some((item) => item === media));
        if (byMime) return byMime.name;
    }
    const ext = extensionOf(fileName);
    if (!ext) return null;
    return FILE_TYPES.find((type) => type.exts.includes(ext))?.name ?? null;
}

function extensionOf(fileName: string | null | undefined): string | null {
    if (!fileName) return null;
    const base = fileName.split(/[/\\]/).pop() ?? fileName;
    const dot = base.lastIndexOf(".");
    if (dot <= 0) return null;
    return base.slice(dot).toLowerCase();
}

function hueForTypeName(name: string): number {
    let sum = 0;
    for (const ch of name) sum += ch.charCodeAt(0);
    return LyoChipHueList[(sum * 5) % LyoChipHueList.length]!;
}

function trimFraction(text: string): string {
    return text.replace(/\.?0+$/, "");
}

function formatOptionalSize(size: number | null | undefined): string {
    return size == null ? "-" : formatFileSize(size);
}

function formatUtcTimestamp(value: string | null | undefined): string {
    if (!value) return "-";
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return value;
    const pad = (part: number) => String(part).padStart(2, "0");
    return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())} ${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}:${pad(date.getUTCSeconds())}Z`;
}

function formatHash(value: string | null | undefined): string {
    if (!value) return "-";
    try {
        const binary = atob(value);
        if (!binary) return "-";
        return Array.from(binary, (ch) => ch.charCodeAt(0).toString(16).padStart(2, "0")).join("").toUpperCase();
    } catch {
        return value;
    }
}

function formatMetadata(metadata: unknown): string {
    if (metadata == null) return "-";
    if (typeof metadata === "string" && !metadata.trim()) return "-";
    return JSON.stringify(metadata, null, 2);
}

export function projectedBool(row: Record<string, unknown>, field: string): boolean {
    const value = row[field] ?? row[field.charAt(0).toLowerCase() + field.slice(1)];
    return value === true || value === "true" || value === "True";
}
