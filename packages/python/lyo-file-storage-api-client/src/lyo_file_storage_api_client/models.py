"""FileStorage request models. Snake_case attributes; camelCase ``to_dict()``."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is not None:
        out[key] = value


@dataclass
class FileStoreResult:
    """Metadata used to decide whether a presigned download is allowed."""

    id: str
    is_encrypted: bool | None = None
    is_compressed: bool | None = None
    original_file_name: str | None = None


@dataclass
class FileDownload:
    """Bytes saved from an API download route, not a provider URL."""

    body: bytes
    file_name: str | None = None
    content_type: str | None = None
    content_digest: str | None = None


@dataclass
class SaveFileRequest:
    data: str
    original_file_name: str | None = None
    compress: bool | None = None
    encrypt: bool | None = None
    key_id: str | None = None
    path_prefix: str | None = None
    content_type: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"data": self.data}
        _put(out, "originalFileName", self.original_file_name)
        if self.compress is not None:
            out["compress"] = self.compress
        if self.encrypt is not None:
            out["encrypt"] = self.encrypt
        _put(out, "keyId", self.key_id)
        _put(out, "pathPrefix", self.path_prefix)
        _put(out, "contentType", self.content_type)
        return out


@dataclass
class SaveStreamRequest:
    file: bytes
    file_name: str | None = None
    original_file_name: str | None = None
    compress: bool | None = None
    encrypt: bool | None = None
    key_id: str | None = None
    path_prefix: str | None = None
    chunk_size: int | None = None
    content_type: str | None = None
    charset: str | None = None
    tenant_id: str | None = None


@dataclass
class CopyFileRequest:
    source_file_id: str
    path_prefix: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"sourceFileId": self.source_file_id}
        _put(out, "pathPrefix", self.path_prefix)
        return out


@dataclass
class MoveFileRequest:
    file_id: str
    path_prefix: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"fileId": self.file_id}
        _put(out, "pathPrefix", self.path_prefix)
        return out


@dataclass
class RenameFileRequest:
    file_id: str
    original_file_name: str

    def to_dict(self) -> dict[str, str]:
        return {"fileId": self.file_id, "originalFileName": self.original_file_name}


@dataclass
class DirectUploadBeginRequest:
    declared_max_size_bytes: int
    original_file_name: str | None = None
    path_prefix: str | None = None
    content_type: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"declaredMaxSizeBytes": self.declared_max_size_bytes}
        _put(out, "originalFileName", self.original_file_name)
        _put(out, "pathPrefix", self.path_prefix)
        _put(out, "contentType", self.content_type)
        return out


@dataclass
class PresignedUploadRequest:
    declared_max_size_bytes: int
    original_file_name: str | None = None
    path_prefix: str | None = None
    content_type: str | None = None
    compress: bool | None = None
    encrypt: bool | None = None

    def to_dict(self) -> dict[str, Any]:
        """Begin-upload body. Encrypt and compress are not sent to the provider URL."""
        out: dict[str, Any] = {"declaredMaxSizeBytes": self.declared_max_size_bytes}
        _put(out, "originalFileName", self.original_file_name)
        _put(out, "pathPrefix", self.path_prefix)
        _put(out, "contentType", self.content_type)
        return out


@dataclass
class PresignedUploadResult:
    file_id: str
    presigned_put_url: str


@dataclass
class DirectUploadCompleteRequest:
    expected_byte_length: int | None = None
    original_file_name: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "expectedByteLength", self.expected_byte_length)
        _put(out, "originalFileName", self.original_file_name)
        return out


@dataclass
class BeginMultipartRequest:
    original_file_name: str | None = None
    part_size_bytes: int | None = None
    compress: bool | None = None
    encrypt: bool | None = None
    key_id: str | None = None
    path_prefix: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "originalFileName", self.original_file_name)
        _put(out, "partSizeBytes", self.part_size_bytes)
        if self.compress is not None:
            out["compress"] = self.compress
        if self.encrypt is not None:
            out["encrypt"] = self.encrypt
        _put(out, "keyId", self.key_id)
        _put(out, "pathPrefix", self.path_prefix)
        return out


@dataclass
class CompletedPart:
    part_number: int
    etag_or_block_id: str

    def to_dict(self) -> dict[str, Any]:
        return {"partNumber": self.part_number, "eTagOrBlockId": self.etag_or_block_id}


@dataclass
class CompleteMultipartRequest:
    session_id: str
    parts: list[CompletedPart] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "sessionId": self.session_id,
            "parts": [part.to_dict() if hasattr(part, "to_dict") else part for part in self.parts],
        }


@dataclass
class MigrateDeksRequest:
    source_key_id: str
    target_key_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"sourceKeyId": self.source_key_id}
        _put(out, "targetKeyId", self.target_key_id)
        return out


@dataclass
class RotateDeksRequest:
    file_ids: list[str]
    target_key_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"fileIds": list(self.file_ids)}
        _put(out, "targetKeyId", self.target_key_id)
        return out


@dataclass
class MovePhysicalRequest:
    physical_key: str
    destination_path_prefix: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"physicalKey": self.physical_key}
        _put(out, "destinationPathPrefix", self.destination_path_prefix)
        return out


@dataclass
class RenamePhysicalRequest:
    physical_key: str
    new_file_name: str

    def to_dict(self) -> dict[str, str]:
        return {"physicalKey": self.physical_key, "newFileName": self.new_file_name}
