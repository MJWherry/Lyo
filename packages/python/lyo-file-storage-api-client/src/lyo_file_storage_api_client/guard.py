"""Presigned provider URLs are only for plaintext, uncompressed objects."""

from __future__ import annotations

from typing import Any, Literal


class ProviderObjectBlockedError(Exception):
    """Raised when a presigned provider URL is requested for an encrypted or compressed object."""

    def __init__(self, kind: Literal["download", "upload"]) -> None:
        message = (
            "Presigned download is only available when the file is neither encrypted nor compressed. Use download."
            if kind == "download"
            else "Presigned upload is only available when the file is neither compressed nor encrypted. Use upload."
        )
        super().__init__(message)


def allows_provider_object(file: Any) -> bool:
    return getattr(file, "is_encrypted", None) is not True and getattr(file, "is_compressed", None) is not True


def assert_provider_download(file: Any) -> None:
    if not allows_provider_object(file):
        raise ProviderObjectBlockedError("download")


def assert_provider_upload(file: Any) -> None:
    if getattr(file, "encrypt", None) is True or getattr(file, "compress", None) is True:
        raise ProviderObjectBlockedError("upload")
