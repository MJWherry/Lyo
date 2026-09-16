"use client";

import {useEffect, useMemo, useState} from "react";
import type {AsyncApiClient} from "lyo-api-client";
import {createAsyncFileStorageApiClient, type AsyncFileStorageApiClient} from "lyo-file-storage-api-client";
import {LyoAlert, LyoStack, LyoTabSet} from "lyo-web-components";
import {FileActionDialogs, type FileDialogKind} from "./dialogs.js";
import {FileBrowser} from "./FileBrowser.js";
import {FilesTab} from "./FilesTab.js";
import {FileTree} from "./FileTree.js";

export type FileStorageManagementProps = {
    apiClient: AsyncApiClient;
    files?: AsyncFileStorageApiClient;
    routePrefix?: string;
};

export function FileStorageManagement({apiClient, files, routePrefix}: FileStorageManagementProps) {
    const client = useMemo(
        () => files ?? createAsyncFileStorageApiClient(apiClient, {routePrefix}),
        [apiClient, files, routePrefix]
    );
    const [health, setHealth] = useState<string>("Checking file storage…");
    const [keyIds, setKeyIds] = useState<string[]>([]);
    const [reloadToken, setReloadToken] = useState(0);
    const [dialog, setDialog] = useState<FileDialogKind>(null);
    const [fileId, setFileId] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        void client.health().then(
            (response) => {
                if (!cancelled) setHealth(response.data?.isHealthy ? "File storage is healthy." : response.data?.message ?? "File storage is unhealthy.");
            },
            () => {
                if (!cancelled) setHealth("File storage health check failed.");
            }
        );
        void client.listKeyIds().then(
            (response) => {
                if (!cancelled) setKeyIds(response.data ?? []);
            },
            () => {
                if (!cancelled) setKeyIds([]);
            }
        );
        return () => {
            cancelled = true;
        };
    }, [client, reloadToken]);

    const open = (kind: FileDialogKind, id: string) => {
        setDialog(kind);
        setFileId(id);
    };

    return (
        <LyoStack spacing={2}>
            <LyoAlert severity="info">{health}</LyoAlert>
            <LyoTabSet
                tabs={[
                    {
                        id: "files",
                        text: "Files",
                        content: <FilesTab files={client} keyIds={keyIds} onChanged={() => setReloadToken((n) => n + 1)} />,
                    },
                    {
                        id: "tree",
                        text: "Tree",
                        content: <FileTree files={client} reloadToken={reloadToken} onOpen={open} />,
                    },
                    {
                        id: "browser",
                        text: "Browser",
                        content: <FileBrowser apiClient={apiClient} files={client} routePrefix={routePrefix} onOpen={open} />,
                    },
                ]}
            />
            <FileActionDialogs
                kind={dialog}
                fileId={fileId}
                files={client}
                onClose={() => setDialog(null)}
                onChanged={() => setReloadToken((n) => n + 1)}
            />
        </LyoStack>
    );
}
