"use client";

import {useCallback, useEffect, useState, type ReactNode} from "react";
import AudioFileIcon from "@mui/icons-material/AudioFile";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import CloudDoneIcon from "@mui/icons-material/CloudDone";
import CloudOffIcon from "@mui/icons-material/CloudOff";
import CodeIcon from "@mui/icons-material/Code";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DescriptionIcon from "@mui/icons-material/Description";
import DriveFileMoveIcon from "@mui/icons-material/DriveFileMove";
import DriveFileRenameOutlineIcon from "@mui/icons-material/DriveFileRenameOutline";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import FileDownloadIcon from "@mui/icons-material/FileDownload";
import FolderIcon from "@mui/icons-material/Folder";
import FolderZipIcon from "@mui/icons-material/FolderZip";
import ImageIcon from "@mui/icons-material/Image";
import InsertDriveFileIcon from "@mui/icons-material/InsertDriveFile";
import LinkIcon from "@mui/icons-material/Link";
import PictureAsPdfIcon from "@mui/icons-material/PictureAsPdf";
import RefreshIcon from "@mui/icons-material/Refresh";
import SdStorageIcon from "@mui/icons-material/SdStorage";
import TableChartIcon from "@mui/icons-material/TableChart";
import VideoFileIcon from "@mui/icons-material/VideoFile";
import VpnKeyIcon from "@mui/icons-material/VpnKey";
import IconButton from "@mui/material/IconButton";
import type {AsyncFileStorageApiClient, FileStorageTagTypeResponse, FileStorageTagValueKindResponse, FileStoreResult} from "lyo-file-storage-api-client";
import {LyoAlert, LyoButton, LyoChip, LyoTextField, LyoTooltip, useLyoSnackbar} from "lyo-web-components";
import type {FileDialogKind} from "./dialogs.js";
import {triggerBlobDownload} from "./downloadBlob.js";
import {FileMetadataTable} from "./FileMetadataTable.js";
import {
    backendDisplayName,
    fileGlyph,
    folderEntriesToNodes,
    presenceChip,
    treeNodeKey,
    visibleTreeNodes,
    type FileGlyph,
    type FileTreeNode,
    type PresenceChipIcon,
} from "./fileStorageUi.js";

const ROOT: FileTreeNode = {
    name: "Root",
    isDirectory: true,
    pathPrefix: null,
    fileId: null,
    presence: "None",
    originalFileName: null,
    originalFileSize: 0,
    physicalKey: null,
};

const GLYPH_ICONS: Record<FileGlyph, typeof FolderIcon> = {
    folder: FolderIcon,
    pdf: PictureAsPdfIcon,
    table: TableChartIcon,
    image: ImageIcon,
    document: DescriptionIcon,
    code: CodeIcon,
    audio: AudioFileIcon,
    video: VideoFileIcon,
    archive: FolderZipIcon,
    file: InsertDriveFileIcon,
};

const PRESENCE_ICONS: Record<PresenceChipIcon, typeof CloudDoneIcon> = {
    both: CloudDoneIcon,
    catalog: CloudOffIcon,
    physical: SdStorageIcon,
};

function catalogFile(node: FileTreeNode): boolean {
    return Boolean(node.fileId) && node.presence !== "Physical";
}

function NodeGlyph({node}: {node: FileTreeNode}) {
    const Icon = GLYPH_ICONS[fileGlyph(node.originalFileName ?? node.name, node.isDirectory)];
    return <Icon className="lyo-fs-glyph" fontSize="small" />;
}

function PresenceChip({node, backendKind}: {node: FileTreeNode; backendKind: string | number | null}) {
    if (node.isDirectory) return null;
    const chip = presenceChip(node.presence, backendKind);
    if (!chip) return null;
    const Icon = PRESENCE_ICONS[chip.icon];
    return (
        <LyoTooltip title={chip.hint}>
            <span className="lyo-fs-chip">
                <LyoChip size="small" variant="filled" color={chip.color} label={chip.label} icon={<Icon fontSize="small" />} />
            </span>
        </LyoTooltip>
    );
}

