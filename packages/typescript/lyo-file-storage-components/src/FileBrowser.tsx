"use client";

import {useEffect, useState, type ReactNode} from "react";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DriveFileMoveIcon from "@mui/icons-material/DriveFileMove";
import DriveFileRenameOutlineIcon from "@mui/icons-material/DriveFileRenameOutline";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import LinkIcon from "@mui/icons-material/Link";
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import IconButton from "@mui/material/IconButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import type {AsyncApiClient} from "lyo-api-client";
import type {AsyncFileStorageApiClient, FileStoreResult} from "lyo-file-storage-api-client";
import {FileStorageRoutes, joinRoutePrefix} from "lyo-file-storage-api-client";
import {asLyoQueryClient, LyoDataGridProjected, LyoMenuItem, LyoTooltip, projectedId, useLyoSnackbar} from "lyo-web-components";
import {fileMetadataColumns} from "./columns.js";
import type {FileDialogKind} from "./dialogs.js";
import {triggerBlobDownload} from "./downloadBlob.js";
import {FileMetadataTable} from "./FileMetadataTable.js";
import {activeFilesWhere, projectedBool, showPresignedDownload} from "./fileStorageUi.js";

type Row = Record<string, unknown>;

function text(row: Row, field: string): string {
    const value = row[field] ?? row[field.charAt(0)!.toLowerCase() + field.slice(1)];
    return value == null ? "" : String(value);
}

function MenuIcon({children}: {children: ReactNode}) {
    return <ListItemIcon sx={{minWidth: 32}}>{children}</ListItemIcon>;
}

function ActionIcon({label, onClick, children}: {label: string; onClick: () => void; children: ReactNode}) {
    return (
        <LyoTooltip title={label}>
            <IconButton size="small" aria-label={label} onClick={onClick}>
                {children}
            </IconButton>
        </LyoTooltip>
    );
}

