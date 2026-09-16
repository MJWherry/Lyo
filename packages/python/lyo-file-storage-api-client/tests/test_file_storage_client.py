from __future__ import annotations

import json

import pytest

from lyo_api_client import ApiClient, ApiResponse, TransportRequest
from lyo_file_storage_api_client import (
    BeginMultipartRequest,
    CompleteMultipartRequest,
    CompletedPart,
    CopyFileRequest,
    DirectUploadBeginRequest,
    DirectUploadCompleteRequest,
    FileStorageApiClient,
    FileStoreResult,
    MigrateDeksRequest,
    MoveFileRequest,
    MovePhysicalRequest,
    PresignedUploadRequest,
    ProviderObjectBlockedError,
    RenameFileRequest,
    RenamePhysicalRequest,
    RotateDeksRequest,
    SaveFileRequest,
    SaveStreamRequest,
    join_route_prefix,
)

_FILE_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"


class _ScriptedTransport:
    def __init__(self, handler) -> None:
        self.handler = handler
        self.requests: list[TransportRequest] = []

    def __call__(self, request: TransportRequest) -> ApiResponse:
        self.requests.append(request)
        return self.handler(request)


def test_join_route_prefix():
    assert join_route_prefix(None, "FileStorage/files/folder") == "/FileStorage/files/folder"
    assert join_route_prefix("api", "FileStorage/files/folder") == "/api/FileStorage/files/folder"


