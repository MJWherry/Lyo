import {describe, expect, it} from "vitest";
import type {FileStoreResult} from "lyo-file-storage-api-client";
import {
    activeFilesWhere,
    backendDisplayName,
    compressionChip,
    encryptionChip,
    fileGlyph,
    selectedStoreRoute,
    fileTypeChip,
    folderEntriesToNodes,
    formatFileSize,
    metadataRows,
    presenceChip,
    presenceLabel,
    showPresignedDownload,
    showPresignedUpload,
    treeNodeKey,
    visibleTreeNodes,
    type FileTreeNode,
} from "./fileStorageUi.js";

describe("selectedStoreRoute_replacesTheFileBase", () => {
    it("uses the only store, the selection, or nothing when the list failed", () => {
        const stores = [
            {name: "a", route: "FileStorage/a", fileMetadataRoute: null},
            {name: "b", route: "FileStorage/b", fileMetadataRoute: null},
        ];
        expect(selectedStoreRoute(stores, "b")).toBe("FileStorage/b");
        expect(selectedStoreRoute([{name: "only", route: "FileStorage/only", fileMetadataRoute: null}], null)).toBe("FileStorage/only");
        expect(selectedStoreRoute(null, null)).toBeUndefined();
    });
});

describe("folderEntriesToNodes_splitsDirectoriesAndFiles", () => {
    it("keeps the API path prefix and joins a name when the prefix is missing", () => {
        const nodes = folderEntriesToNodes([
            {name: "notes", isDirectory: true, pathPrefix: null, presence: "none"},
            {name: "a.bin", isDirectory: false, pathPrefix: "docs", fileId: "f1", presence: "both", originalFileName: "a.bin"},
        ], "docs");
        expect(nodes.map((node) => node.name)).toEqual(["notes", "a.bin"]);
        expect(nodes[0]).toMatchObject({isDirectory: true, pathPrefix: "docs/notes", presence: "None"});
        expect(nodes[1]).toMatchObject({isDirectory: false, fileId: "f1", pathPrefix: "docs", presence: "Both"});
    });
});

describe("presenceLabel_mapsStorePhysicalBoth", () => {
    it("reads numeric flags and camelCase names", () => {
        expect(presenceLabel(0)).toBe("None");
        expect(presenceLabel(1)).toBe("Store");
        expect(presenceLabel(2)).toBe("Physical");
        expect(presenceLabel(3)).toBe("Both");
        expect(presenceLabel("store")).toBe("Store");
        expect(presenceLabel("physical")).toBe("Physical");
        expect(presenceLabel("both")).toBe("Both");
    });
});

describe("activeFilesWhere_omitsSoftDeleted", () => {
    it("requires DeletedAt null and availability other than Deleted", () => {
        const where = activeFilesWhere();
        expect(where).toMatchObject({
            $type: "group",
            Operator: "And",
            Children: [
                {$type: "condition", Field: "DeletedAt", Comparison: "Equals", Value: null},
                {$type: "condition", Field: "Availability", Comparison: "NotEquals", Value: "Deleted"},
            ],
        });
    });
});

describe("presenceChip_matchesCatalogAndBackend", () => {
    it("labels Both, catalog-only, and backend-only files", () => {
        expect(backendDisplayName("AwsS3")).toBe("S3");
        expect(backendDisplayName(1)).toBe("Local");
        expect(presenceChip("Both", "AwsS3")).toMatchObject({
            label: "Both",
            color: "success",
            hint: "In the catalog and on S3",
            icon: "both",
        });
        expect(presenceChip("Store", 2)).toMatchObject({
            label: "Catalog only",
            color: "error",
            hint: "In the catalog; no object on S3",
            icon: "catalog",
        });
        expect(presenceChip("Physical", "Local")).toMatchObject({
            label: "Local only",
            color: "info",
            hint: "On Local; not in the catalog",
            icon: "physical",
        });
        expect(presenceChip("None", "AwsS3")).toBeNull();
    });
});

