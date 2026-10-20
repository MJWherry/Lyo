"""Sync FileStorage API client built on ``lyo_api_client``."""

from __future__ import annotations

from typing import Any
from urllib.parse import urlencode

from lyo_api_client import ApiClient, ApiRequest, ApiResponse, HttpMethod, file_name_from_disposition, multipart_form

from . import routes
from .guard import assert_provider_download, assert_provider_upload
from .models import FileDownload, PresignedUploadResult, SaveStreamRequest
from .routes import join_route_prefix


def _header(headers: dict[str, str], name: str) -> str | None:
    for key, value in headers.items():
        if key.lower() == name:
            return value
    return None


def _as_download(response: ApiResponse) -> FileDownload:
    payload = response.body_bytes
    if payload is None and response.raw_body is not None:
        payload = response.raw_body.encode()
    return FileDownload(
        body=payload or b"",
        file_name=file_name_from_disposition(_header(response.headers, "content-disposition")),
        content_type=_header(response.headers, "content-type"),
        content_digest=_header(response.headers, "content-digest"),
    )


def _stream_query(args: SaveStreamRequest) -> dict[str, Any]:
    return {
        "originalFileName": args.original_file_name or args.file_name,
        "compress": False if args.compress is None else args.compress,
        "encrypt": False if args.encrypt is None else args.encrypt,
        "keyId": args.key_id,
        "pathPrefix": args.path_prefix,
        "chunkSize": args.chunk_size,
        "contentType": args.content_type,
        "charset": args.charset,
        "tenantId": args.tenant_id,
    }