export function FileBrowser({
    apiClient,
    files,
    routePrefix,
    onOpen,
}: {
    apiClient: AsyncApiClient;
    files: AsyncFileStorageApiClient;
    routePrefix?: string;
    onOpen: (kind: FileDialogKind, fileId: string) => void;
}) {
    const snackbar = useLyoSnackbar();
    const query = asLyoQueryClient(apiClient);
    const route = joinRoutePrefix(routePrefix, FileStorageRoutes.fileMetadata);
    const [selected, setSelected] = useState<Row | null>(null);
    const [metadata, setMetadata] = useState<FileStoreResult | null>(null);
    const [metadataBusy, setMetadataBusy] = useState(false);
    const [metadataStatus, setMetadataStatus] = useState("");
    const selectedId = selected ? projectedId(selected) : null;

    useEffect(() => {
        if (!selectedId) {
            setMetadata(null);
            setMetadataBusy(false);
            setMetadataStatus("");
            return;
        }
        let cancelled = false;
        setMetadata(null);
        setMetadataBusy(true);
        setMetadataStatus("");
        void files.getMetadata(selectedId).then(
            (response) => {
                if (cancelled) return;
                setMetadata(response.data ?? null);
                setMetadataStatus(response.data ? "" : `Metadata for ${selectedId} was not returned.`);
                setMetadataBusy(false);
            },
            (err: unknown) => {
                if (cancelled) return;
                setMetadata(null);
                setMetadataStatus(err instanceof Error ? err.message : "Metadata request failed");
                setMetadataBusy(false);
            }
        );
        return () => {
            cancelled = true;
        };
    }, [files, selectedId]);

    const download = async (row: Row) => {
        const saved = await files.download(projectedId(row));
        triggerBlobDownload(saved.blob, saved.fileName);
    };

    const downloadSelected = () => {
        if (!selected) return;
        void download(selected).catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Download failed", "error"));
    };

    return (
        <div className="lyo-fs-browser">
            <div className="lyo-fs-panel lyo-fs-scroll">
                <LyoDataGridProjected<Row>
                    apiClient={query}
                    gridKey="FileStorageBrowser"
                    route={route}
                    columns={fileMetadataColumns()}
                    keySelector={(row) => [projectedId(row)]}
                    onRowClick={setSelected}
                    isRowSelected={(row) => projectedId(row) === selectedId}
                    beforeQuery={(req) => {
                        const active = activeFilesWhere();
                        const current = req.whereClause;
                        return {
                            ...req,
                            whereClause: current ? {$type: "group", Operator: "And", Children: [active, current]} : active,
                        };
                    }}
                    rowMenu={(row) => {
                        const id = projectedId(row);
                        const plaintext = showPresignedDownload({
                            isEncrypted: projectedBool(row, "IsEncrypted"),
                            isCompressed: projectedBool(row, "IsCompressed"),
                        });
                        return (
                            <>
                                <LyoMenuItem onClick={() => void download(row)}>
                                    <MenuIcon>
                                        <FileDownloadIcon fontSize="small" />
                                    </MenuIcon>
                                    Download
                                </LyoMenuItem>
                                {plaintext ? (
                                    <LyoMenuItem
                                        onClick={() => {
                                            void files.getMetadata(id).then((response) => {
                                                const meta = response.data;
                                                if (!meta || !showPresignedDownload(meta)) {
                                                    snackbar.show("Use download. This file is encrypted or compressed.", "warning");
                                                    return;
                                                }
                                                void files.presignedDownloadUrl(meta).then((url) => snackbar.show(url, "success"));
                                            });
                                        }}
                                    >
                                        <MenuIcon>
                                            <LinkIcon fontSize="small" />
                                        </MenuIcon>
                                        Presigned download URL
                                    </LyoMenuItem>
                                ) : null}
                                <LyoMenuItem onClick={() => onOpen("access", id)}>
                                    <MenuIcon>
                                        <LinkIcon fontSize="small" />
                                    </MenuIcon>
                                    Access link
                                </LyoMenuItem>
                                <LyoMenuItem onClick={() => setSelected(row)}>
                                    <MenuIcon>
                                        <InfoOutlinedIcon fontSize="small" />
                                    </MenuIcon>
                                    Show metadata
                                </LyoMenuItem>
                                <LyoMenuItem onClick={() => onOpen("move", id)}>
                                    <MenuIcon>
                                        <DriveFileMoveIcon fontSize="small" />
                                    </MenuIcon>
                                    Move
                                </LyoMenuItem>
                                <LyoMenuItem onClick={() => onOpen("copy", id)}>
                                    <MenuIcon>
                                        <ContentCopyIcon fontSize="small" />
                                    </MenuIcon>
                                    Copy
                                </LyoMenuItem>
                                <LyoMenuItem onClick={() => onOpen("rename", id)}>
                                    <MenuIcon>
                                        <DriveFileRenameOutlineIcon fontSize="small" />
                                    </MenuIcon>
                                    Rename
                                </LyoMenuItem>
                                <LyoMenuItem onClick={() => onOpen("rotate", id)}>
                                    <MenuIcon>
                                        <VpnKeyIcon fontSize="small" />
                                    </MenuIcon>
                                    Rotate DEK
                                </LyoMenuItem>
                                <LyoMenuItem
                                    onClick={() => {
                                        void files.downloadArchive([id]).then(
                                            (saved) => triggerBlobDownload(saved.blob, saved.fileName),
                                            (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Zip failed", "error")
                                        );
                                    }}
                                >
                                    <MenuIcon>
                                        <FileDownloadIcon fontSize="small" />
                                    </MenuIcon>
                                    Download zip
                                </LyoMenuItem>
                                <LyoMenuItem
                                    onClick={() => {
                                        void files.delete(id).then(
                                            () => snackbar.show("Deleted.", "success"),
                                            (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Delete failed", "error")
                                        );
                                    }}
                                >
                                    <MenuIcon>
                                        <DeleteOutlineIcon fontSize="small" />
                                    </MenuIcon>
                                    Delete
                                </LyoMenuItem>
                            </>
                        );
                    }}
                />
            </div>
            <div className="lyo-fs-panel">
                {selectedId ? (
                    <div className="lyo-fs-inspector">
                        <div className="lyo-fs-inspector-head">
                            <div className="lyo-fs-inspector-titles">
                                <strong className="lyo-fs-file-title">{text(selected!, "OriginalFileName") || selectedId}</strong>
                                <div className="lyo-fs-path">{text(selected!, "PathPrefix") || "Root"}</div>
                            </div>
                        </div>
                        <div className="lyo-fs-actions" role="toolbar" aria-label="File actions">
                            <ActionIcon label="Download" onClick={downloadSelected}>
                                <FileDownloadIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon label="Access link" onClick={() => onOpen("access", selectedId)}>
                                <LinkIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon label="Move" onClick={() => onOpen("move", selectedId)}>
                                <DriveFileMoveIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon label="Copy" onClick={() => onOpen("copy", selectedId)}>
                                <ContentCopyIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon label="Rename" onClick={() => onOpen("rename", selectedId)}>
                                <DriveFileRenameOutlineIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon label="Rotate DEK" onClick={() => onOpen("rotate", selectedId)}>
                                <VpnKeyIcon fontSize="small" />
                            </ActionIcon>
                            <ActionIcon
                                label="Delete"
                                onClick={() => {
                                    void files.delete(selectedId).then(
                                        () => snackbar.show("Deleted.", "success"),
                                        (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Delete failed", "error")
                                    );
                                }}
                            >
                                <DeleteOutlineIcon fontSize="small" />
                            </ActionIcon>
                        </div>
                        <div className="lyo-fs-meta">
                            {metadataBusy ? <p className="lyo-fs-empty">Loading metadata…</p> : null}
                            {metadata ? <FileMetadataTable metadata={metadata} /> : null}
                            {!metadataBusy && !metadata && metadataStatus ? <p className="lyo-fs-empty">{metadataStatus}</p> : null}
                        </div>
                    </div>
                ) : (
                    <p className="lyo-fs-empty">Select a file to see its metadata.</p>
                )}
            </div>
        </div>
    );
}