def test_route_matrix():
    def handler(request: TransportRequest) -> ApiResponse:
        if "/download" in request.url or "/archive" in request.url:
            return ApiResponse(
                status=200,
                ok=True,
                body_bytes=b"bytes",
                headers={"content-disposition": 'attachment; filename="a.bin"'},
            )
        if "presigned-read" in request.url:
            return ApiResponse(status=200, ok=True, data={"url": "https://provider.example/get"})
        if "direct-upload/begin" in request.url:
            return ApiResponse(
                status=200,
                ok=True,
                data={
                    "fileId": _FILE_ID,
                    "presignedPutUrl": "https://provider.example/put",
                    "urlExpiresUtc": "2026-01-01T00:00:00Z",
                    "storageLocation": "s3",
                },
            )
        return ApiResponse(status=200, ok=True, data={"id": _FILE_ID})

    transport = _ScriptedTransport(handler)
    files = FileStorageApiClient(ApiClient("http://x", transport=transport))
    files.health()
    files.list_key_ids()
    files.list_folder(path_prefix="docs", max_keys=10)
    files.list_storage_keys(prefix="docs", max_keys=5)
    files.save_bytes(SaveFileRequest(data="YQ==", original_file_name="a.bin", path_prefix="docs"))
    files.save_stream(
        SaveStreamRequest(file=b"hi", file_name="a.bin", compress=False, encrypt=True, key_id="k1", path_prefix="docs")
    )
    files.get_metadata(_FILE_ID)
    files.download(_FILE_ID)
    files.download_archive([_FILE_ID], "bundle.zip")
    files.copy(CopyFileRequest(source_file_id=_FILE_ID, path_prefix="other"))
    files.move(MoveFileRequest(file_id=_FILE_ID, path_prefix="other"))
    files.rename(RenameFileRequest(file_id=_FILE_ID, original_file_name="b.bin"))
    files.delete(_FILE_ID)
    files.create_access_link(_FILE_ID, {"maxDownloads": 1})
    files.presigned_read(_FILE_ID)
    files.begin_direct_upload(DirectUploadBeginRequest(declared_max_size_bytes=10, original_file_name="a.bin"))
    files.complete_direct_upload(_FILE_ID, DirectUploadCompleteRequest(expected_byte_length=2))
    files.put_direct_upload(_FILE_ID, b"hi")
    files.begin_multipart(BeginMultipartRequest(original_file_name="a.bin", part_size_bytes=1024))
    files.part_url(_FILE_ID, 1)
    files.complete_multipart(
        CompleteMultipartRequest(session_id=_FILE_ID, parts=[CompletedPart(part_number=1, etag_or_block_id="etag")])
    )
    files.abort_multipart(_FILE_ID)
    files.migrate_deks(MigrateDeksRequest(source_key_id="k1"))
    files.rotate_deks(RotateDeksRequest(file_ids=[_FILE_ID], target_key_id="k2"))
    files.download_physical("obj/key")
    files.delete_physical("obj/key")
    files.move_physical(MovePhysicalRequest(physical_key="obj/key", destination_path_prefix="next"))
    files.copy_physical(MovePhysicalRequest(physical_key="obj/key", destination_path_prefix="next"))
    files.rename_physical(RenamePhysicalRequest(physical_key="obj/key", new_file_name="b.bin"))
    files.query_concrete({"Amount": 10})
    files.query_project({"Amount": 10, "Select": ["Id"]})

    calls = transport.requests
    url = lambda index: calls[index].url
    assert (calls[0].method, url(0)) == ("GET", "http://x/FileStorage/health")
    assert (calls[1].method, url(1)) == ("GET", "http://x/FileStorage/key-ids")
    assert "/FileStorage/files/folder" in url(2) and "pathPrefix=docs" in url(2)
    assert "/FileStorage/diagnostics/storage-keys" in url(3)
    assert (calls[4].method, url(4)) == ("POST", "http://x/FileStorage/files/save")
    assert json.loads(calls[4].body)["data"] == "YQ=="
    assert json.loads(calls[4].body)["pathPrefix"] == "docs"
    assert calls[5].method == "POST"
    assert "/FileStorage/files/save-stream" in url(5)
    assert "encrypt=true" in url(5) and "keyId=k1" in url(5) and "pathPrefix=docs" in url(5)
    assert isinstance(calls[5].body, bytes)
    assert calls[5].headers["Content-Type"].startswith("multipart/form-data")
    assert (calls[6].method, url(6)) == ("GET", f"http://x/FileStorage/files/{_FILE_ID}/metadata")
    assert (calls[7].method, url(7)) == ("GET", f"http://x/FileStorage/files/{_FILE_ID}/download")
    assert "/FileStorage/files/archive" in url(8) and f"id={_FILE_ID}" in url(8)
    assert (calls[9].method, url(9)) == ("POST", "http://x/FileStorage/files/copy")
    assert (calls[10].method, url(10)) == ("POST", "http://x/FileStorage/files/move")
    assert (calls[11].method, url(11)) == ("POST", "http://x/FileStorage/files/rename")
    assert (calls[12].method, url(12)) == ("DELETE", f"http://x/FileStorage/files/{_FILE_ID}")
    assert (calls[13].method, url(13)) == ("POST", f"http://x/FileStorage/files/{_FILE_ID}/access-links")
    assert (calls[14].method, url(14)) == ("GET", f"http://x/FileStorage/files/{_FILE_ID}/presigned-read")
    assert (calls[15].method, url(15)) == ("POST", "http://x/FileStorage/direct-upload/begin")
    assert (calls[16].method, url(16)) == ("POST", f"http://x/FileStorage/direct-upload/{_FILE_ID}/complete")
    assert (calls[17].method, url(17)) == ("PUT", f"http://x/FileStorage/direct-upload/{_FILE_ID}/put")
    assert (calls[18].method, url(18)) == ("POST", "http://x/FileStorage/multipart/begin")
    assert f"/FileStorage/multipart/{_FILE_ID}/part-url" in url(19) and "partNumber=1" in url(19)
    assert (calls[20].method, url(20)) == ("POST", "http://x/FileStorage/multipart/complete")
    assert (calls[21].method, url(21)) == ("POST", f"http://x/FileStorage/multipart/{_FILE_ID}/abort")
    assert (calls[22].method, url(22)) == ("POST", "http://x/FileStorage/files/migrate-deks")
    assert (calls[23].method, url(23)) == ("POST", "http://x/FileStorage/files/rotate-deks")
    assert "/FileStorage/files/physical/download" in url(24)
    assert calls[25].method == "DELETE" and "/FileStorage/files/physical" in url(25)
    assert (calls[26].method, url(26)) == ("POST", "http://x/FileStorage/files/physical/move")
    assert (calls[27].method, url(27)) == ("POST", "http://x/FileStorage/files/physical/copy")
    assert (calls[28].method, url(28)) == ("POST", "http://x/FileStorage/files/physical/rename")
    assert (calls[29].method, url(29)) == ("POST", "http://x/FileStorage/FileMetadata/QueryConcrete")
    assert (calls[30].method, url(30)) == ("POST", "http://x/FileStorage/FileMetadata/QueryProject")