class FileStorageApiClient:
    """FileStorage HTTP client. Downloads use the API blob routes."""

    def __init__(self, api_client: ApiClient, *, route_prefix: str | None = None, store_route: str | None = None) -> None:
        self._api = api_client
        self._prefix = route_prefix
        self._store_route = store_route

    def stores(self) -> ApiResponse:
        return self._send("GET", routes.stores_path())

    def health(self) -> ApiResponse:
        return self._send("GET", routes.HEALTH)

    def capabilities(self) -> ApiResponse:
        return self._send("GET", routes.CAPABILITIES)

    def list_key_ids(self) -> ApiResponse:
        return self._send("GET", routes.KEY_IDS)

    def list_folder(self, *, path_prefix: str | None = None, max_keys: int | None = None) -> ApiResponse:
        return self._send("GET", routes.FOLDER, query={"pathPrefix": path_prefix, "maxKeys": max_keys})

    def list_storage_keys(self, *, prefix: str | None = None, max_keys: int | None = None) -> ApiResponse:
        return self._send("GET", routes.STORAGE_KEYS, query={"prefix": prefix, "maxKeys": max_keys})

    def save_bytes(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.SAVE, body=request)

    def save_stream(self, request: SaveStreamRequest) -> ApiResponse:
        filename = request.file_name or request.original_file_name or "file.bin"
        body, content_type = multipart_form([("file", request.file, filename)])
        return self._send(
            "POST",
            routes.SAVE_STREAM,
            body=body,
            query=_stream_query(request),
            headers={"Content-Type": content_type},
        )

    def integrity(self, file_id: str) -> ApiResponse:
        return self._send("GET", routes.file_integrity_path(file_id))

    def get_metadata(self, file_id: str, include_deleted: bool = False) -> ApiResponse:
        query = {"includeDeleted": True} if include_deleted else None
        return self._send("GET", routes.file_metadata_path(file_id), query=query)

    def download(self, file_id: str, inline: bool = False) -> FileDownload:
        query = {"inline": True} if inline else None
        return _as_download(self._send("GET", routes.file_download_path(file_id), query=query))

    def download_archive(self, ids: list[str], file_name: str | None = None) -> FileDownload:
        pairs: list[tuple[str, str]] = [("id", item) for item in ids]
        if file_name:
            pairs.append(("fileName", file_name))
        return _as_download(self._send("GET", f"{routes.ARCHIVE}?{urlencode(pairs)}"))

    def copy(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.COPY, body=request)

    def move(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.MOVE, body=request)

    def rename(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.RENAME, body=request)

    def delete(self, file_id: str) -> ApiResponse:
        return self._send("DELETE", routes.file_delete_path(file_id))

    def create_access_link(self, file_id: str, request: Any = None) -> ApiResponse:
        return self._send("POST", routes.file_access_link_path(file_id), body={} if request is None else request)

    def presigned_read(self, file_id: str, *, expires_hours: int | None = None) -> ApiResponse:
        query = {"expiresHours": expires_hours} if expires_hours is not None else None
        return self._send("GET", routes.file_presigned_read_path(file_id), query=query)

    def presigned_download_url(self, metadata: Any) -> str:
        """Provider GET URL. Refuses encrypted or compressed files."""
        assert_provider_download(metadata)
        response = self.presigned_read(metadata.id)
        data = response.data if isinstance(response.data, dict) else {}
        return str(data.get("url") or "")

    def begin_direct_upload(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.DIRECT_UPLOAD_BEGIN, body=request)

    def presigned_upload_url(self, request: Any) -> PresignedUploadResult:
        """Provider PUT URL. Refuses compressed or encrypted uploads."""
        assert_provider_upload(request)
        response = self.begin_direct_upload(request)
        data = response.data if isinstance(response.data, dict) else {}
        return PresignedUploadResult(
            file_id=str(data.get("fileId") or ""),
            presigned_put_url=str(data.get("presignedPutUrl") or ""),
        )

    def complete_direct_upload(self, file_id: str, request: Any = None) -> ApiResponse:
        return self._send("POST", routes.direct_upload_complete_path(file_id), body={} if request is None else request)

    def put_direct_upload(self, file_id: str, body: bytes) -> ApiResponse:
        return self._send(
            "PUT",
            routes.direct_upload_put_path(file_id),
            body=body,
            headers={"Content-Type": "application/octet-stream"},
        )

    def begin_multipart(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.MULTIPART_BEGIN, body=request)

    def part_url(self, session_id: str, part_number: int) -> ApiResponse:
        return self._send("GET", routes.multipart_part_url_path(session_id), query={"partNumber": part_number})

    def complete_multipart(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.MULTIPART_COMPLETE, body=request)

    def abort_multipart(self, session_id: str) -> ApiResponse:
        return self._send("POST", routes.multipart_abort_path(session_id))

    def migrate_deks(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.MIGRATE_DEKS, body=request)

    def rotate_deks(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.ROTATE_DEKS, body=request)

    def download_physical(self, physical_key: str) -> FileDownload:
        return _as_download(self._send("GET", routes.PHYSICAL_DOWNLOAD, query={"physicalKey": physical_key}))

    def delete_physical(self, physical_key: str) -> ApiResponse:
        return self._send("DELETE", routes.PHYSICAL, query={"physicalKey": physical_key})

    def move_physical(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.PHYSICAL_MOVE, body=request)

    def copy_physical(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.PHYSICAL_COPY, body=request)

    def rename_physical(self, request: Any) -> ApiResponse:
        return self._send("POST", routes.PHYSICAL_RENAME, body=request)

    def query_concrete(self, request: Any) -> ApiResponse:
        return self._send("POST", f"{routes.FILE_METADATA}/QueryConcrete", body=request)

    def query_project(self, request: Any) -> ApiResponse:
        return self._send("POST", f"{routes.FILE_METADATA}/QueryProject", body=request)

    def _send(
        self,
        method: HttpMethod,
        relative: str,
        *,
        body: Any = None,
        query: dict[str, Any] | None = None,
        headers: dict[str, str] | None = None,
    ) -> ApiResponse:
        return self._api.request(
            ApiRequest(
                method=method,
                path=join_route_prefix(self._prefix, routes.apply_file_storage_store(relative, self._store_route)),
                body=body,
                query=query,
                headers=headers,
            )
        )
