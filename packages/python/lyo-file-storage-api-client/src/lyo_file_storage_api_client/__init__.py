"""FileStorage API client package built on top of lyo_api_client."""

from .client import FileStorageApiClient
from .guard import ProviderObjectBlockedError
from .models import (
    BeginMultipartRequest,
    CompleteMultipartRequest,
    CompletedPart,
    CopyFileRequest,
    DirectUploadBeginRequest,
    DirectUploadCompleteRequest,
    FileDownload,
    FileStoreResult,
    MigrateDeksRequest,
    MoveFileRequest,
    MovePhysicalRequest,
    PresignedUploadRequest,
    PresignedUploadResult,
    RenameFileRequest,
    RenamePhysicalRequest,
    RotateDeksRequest,
    SaveFileRequest,
    SaveStreamRequest,
)
from .routes import join_route_prefix

__all__ = [
    "BeginMultipartRequest",
    "CompleteMultipartRequest",
    "CompletedPart",
    "CopyFileRequest",
    "DirectUploadBeginRequest",
    "DirectUploadCompleteRequest",
    "FileDownload",
    "FileStorageApiClient",
    "FileStoreResult",
    "MigrateDeksRequest",
    "MoveFileRequest",
    "MovePhysicalRequest",
    "PresignedUploadRequest",
    "PresignedUploadResult",
    "ProviderObjectBlockedError",
    "RenameFileRequest",
    "RenamePhysicalRequest",
    "RotateDeksRequest",
    "SaveFileRequest",
    "SaveStreamRequest",
    "join_route_prefix",
]
