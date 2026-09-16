"""Stdlib multipart bodies for uploads. No third-party HTTP library."""

from __future__ import annotations

import uuid
from collections.abc import Sequence

Part = tuple[str, str] | tuple[str, bytes, str]


def multipart_form(parts: Sequence[Part]) -> tuple[bytes, str]:
    """Build a ``multipart/form-data`` payload and its ``Content-Type``.

    A part is ``(name, text)`` or ``(name, file_bytes, filename)``.
    """
    boundary = f"----lyo{uuid.uuid4().hex}"
    chunks: list[bytes] = []
    for part in parts:
        chunks.append(f"--{boundary}\r\n".encode())
        if len(part) == 3:
            name, data, filename = part
            safe_name = filename.replace('"', "")
            chunks.append(
                (
                    f'Content-Disposition: form-data; name="{name}"; filename="{safe_name}"\r\n'
                    "Content-Type: application/octet-stream\r\n\r\n"
                ).encode()
            )
            chunks.append(data)
        else:
            name, value = part
            chunks.append(f'Content-Disposition: form-data; name="{name}"\r\n\r\n{value}'.encode())
        chunks.append(b"\r\n")
    chunks.append(f"--{boundary}--\r\n".encode())
    return b"".join(chunks), f"multipart/form-data; boundary={boundary}"
