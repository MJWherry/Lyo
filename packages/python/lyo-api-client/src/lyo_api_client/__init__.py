"""Core, reusable API client foundation for Lyo Python consumers."""

from .client import (
    ApiClient,
    build_url,
    entity_metadata_path,
    metadata_path,
    normalize_route_prefix,
    with_bearer_token,
)
from .errors import ApiClientError, to_api_client_error
from .models import ApiRequest, ApiResponse, HttpMethod, TransportRequest
from .transport import Transport, UrllibTransport

__all__ = [
    "ApiClient",
    "ApiClientError",
    "ApiRequest",
    "ApiResponse",
    "HttpMethod",
    "Transport",
    "TransportRequest",
    "UrllibTransport",
    "build_url",
    "entity_metadata_path",
    "metadata_path",
    "normalize_route_prefix",
    "to_api_client_error",
    "with_bearer_token",
]