describe("metadataRows_matchesFileStoreTable", () => {
    it("formats an active catalog file the way the Blazor metadata table does", () => {
        const rows = Object.fromEntries(metadataRows(sampleMetadata()).map((row) => [row.label, row.value]));
        expect(rows["Id"]).toBe("01a0e84c-6886-7648-904e-85ee6d069b74");
        expect(rows["Status"]).toBe("Active");
        expect(rows["Original name"]).toBe("main.tscn");
        expect(rows["Original size"]).toBe("39.49 KB");
        expect(rows["Stored source"]).toBe("01a0e84c-6886-7648-904e-85ee6d069b74");
        expect(rows["Stored size"]).toBe("39.49 KB");
        expect(rows["Compressed"]).toBe("No");
        expect(rows["Encrypted"]).toBe("No");
        expect(rows["Path prefix"]).toBe("recipe/ingredients/01a0e619-e477-7d40-b8be-e463b913e8ce");
        expect(rows["Timestamp"]).toBe("2026-09-28 13:55:16Z");
        expect(rows["Hash algorithm"]).toBe("Sha256");
        expect(rows["Original hash"]).toBe("C563");
        expect(rows["Stored hash"]).toBe("C563");
        expect(rows["Compressed size"]).toBe("-");
        expect(rows["Metadata"]).toBe("-");
    });

    it("marks a deleted file and shows encryption and compression details", () => {
        const file = sampleMetadata();
        file.deletedAt = "2026-09-29T01:02:03Z";
        file.isCompressed = true;
        file.compressionAlgorithm = "Brotli";
        file.compressedFileSize = 100;
        file.isEncrypted = true;
        file.dataEncryptionKeyId = "kek";
        file.dataEncryptionKeyVersion = "3";
        const rows = Object.fromEntries(metadataRows(file).map((row) => [row.label, row.value]));
        expect(rows["Status"]).toBe("Deleted (2026-09-29 01:02:03Z)");
        expect(rows["Compressed"]).toBe("Brotli");
        expect(rows["Encrypted"]).toBe("kek / 3");
        expect(rows["Compressed size"]).toBe("100 B");
    });
});

describe("formatFileSize_usesBinaryUnits", () => {
    it("keeps whole bytes and trims fractional kilobytes", () => {
        expect(formatFileSize(0)).toBe("0 B");
        expect(formatFileSize(-4)).toBe("0 B");
        expect(formatFileSize(512)).toBe("512 B");
        expect(formatFileSize(1536)).toBe("1.5 KB");
        expect(formatFileSize(40437)).toBe("39.49 KB");
    });
});

describe("gridChips_matchFileStorageColorHelper", () => {
    it("colors known file types, encryption, and compression", () => {
        expect(fileTypeChip("application/pdf")).toMatchObject({label: "PDF", hue: 312});
        expect(fileTypeChip("text/csv; charset=utf-8")).toMatchObject({label: "CSV"});
        expect(fileTypeChip(null, "notes.txt")).toMatchObject({label: "TXT"});
        expect(fileTypeChip("application/x-custom")).toMatchObject({label: "application/x-custom", color: "default"});
        expect(fileTypeChip("application/x-custom").hue).toBeUndefined();
        expect(fileTypeChip(null, null)).toMatchObject({label: "File"});

        expect(encryptionChip(null)).toMatchObject({spec: {label: "None", color: "default"}, icon: "lockOpen"});
        expect(encryptionChip("AES-GCM")).toMatchObject({spec: {label: "AES-GCM", hue: 228}, icon: "lock"});
        expect(encryptionChip("ChaCha20-Poly1305").spec.hue).toBe(174);
        expect(encryptionChip("RSA").spec.hue).toBe(274);
        expect(encryptionChip("custom")).toMatchObject({spec: {label: "custom", color: "default"}, icon: "lock"});
        expect(encryptionChip("custom").spec.hue).toBeUndefined();

        expect(compressionChip(null)).toMatchObject({spec: {label: "None", color: "default"}});
        expect(compressionChip(null).icon).toBeUndefined();
        expect(compressionChip("Brotli")).toMatchObject({spec: {label: "Brotli", hue: 84}, icon: "compress"});
        expect(compressionChip("GZip").spec.hue).toBe(52);
        expect(compressionChip("Deflate").spec.hue).toBe(192);
        expect(compressionChip("LZ4").spec.hue).toBe(18);
        expect(compressionChip("Snappy")).toMatchObject({spec: {label: "Snappy"}, icon: "compress"});
        expect(compressionChip("Snappy").spec.hue).toBeUndefined();
    });
});