function ActionIcon({
    label,
    disabled,
    color,
    onClick,
    children,
}: {
    label: string;
    disabled?: boolean;
    color?: "error" | "default";
    onClick: () => void;
    children: ReactNode;
}) {
    return (
        <LyoTooltip title={label}>
            <span>
                <IconButton
                    size="small"
                    aria-label={label}
                    disabled={disabled}
                    color={color ?? "default"}
                    onClick={onClick}
                    sx={{
                        color: color === "error" ? undefined : "var(--ink, inherit)",
                        "&.Mui-disabled": {
                            opacity: 1,
                            color: "color-mix(in srgb, var(--ink, #e8eef6) 80%, transparent)",
                        },
                    }}
                >
                    {children}
                </IconButton>
            </span>
        </LyoTooltip>
    );
}

export function FileTree({
    files,
    reloadToken,
    onOpen,
}: {
    files: AsyncFileStorageApiClient;
    reloadToken: number;
    onOpen: (kind: FileDialogKind, fileId: string) => void;
}) {
    const snackbar = useLyoSnackbar();
    const [roots, setRoots] = useState<FileTreeNode[]>([]);
    const [children, setChildren] = useState<Record<string, FileTreeNode[]>>({});
    const [expanded, setExpanded] = useState<Set<string>>(() => new Set([treeNodeKey(ROOT)]));
    const [loadingKey, setLoadingKey] = useState<string | null>(null);
    const [selectedKey, setSelectedKey] = useState<string | null>(null);
    const [selected, setSelected] = useState<FileTreeNode | null>(null);
    const [checked, setChecked] = useState<string[]>([]);
    const [selecting, setSelecting] = useState(false);
    const [query, setQuery] = useState("");
    const [backendKind, setBackendKind] = useState<string | number | null>(null);
    const [metadata, setMetadata] = useState<FileStoreResult | null>(null);
    const [metadataBusy, setMetadataBusy] = useState(false);
    const [metadataStatus, setMetadataStatus] = useState("");
    const [groupBy, setGroupBy] = useState<string[]>([]);
    const [tagTypes, setTagTypes] = useState<FileStorageTagTypeResponse[]>([]);
    const [tagValueKinds, setTagValueKinds] = useState<FileStorageTagValueKindResponse[]>([]);
    const [addType, setAddType] = useState("");
    const [addKind, setAddKind] = useState("string");
    const [addGrain, setAddGrain] = useState("Value");

    useEffect(() => {
        let cancelled = false;
        void files.capabilities().then(
            (response) => {
                if (!cancelled) {
                    setTagTypes(response.data?.tagTypes ?? []);
                    setTagValueKinds(response.data?.tagValueKinds ?? []);
                }
            },
            () => {
                if (!cancelled) {
                    setTagTypes([]);
                    setTagValueKinds([]);
                }
            }
        );
        return () => {
            cancelled = true;
        };
    }, [files]);

    const selectedType = tagTypes.find((type) => type.tagType === addType);
    const effectiveKind = selectedType?.automatic ? selectedType.valueKind || "string" : addKind || selectedType?.valueKind || "string";
    const grains = grainsForKind(effectiveKind, tagValueKinds, selectedType);
    const needsGrain = grains.some((grain) => grain.toLowerCase() !== "value");
    const grainValue = grains.includes(addGrain) ? addGrain : grains.find((grain) => grain.toLowerCase() !== "value") ?? "Value";
    const kindChoices = tagValueKinds.length > 0 ? tagValueKinds : [{kind: effectiveKind, sort: selectedType?.sort ?? "Ordinal", grains}];
    const blobStructure = groupBy.length === 0;

    const loadFolder = useCallback(
        async (pathPrefix: string | null) => {
            const response = blobStructure
                ? await files.listFolder({pathPrefix: pathPrefix ?? undefined})
                : await files.listTagGroup(groupBy, pathPrefix ?? undefined);
            if (response.data?.backendKind != null) setBackendKind(response.data.backendKind);
            return folderEntriesToNodes(response.data?.entries ?? [], pathPrefix);
        },
        [blobStructure, files, groupBy]
    );

    useEffect(() => {
        let cancelled = false;
        setChildren({});
        setExpanded(new Set([treeNodeKey(ROOT)]));
        setSelected(null);
        setSelectedKey(null);
        void loadFolder(null).then(
            (nodes) => {
                if (!cancelled) setRoots(nodes);
            },
            (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Failed to list folder", "error")
        );
        return () => {
            cancelled = true;
        };
    }, [loadFolder, reloadToken, snackbar]);

    const selectedId = selected && !selected.isDirectory ? selected.fileId : null;
    const selectedPresence = selected?.presence;

    useEffect(() => {
        if (!selectedId || selectedPresence === "Physical") {
            setMetadata(null);
            setMetadataBusy(false);
            setMetadataStatus(
                selectedPresence === "Physical"
                    ? "This object is on disk only (not in the catalog). Download is the raw physical file — it is not decrypted or decompressed."
                    : ""
            );
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
    }, [files, selectedId, selectedPresence]);

    const toggleDirectory = async (node: FileTreeNode) => {
        const key = treeNodeKey(node);
        if (expanded.has(key)) {
            setExpanded((cur) => {
                const next = new Set(cur);
                next.delete(key);
                return next;
            });
            return;
        }
        if (key !== treeNodeKey(ROOT) && !children[key]) {
            setLoadingKey(key);
            try {
                const nodes = await loadFolder(node.pathPrefix);
                setChildren((cur) => ({...cur, [key]: nodes}));
            } catch (err) {
                snackbar.show(err instanceof Error ? err.message : "Failed to list folder", "error");
                return;
            } finally {
                setLoadingKey(null);
            }
        }
        setExpanded((cur) => new Set(cur).add(key));
    };

    const selectNode = (node: FileTreeNode) => {
        setSelected(node);
        setSelectedKey(treeNodeKey(node));
        if (node.isDirectory) void toggleOpen(node);
    };

    const toggleOpen = async (node: FileTreeNode) => {
        const key = treeNodeKey(node);
        if (expanded.has(key)) return;
        if (key === treeNodeKey(ROOT)) {
            setExpanded((cur) => new Set(cur).add(key));
            return;
        }
        if (!children[key]) {
            setLoadingKey(key);
            try {
                const nodes = await loadFolder(node.pathPrefix);
                setChildren((cur) => ({...cur, [key]: nodes}));
            } catch (err) {
                snackbar.show(err instanceof Error ? err.message : "Failed to list folder", "error");
                return;
            } finally {
                setLoadingKey(null);
            }
        }
        setExpanded((cur) => new Set(cur).add(key));
    };

    const download = async (node: FileTreeNode) => {
        const saved =
            node.fileId && node.presence !== "Physical"
                ? await files.download(node.fileId)
                : node.physicalKey
                  ? await files.downloadPhysical(node.physicalKey)
                  : null;
        if (saved) triggerBlobDownload(saved.blob, saved.fileName);
    };

    const remove = async (node: FileTreeNode) => {
        if (node.fileId && node.presence !== "Physical") {
            await files.delete(node.fileId);
            return;
        }
        if (node.physicalKey) await files.deletePhysical(node.physicalKey);
    };

    const childList = (node: FileTreeNode): FileTreeNode[] | undefined => {
        if (treeNodeKey(node) === treeNodeKey(ROOT)) return roots;
        return children[treeNodeKey(node)];
    };

    const loaded = {...children, [treeNodeKey(ROOT)]: roots};

    const renderNodes = (nodes: FileTreeNode[], guides: boolean[], elbow: boolean) => {
        const shown = visibleTreeNodes(nodes, query, loaded);
        return shown.map((node, index) => {
            const key = treeNodeKey(node);
            const last = index === shown.length - 1;
            const open = expanded.has(key);
            const kids = node.isDirectory ? childList(node) : undefined;
            return (
                <li key={key} className="lyo-fs-item" role="treeitem" aria-expanded={node.isDirectory ? open : undefined} aria-selected={selectedKey === key}>
                    <div className={selectedKey === key ? "lyo-fs-row is-selected" : "lyo-fs-row"}>
                        {guides.map((line, guideIndex) => (
                            <span key={guideIndex} className={line ? "lyo-fs-guide is-line" : "lyo-fs-guide"} />
                        ))}
                        {elbow ? <span className={last ? "lyo-fs-elbow" : "lyo-fs-elbow is-through"} /> : null}
                        {node.isDirectory ? (
                            <IconButton
                                size="small"
                                className="lyo-fs-toggle"
                                sx={{width: 28, height: 28, p: 0}}
                                aria-label={open ? "Collapse" : "Expand"}
                                onClick={(event) => {
                                    event.stopPropagation();
                                    void toggleDirectory(node);
                                }}
                            >
                                {loadingKey === key ? <span className="lyo-fs-wait">…</span> : open ? <ExpandMoreIcon fontSize="small" /> : <ChevronRightIcon fontSize="small" />}
                            </IconButton>
                        ) : (
                            <span className="lyo-fs-toggle" />
                        )}
                        {selecting && !node.isDirectory ? (
                            <input
                                type="checkbox"
                                className="lyo-fs-check"
                                aria-label={`Select ${node.name}`}
                                checked={node.fileId != null && checked.includes(node.fileId)}
                                disabled={!node.fileId}
                                onChange={() => {
                                    if (!node.fileId) return;
                                    const id = node.fileId;
                                    setChecked((cur) => (cur.includes(id) ? cur.filter((item) => item !== id) : [...cur, id]));
                                }}
                            />
                        ) : null}
                        <button type="button" className="lyo-fs-label" onClick={() => selectNode(node)}>
                            <NodeGlyph node={node} />
                            <span className="lyo-fs-name">{node.isDirectory ? node.name : node.originalFileName || node.name}</span>
                        </button>
                        <PresenceChip node={node} backendKind={backendKind} />
                    </div>
                    {node.isDirectory && open && kids ? (
                        <ul className="lyo-fs-branch" role="group">
                            {renderNodes(kids, elbow ? [...guides, !last] : guides, true)}
                        </ul>
                    ) : null}
                </li>
            );
        });
    };

    const catalog = selected ? catalogFile(selected) : false;
    const canDownload = Boolean(selected && !selected.isDirectory && (catalog || selected.physicalKey));
    const fileId = selected?.fileId;
    const folderKids = selected?.isDirectory ? childList(selected) : undefined;

    return (
        <div className="lyo-fs-tree">
            <div className="lyo-fs-panel">
                <div className="lyo-fs-toolbar">
                    <div className="lyo-fs-toolbar-row">
                        <LyoButton size="small" variant={blobStructure ? "contained" : "outlined"} onClick={() => setGroupBy([])}>
                            Blob
                        </LyoButton>
                    </div>
                    <p className="lyo-fs-muted">
                        {blobStructure
                            ? "Folders come from PathPrefix."
                            : "Folders come from the tag grouping. They are not blob prefixes."}
                    </p>
                    <div className="lyo-fs-toolbar-row">
                        <span className="lyo-fs-title">Paths</span>
                        <LyoTooltip title="Refresh tree">
                            <IconButton
                                size="small"
                                aria-label="Refresh tree"
                                onClick={() => {
                                    setChildren({});
                                    setExpanded(new Set([treeNodeKey(ROOT)]));
                                    void loadFolder(null).then(
                                        (nodes) => setRoots(nodes),
                                        (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Failed to list folder", "error")
                                    );
                                }}
                            >
                                <RefreshIcon fontSize="small" />
                            </IconButton>
                        </LyoTooltip>
                    </div>
                    <div className="lyo-fs-toolbar-row lyo-fs-structure">
                        <select
                            className="lyo-fs-tag-type"
                            aria-label="Tag type"
                            value={addType}
                            onChange={(event) => {
                                const nextType = event.target.value;
                                const next = tagTypes.find((type) => type.tagType === nextType);
                                const kind = next?.valueKind || "string";
                                setAddType(nextType);
                                setAddKind(kind);
                                setAddGrain(firstGrain(kind, tagValueKinds, next));
                            }}
                        >
                            <option value="">Tag type</option>
                            {tagTypes.map((type) => (
                                <option key={type.tagType} value={type.tagType}>
                                    {type.tagType}
                                </option>
                            ))}
                        </select>
                        {addType ? (
                            <select
                                aria-label="Kind"
                                value={effectiveKind}
                                disabled={selectedType?.automatic === true}
                                onChange={(event) => {
                                    const kind = event.target.value;
                                    setAddKind(kind);
                                    setAddGrain(firstGrain(kind, tagValueKinds, selectedType));
                                }}
                            >
                                {kindChoices.map((kind) => (
                                    <option key={kind.kind} value={kind.kind}>
                                        {kind.kind}
                                    </option>
                                ))}
                            </select>
                        ) : null}
                        {needsGrain ? (
                            <select aria-label="Grain" value={grainValue} onChange={(event) => setAddGrain(event.target.value)}>
                                {grains
                                    .filter((grain) => grain.toLowerCase() !== "value")
                                    .map((grain) => (
                                        <option key={grain} value={grain}>
                                            {grain}
                                        </option>
                                    ))}
                            </select>
                        ) : null}
                        <LyoButton
                            size="small"
                            variant="outlined"
                            disabled={!addType}
                            onClick={() => {
                                const grain = needsGrain ? grainValue : "Value";
                                setGroupBy((current) => [...current, formatGroupKey(addType, effectiveKind, grain)]);
                            }}
                        >
                            Add
                        </LyoButton>
                    </div>
                    {groupBy.length > 0 ? (
                        <div className="lyo-fs-toolbar-row lyo-fs-tag-list">
                            {groupBy.map((key, index) => (
                                <LyoChip
                                    key={`${key}-${index}`}
                                    size="small"
                                    label={key.split(":").join(" ")}
                                    onDelete={() => setGroupBy((current) => current.filter((_, item) => item !== index))}
                                />
                            ))}
                        </div>
                    ) : null}
                    <div className="lyo-fs-toolbar-row">
                        <LyoTextField
                            size="small"
                            placeholder="Search"
                            value={query}
                            onChange={(event) => setQuery(event.target.value)}
                            aria-label="Search paths"
                            fullWidth
                        />
                        <LyoButton size="small" variant={selecting ? "contained" : "outlined"} onClick={() => setSelecting((on) => !on)}>
                            {selecting ? "Done" : "Select"}
                        </LyoButton>
                    </div>
                    {selecting ? (
                        <div className="lyo-fs-toolbar-row">
                            <span className="lyo-fs-muted">{checked.length} file(s)</span>
                            <LyoButton
                                size="small"
                                variant="outlined"
                                disabled={checked.length === 0}
                                onClick={() => {
                                    void files.downloadArchive(checked, "files.zip").then(
                                        (saved) => triggerBlobDownload(saved.blob, saved.fileName),
                                        (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Zip failed", "error")
                                    );
                                }}
                            >
                                Download zip
                            </LyoButton>
                        </div>
                    ) : null}
                </div>
                <div className="lyo-fs-scroll" role="tree" aria-label="File storage paths">
                    <ul className="lyo-fs-branch">{renderNodes([ROOT], [], false)}</ul>
                </div>
            </div>
            <div className="lyo-fs-panel">
                <FileInspector
                    selected={selected}
                    backendKind={backendKind}
                    metadata={metadata}
                    metadataBusy={metadataBusy}
                    metadataStatus={metadataStatus}
                    folderKids={folderKids}
                    allowPrefixChanges={blobStructure}
                    catalog={catalog}
                    canDownload={canDownload}
                    onSelect={selectNode}
                    onOpen={onOpen}
                    onDownload={() => {
                        if (!selected) return;
                        void download(selected).catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Download failed", "error"));
                    }}
                    onDelete={() => {
                        if (!selected) return;
                        void remove(selected).then(
                            () => snackbar.show("Deleted.", "success"),
                            (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Delete failed", "error")
                        );
                    }}
                    fileId={fileId}
                />
            </div>
        </div>
    );
}

function FileInspector({
    selected,
    backendKind,
    metadata,
    metadataBusy,
    metadataStatus,
    folderKids,
    allowPrefixChanges,
    catalog,
    canDownload,
    fileId,
    onSelect,
    onOpen,
    onDownload,
    onDelete,
}: {
    selected: FileTreeNode | null;
    backendKind: string | number | null;
    metadata: FileStoreResult | null;
    metadataBusy: boolean;
    metadataStatus: string;
    folderKids: FileTreeNode[] | undefined;
    allowPrefixChanges: boolean;
    catalog: boolean;
    canDownload: boolean;
    fileId: string | null | undefined;
    onSelect: (node: FileTreeNode) => void;
    onOpen: (kind: FileDialogKind, fileId: string) => void;
    onDownload: () => void;
    onDelete: () => void;
}) {
    if (!selected) return <p className="lyo-fs-empty">Select a folder or file in the tree.</p>;
    if (selected.isDirectory) {
        const crumbs = selected.pathPrefix ? selected.pathPrefix.split("/").filter(Boolean) : [];
        return (
            <div className="lyo-fs-inspector">
                <div className="lyo-fs-inspector-head">
                    <NodeGlyph node={selected} />
                    <div>
                        <strong className="lyo-fs-file-title">{selected.name}</strong>
                        <div className="lyo-fs-path">{crumbs.length === 0 ? "Root" : crumbs.join(" / ")}</div>
                    </div>
                </div>
                <div className="lyo-fs-section-label">Contents</div>
                {folderKids == null ? (
                    <p className="lyo-fs-empty">Loading…</p>
                ) : folderKids.length === 0 ? (
                    <p className="lyo-fs-empty">Empty.</p>
                ) : (
                    <ul className="lyo-fs-contents">
                        {folderKids.map((child) => (
                            <li key={treeNodeKey(child)}>
                                <button type="button" className="lyo-fs-content" onClick={() => onSelect(child)}>
                                    <NodeGlyph node={child} />
                                    <span>{child.isDirectory ? child.name : child.originalFileName || child.name}</span>
                                    <PresenceChip node={child} backendKind={backendKind} />
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        );
    }

    const open = (kind: FileDialogKind) => {
        if (fileId) onOpen(kind, fileId);
    };

    return (
        <div className="lyo-fs-inspector">
            <div className="lyo-fs-inspector-head">
                <NodeGlyph node={selected} />
                <div className="lyo-fs-inspector-titles">
                    <strong className="lyo-fs-file-title">{selected.originalFileName ?? selected.name}</strong>
                    <div className="lyo-fs-path">{selected.pathPrefix || "Root"}</div>
                </div>
                <PresenceChip node={selected} backendKind={backendKind} />
            </div>
            {selected.presence === "Physical" ? (
                <LyoAlert severity="info">
                    On {backendDisplayName(backendKind)} only — not in the catalog. Download and delete act on the raw object (not decrypted).
                </LyoAlert>
            ) : null}
            <div className="lyo-fs-actions" role="toolbar" aria-label="File actions">
                <ActionIcon label="Download" disabled={!canDownload} onClick={onDownload}>
                    <FileDownloadIcon fontSize="small" />
                </ActionIcon>
                <ActionIcon label="Access link" disabled={!catalog || !fileId} onClick={() => open("access")}>
                    <LinkIcon fontSize="small" />
                </ActionIcon>
                {allowPrefixChanges ? (
                    <ActionIcon label="Move" disabled={!catalog || !fileId} onClick={() => open("move")}>
                        <DriveFileMoveIcon fontSize="small" />
                    </ActionIcon>
                ) : null}
                {allowPrefixChanges ? (
                    <ActionIcon label="Copy" disabled={!catalog || !fileId} onClick={() => open("copy")}>
                        <ContentCopyIcon fontSize="small" />
                    </ActionIcon>
                ) : null}
                <ActionIcon label="Rename" disabled={!catalog || !fileId} onClick={() => open("rename")}>
                    <DriveFileRenameOutlineIcon fontSize="small" />
                </ActionIcon>
                <ActionIcon label="Rotate DEK" disabled={!catalog || !fileId} onClick={() => open("rotate")}>
                    <VpnKeyIcon fontSize="small" />
                </ActionIcon>
                <ActionIcon label="Delete" color="error" disabled={!canDownload} onClick={onDelete}>
                    <DeleteOutlineIcon fontSize="small" />
                </ActionIcon>
            </div>
            <div className="lyo-fs-meta">
                {metadataBusy ? <p className="lyo-fs-empty">Loading metadata…</p> : null}
                {metadata ? <FileMetadataTable metadata={metadata} /> : null}
                {!metadataBusy && !metadata && metadataStatus ? <p className="lyo-fs-empty">{metadataStatus}</p> : null}
            </div>
        </div>
    );
}

const impliedKindByGrain: Record<string, string> = {
    value: "string",
    year: "datetime",
    month: "datetime",
    day: "datetime",
    hour: "timeonly",
    minute: "timeonly",
    second: "timeonly",
    days: "timespan",
    hours: "timespan",
    minutes: "timespan",
    seconds: "timespan",
};

function grainsForKind(kind: string, kinds: FileStorageTagValueKindResponse[], type?: FileStorageTagTypeResponse): string[] {
    const match = kinds.find((item) => item.kind === kind);
    if (match) return match.grains;
    return type?.grains ?? [];
}

function firstGrain(kind: string, kinds: FileStorageTagValueKindResponse[], type?: FileStorageTagTypeResponse): string {
    const grains = grainsForKind(kind, kinds, type);
    return grains.find((grain) => grain.toLowerCase() !== "value") ?? "Value";
}

function formatGroupKey(tagType: string, kind: string, grain: string): string {
    const grainToken = grain.toLowerCase();
    if (kind === "string" && grainToken === "value") return tagType;
    if (grainToken !== "value" && impliedKindByGrain[grainToken] === kind) return `${tagType}:${grainToken}`;
    if (grainToken === "value") return `${tagType}:${kind}`;
    return `${tagType}:${kind}:${grainToken}`;
}
