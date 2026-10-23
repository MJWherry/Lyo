"use client";

import {useEffect, useState} from "react";
import type {AsyncFileStorageApiClient, DownloadAccessLinkResponse, FileStoreResult} from "lyo-file-storage-api-client";
import {LyoDialog, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";
import {FileMetadataTable} from "./FileMetadataTable.js";

export type FileDialogKind = "metadata" | "move" | "copy" | "rename" | "rotate" | "access" | null;

export function FileActionDialogs({
    kind,
    fileId,
    files,
    onClose,
    onChanged,
}: {
    kind: FileDialogKind;
    fileId: string | null;
    files: AsyncFileStorageApiClient;
    onClose: () => void;
    onChanged: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [pathPrefix, setPathPrefix] = useState("");
    const [name, setName] = useState("");
    const [keyId, setKeyId] = useState("");
    const [metadata, setMetadata] = useState<FileStoreResult | null>(null);
    const [metadataStatus, setMetadataStatus] = useState("");
    const [link, setLink] = useState<DownloadAccessLinkResponse | null>(null);
    const open = kind != null && Boolean(fileId);

    useEffect(() => {
        if (!(open && kind === "metadata" && fileId)) {
            setMetadata(null);
            setMetadataStatus("");
            return;
        }
        let cancelled = false;
        setMetadataStatus("");
        void files.getMetadata(fileId).then(
            (response) => {
                if (cancelled) return;
                setMetadata(response.data ?? null);
                setMetadataStatus(response.data ? "" : `Metadata for ${fileId} was not returned.`);
            },
            (err: unknown) => {
                if (cancelled) return;
                setMetadata(null);
                setMetadataStatus(err instanceof Error ? err.message : "Metadata request failed");
            }
        );
        return () => {
            cancelled = true;
        };
    }, [files, fileId, kind, open]);

    const run = async (work: () => Promise<void>, message: string) => {
        try {
            await work();
            snackbar.show(message, "success");
            onChanged();
            onClose();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Request failed", "error");
        }
    };

    return (
        <>
            <LyoDialog open={open && kind === "metadata"} title="File metadata" onClose={onClose}>
                {metadata ? <FileMetadataTable metadata={metadata} /> : <span>{metadataStatus || "Loading metadata…"}</span>}
            </LyoDialog>
            <LyoDialog
                open={open && (kind === "move" || kind === "copy")}
                title={kind === "copy" ? "Copy" : "Move"}
                onClose={onClose}
                saveText={kind === "copy" ? "Copy" : "Move"}
                onSave={() =>
                    run(async () => {
                        if (!fileId) return;
                        if (kind === "copy") await files.copy({sourceFileId: fileId, pathPrefix});
                        else await files.move({fileId, pathPrefix});
                    }, kind === "copy" ? "Copied." : "Moved.")
                }
            >
                <LyoTextField label="Path prefix" value={pathPrefix} onChange={(event) => setPathPrefix(event.target.value)} fullWidth />
            </LyoDialog>
            <LyoDialog
                open={open && kind === "rename"}
                title="Rename"
                onClose={onClose}
                onSave={() =>
                    run(async () => {
                        if (!fileId) return;
                        await files.rename({fileId, originalFileName: name});
                    }, "Renamed.")
                }
            >
                <LyoTextField label="File name" value={name} onChange={(event) => setName(event.target.value)} fullWidth />
            </LyoDialog>
            <LyoDialog
                open={open && kind === "rotate"}
                title="Rotate DEK"
                onClose={onClose}
                onSave={() =>
                    run(async () => {
                        if (!fileId) return;
                        await files.rotateDeks({fileIds: [fileId], targetKeyId: keyId || null});
                    }, "DEK rotation requested.")
                }
            >
                <LyoTextField label="Target key id" value={keyId} onChange={(event) => setKeyId(event.target.value)} fullWidth />
            </LyoDialog>
            <LyoDialog
                open={open && kind === "access"}
                title="Access link"
                onClose={onClose}
                saveText="Create"
                onSave={async () => {
                    if (!fileId) return;
                    try {
                        const response = await files.createAccessLink(fileId, {});
                        setLink(response.data ?? null);
                        snackbar.show("Access link created.", "success");
                    } catch (err) {
                        snackbar.show(err instanceof Error ? err.message : "Request failed", "error");
                    }
                }}
            >
                <LyoStack spacing={1}>
                    <span>The API decrypts and decompresses when this link is downloaded.</span>
                    {link ? (
                        <>
                            <span>{link.downloadUrl}</span>
                            <span>{link.presignedReadUrl}</span>
                        </>
                    ) : null}
                </LyoStack>
            </LyoDialog>
        </>
    );
}
