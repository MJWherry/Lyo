"""REST paths from ``Lyo.Api.FileStorage`` (no leading slash)."""

ROUTE = "FileStorage"
FILE_METADATA = "FileStorage/FileMetadata"
HEALTH = "FileStorage/health"
CAPABILITIES = "FileStorage/capabilities"
KEY_IDS = "FileStorage/key-ids"
FOLDER = "FileStorage/files/folder"
STORAGE_KEYS = "FileStorage/diagnostics/storage-keys"
SAVE = "FileStorage/files/save"
SAVE_STREAM = "FileStorage/files/save-stream"
COPY = "FileStorage/files/copy"
MOVE = "FileStorage/files/move"
RENAME = "FileStorage/files/rename"
ARCHIVE = "FileStorage/files/archive"
DIRECT_UPLOAD_BEGIN = "FileStorage/direct-upload/begin"
MULTIPART_BEGIN = "FileStorage/multipart/begin"
MULTIPART_COMPLETE = "FileStorage/multipart/complete"
MIGRATE_DEKS = "FileStorage/files/migrate-deks"
ROTATE_DEKS = "FileStorage/files/rotate-deks"
PHYSICAL_DOWNLOAD = "FileStorage/files/physical/download"
PHYSICAL = "FileStorage/files/physical"
PHYSICAL_MOVE = "FileStorage/files/physical/move"
PHYSICAL_COPY = "FileStorage/files/physical/copy"
PHYSICAL_RENAME = "FileStorage/files/physical/rename"


def file_metadata_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}/metadata"


def file_download_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}/download"


def file_integrity_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}/integrity"


def file_delete_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}"


def file_access_link_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}/access-links"


def file_presigned_read_path(file_id: str) -> str:
    return f"{ROUTE}/files/{file_id}/presigned-read"


def direct_upload_complete_path(file_id: str) -> str:
    return f"{ROUTE}/direct-upload/{file_id}/complete"


def direct_upload_put_path(file_id: str) -> str:
    return f"{ROUTE}/direct-upload/{file_id}/put"


def multipart_part_url_path(session_id: str) -> str:
    return f"{ROUTE}/multipart/{session_id}/part-url"


def multipart_abort_path(session_id: str) -> str:
    return f"{ROUTE}/multipart/{session_id}/abort"


def join_route_prefix(route_prefix: str | None, relative: str) -> str:
    query_at = relative.find("?")
    path_only = relative[:query_at] if query_at >= 0 else relative
    query = relative[query_at:] if query_at >= 0 else ""
    relative_path = path_only.lstrip("/")
    prefix = (route_prefix or "").strip().strip("/")
    base = f"/{prefix}/{relative_path}" if prefix else f"/{relative_path}"
    return f"{base}{query}"