function sampleMetadata(): FileStoreResult {
    return {
        id: "01a0e84c-6886-7648-904e-85ee6d069b74",
        originalFileName: "main.tscn",
        originalFileSize: 40437,
        originalFileHash: "xWM=",
        sourceFileName: "01a0e84c-6886-7648-904e-85ee6d069b74",
        sourceFileSize: 40437,
        sourceFileHash: "xWM=",
        isCompressed: false,
        isEncrypted: false,
        pathPrefix: "recipe/ingredients/01a0e619-e477-7d40-b8be-e463b913e8ce",
        timestamp: "2026-09-28T13:55:16Z",
        hashAlgorithm: "Sha256",
    };
}

describe("treeNodeKey_usesFileId", () => {
    it("keys a file by id so the same filename in one folder stays a separate row", () => {
        const left = file("INV-2026-00001.pdf", "reports");
        left.fileId = "id-a";
        const right = file("INV-2026-00001.pdf", "reports");
        right.fileId = "id-b";
        expect(treeNodeKey(left)).toBe("f:id-a");
        expect(treeNodeKey(right)).toBe("f:id-b");
        expect(treeNodeKey(dir("reports", "reports"))).toBe("d:reports/reports");
        const physical = file("INV-2026-00001.pdf", "reports");
        physical.fileId = null;
        physical.physicalKey = "s3/obj-1";
        expect(treeNodeKey(physical)).toBe("p:s3/obj-1");
    });
});

describe("fileGlyph_matchesTreeIcons", () => {
    it("uses a folder glyph for directories and a type glyph from the file name", () => {
        expect(fileGlyph("recipe", true)).toBe("folder");
        expect(fileGlyph("INV-2026-00001.pdf", false)).toBe("pdf");
        expect(fileGlyph("sheet.xlsx", false)).toBe("table");
        expect(fileGlyph("photo.PNG", false)).toBe("image");
        expect(fileGlyph("notes.txt", false)).toBe("document");
        expect(fileGlyph("MAIN.CS", false)).toBe("code");
        expect(fileGlyph("clip.mp3", false)).toBe("audio");
        expect(fileGlyph("clip.mp4", false)).toBe("video");
        expect(fileGlyph("bundle.zip", false)).toBe("archive");
        expect(fileGlyph("blob.bin", false)).toBe("file");
    });
});

describe("visibleTreeNodes_keepsMatchesAndAncestorFolders", () => {
    it("hides files that miss the query and keeps a folder when a loaded child matches", () => {
        const recipe = dir("recipe", "recipe");
        const report = dir("report", "report");
        const notes = file("notes.pdf", "recipe");
        const other = file("other.bin", "report");
        const children = {
            [treeKey(recipe)]: [notes],
            [treeKey(report)]: [other],
        };
        expect(visibleTreeNodes([recipe, report], "notes", children).map((node) => node.name)).toEqual(["recipe"]);
        expect(visibleTreeNodes([notes, other], "pdf", children).map((node) => node.name)).toEqual(["notes.pdf"]);
        expect(visibleTreeNodes([recipe, report], "   ", children).map((node) => node.name)).toEqual(["recipe", "report"]);
    });
});

function dir(name: string, pathPrefix: string): FileTreeNode {
    return {name, isDirectory: true, pathPrefix, fileId: null, presence: "None", originalFileName: null, originalFileSize: 0, physicalKey: null};
}

function file(name: string, pathPrefix: string): FileTreeNode {
    return {name, isDirectory: false, pathPrefix, fileId: name, presence: "Both", originalFileName: name, originalFileSize: 1, physicalKey: null};
}

function treeKey(node: FileTreeNode): string {
    return `${node.isDirectory ? "d" : "f"}:${node.pathPrefix ?? ""}/${node.name}`;
}

describe("presignedActions_plaintextOnly", () => {
    it("hides provider download and upload when the file is encrypted or compressed", () => {
        expect(showPresignedDownload({isEncrypted: false, isCompressed: false})).toBe(true);
        expect(showPresignedDownload({isEncrypted: true, isCompressed: false})).toBe(false);
        expect(showPresignedDownload({isEncrypted: false, isCompressed: true})).toBe(false);
        expect(showPresignedUpload({compress: false, encrypt: false})).toBe(true);
        expect(showPresignedUpload({compress: true, encrypt: false})).toBe(false);
        expect(showPresignedUpload({compress: false, encrypt: true})).toBe(false);
    });
});
