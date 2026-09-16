"use client";

import type {FileStoreResult} from "lyo-file-storage-api-client";
import {metadataRows} from "./fileStorageUi.js";

/** Label/value metadata table. Same rows as Blazor `FileStoreMetadataTable`. */
export function FileMetadataTable({metadata}: {metadata: FileStoreResult}) {
    return (
        <table className="lyo-file-storage-meta">
            <tbody>
                {metadataRows(metadata).map((row) => (
                    <tr key={row.label}>
                        <th scope="row">{row.label}</th>
                        <td>{row.value}</td>
                    </tr>
                ))}
            </tbody>
        </table>
    );
}