def test_capabilities_gets_the_capabilities_route():
    def handler(request: TransportRequest) -> ApiResponse:
        return ApiResponse(status=200, ok=True, data={"presignedRead": True, "presignedMultipartParts": False, "encryption": False})

    transport = _ScriptedTransport(handler)
    files = FileStorageApiClient(ApiClient("http://x", transport=transport))
    response = files.capabilities()
    assert transport.requests[0].method == "GET"
    assert transport.requests[0].url == "http://x/FileStorage/capabilities"
    assert response.data["presignedMultipartParts"] is False
    assert response.data["encryption"] is False


def test_presigned_urls_refuse_encrypted_or_compressed():
    calls: list[str] = []

    def handler(request: TransportRequest) -> ApiResponse:
        calls.append(request.url)
        return ApiResponse(
            status=200,
            ok=True,
            data={"url": "https://provider.example/get", "fileId": _FILE_ID, "presignedPutUrl": "https://provider.example/put"},
        )

    files = FileStorageApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    blocked = FileStoreResult(id=_FILE_ID, is_encrypted=False, is_compressed=False, original_file_name="a.bin")
    with pytest.raises(ProviderObjectBlockedError, match="download"):
        files.presigned_download_url(FileStoreResult(id=_FILE_ID, is_encrypted=True, is_compressed=False))
    with pytest.raises(ProviderObjectBlockedError, match="download"):
        files.presigned_download_url(FileStoreResult(id=_FILE_ID, is_encrypted=False, is_compressed=True))
    with pytest.raises(ProviderObjectBlockedError, match="upload"):
        files.presigned_upload_url(PresignedUploadRequest(declared_max_size_bytes=4, encrypt=True))
    with pytest.raises(ProviderObjectBlockedError, match="upload"):
        files.presigned_upload_url(PresignedUploadRequest(declared_max_size_bytes=4, compress=True))
    assert calls == []

    download_url = files.presigned_download_url(blocked)
    upload = files.presigned_upload_url(PresignedUploadRequest(declared_max_size_bytes=4, original_file_name="a.bin"))
    assert download_url == "https://provider.example/get"
    assert upload.presigned_put_url == "https://provider.example/put"
    assert upload.file_id == _FILE_ID
    assert calls[0] == f"http://x/FileStorage/files/{_FILE_ID}/presigned-read"
    assert calls[1] == "http://x/FileStorage/direct-upload/begin"


def test_download_returns_bytes_from_api_route():
    def handler(request: TransportRequest) -> ApiResponse:
        assert request.method == "GET"
        assert request.url == f"http://x/FileStorage/files/{_FILE_ID}/download"
        return ApiResponse(
            status=200,
            ok=True,
            body_bytes=b"plain",
            headers={
                "content-disposition": 'attachment; filename="a.bin"',
                "content-type": "application/octet-stream",
            },
        )

    files = FileStorageApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    saved = files.download(_FILE_ID)
    assert saved.file_name == "a.bin"
    assert saved.body == b"plain"
    assert saved.content_digest is None


def test_integrity_and_content_digest():
    digest = "sha-256=:AQIDBA==:"

    def handler(request: TransportRequest) -> ApiResponse:
        if request.url.endswith("/integrity"):
            assert request.method == "GET"
            return ApiResponse(
                status=200,
                ok=True,
                data={"hashAlgorithm": "sha256", "originalFileHash": "AQIDBA==", "originalFileSize": 4},
            )
        return ApiResponse(
            status=200,
            ok=True,
            body_bytes=b"plain",
            headers={"content-digest": digest, "content-disposition": 'attachment; filename="a.bin"'},
        )

    files = FileStorageApiClient(ApiClient("http://x", transport=_ScriptedTransport(handler)))
    integrity = files.integrity(_FILE_ID)
    saved = files.download(_FILE_ID)
    assert integrity.data["hashAlgorithm"] == "sha256"
    assert integrity.data["originalFileHash"] == "AQIDBA=="
    assert saved.content_digest == digest
