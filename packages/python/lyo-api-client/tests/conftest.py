from __future__ import annotations

import pytest

from lyo_api_client import ApiResponse, TransportRequest


class StubTransport:
    """Records the resolved request and returns a canned response (no network)."""

    def __init__(self, response: ApiResponse | None = None) -> None:
        self.response = response or ApiResponse(status=200, ok=True, data={})
        self.requests: list[TransportRequest] = []

    def __call__(self, request: TransportRequest) -> ApiResponse:
        self.requests.append(request)
        return self.response

    @property
    def last(self) -> TransportRequest:
        return self.requests[-1]


@pytest.fixture
def stub_transport() -> StubTransport:
    return StubTransport()
