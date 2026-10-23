"use client";

import {useState} from "react";
import type {AsyncFileStorageApiClient} from "lyo-file-storage-api-client";
import {LyoButton, LyoCheckbox, LyoFileUpload, LyoStack, LyoTextField, useLyoSnackbar} from "lyo-web-components";
import {showPresignedUpload} from "./fileStorageUi.js";

export function FilesTab({
    files,
    keyIds,
    onChanged,
}: {
    files: AsyncFileStorageApiClient;
    keyIds: string[];
    onChanged: () => void;
}) {
    const snackbar = useLyoSnackbar();
    const [pathPrefix, setPathPrefix] = useState("");
    const [compress, setCompress] = useState(false);
    const [encrypt, setEncrypt] = useState(false);
    const [keyId, setKeyId] = useState("");
    const [picked, setPicked] = useState<File | null>(null);
    const [pickerKey, setPickerKey] = useState(0);
    const [uploading, setUploading] = useState(false);
    const [presigned, setPresigned] = useState<string | null>(null);
    const [sourceKeyId, setSourceKeyId] = useState("");
    const [rotateIds, setRotateIds] = useState("");
    const [targetKeyId, setTargetKeyId] = useState("");
    const allowPresign = showPresignedUpload({compress, encrypt});

    const upload = async () => {
        if (!picked) return;
        setUploading(true);
        try {
            await files.saveStream({
                file: picked,
                fileName: picked.name,
                pathPrefix: pathPrefix || undefined,
                compress,
                encrypt,
                keyId: keyId || undefined,
            });
            snackbar.show("Uploaded to file storage.", "success");
            setPicked(null);
            setPickerKey((n) => n + 1);
            onChanged();
        } catch (err) {
            snackbar.show(err instanceof Error ? err.message : "Upload failed", "error");
        } finally {
            setUploading(false);
        }
    };

    return (
        <LyoStack spacing={2}>
            <LyoTextField label="Path prefix" value={pathPrefix} onChange={(event) => setPathPrefix(event.target.value)} />
            <label>
                <LyoCheckbox checked={compress} onChange={(event) => setCompress(event.target.checked)} /> Compress
            </label>
            <label>
                <LyoCheckbox checked={encrypt} onChange={(event) => setEncrypt(event.target.checked)} /> Encrypt
            </label>
            <LyoTextField label="Key id" value={keyId} onChange={(event) => setKeyId(event.target.value)} placeholder={keyIds[0] ?? ""} />
            <LyoFileUpload key={pickerKey} maxFiles={1} buttonText="Choose file" onFiles={(list) => setPicked(list[0] ?? null)} />
            <LyoButton variant="contained" disabled={!picked || uploading} onClick={() => void upload()}>
                {uploading ? "Uploading…" : "Upload"}
            </LyoButton>
            <LyoButton
                variant="text"
                disabled={!allowPresign || !picked || uploading}
                onClick={() => {
                    if (!picked) return;
                    void files
                        .presignedUploadUrl({
                            declaredMaxSizeBytes: picked.size,
                            originalFileName: picked.name,
                            pathPrefix: pathPrefix || undefined,
                            compress,
                            encrypt,
                        })
                        .then((result) => {
                            setPresigned(result.presignedPutUrl);
                            snackbar.show("Presigned upload URL ready.", "success");
                        })
                        .catch((err: unknown) => snackbar.show(err instanceof Error ? err.message : "Request failed", "error"));
                }}
            >
                Request presigned upload URL
            </LyoButton>
            {presigned ? (
                <span className="lyo-file-storage-presigned">
                    Presigned PUT URL. The file is not uploaded until something PUTs the bytes to this address.
                    <br />
                    {presigned}
                </span>
            ) : null}
            <LyoTextField label="Migrate source key id" value={sourceKeyId} onChange={(event) => setSourceKeyId(event.target.value)} />
            <LyoButton
                disabled={!sourceKeyId}
                onClick={() => {
                    void files.migrateDeks({sourceKeyId}).then(
                        () => snackbar.show("DEK migration requested.", "success"),
                        (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Request failed", "error")
                    );
                }}
            >
                Migrate DEKs
            </LyoButton>
            <LyoTextField label="Rotate file ids" value={rotateIds} onChange={(event) => setRotateIds(event.target.value)} />
            <LyoTextField label="Target key id" value={targetKeyId} onChange={(event) => setTargetKeyId(event.target.value)} />
            <LyoButton
                disabled={!rotateIds}
                onClick={() => {
                    const fileIds = rotateIds.split(",").map((id) => id.trim()).filter(Boolean);
                    void files.rotateDeks({fileIds, targetKeyId: targetKeyId || null}).then(
                        () => snackbar.show("DEK rotation requested.", "success"),
                        (err: unknown) => snackbar.show(err instanceof Error ? err.message : "Request failed", "error")
                    );
                }}
            >
                Rotate DEKs
            </LyoButton>
        </LyoStack>
    );
}
