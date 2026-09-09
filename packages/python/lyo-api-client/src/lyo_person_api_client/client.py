"""Person API client built on top of lyo_api_client."""

from __future__ import annotations

from lyo_api_client import ApiClient, ApiRequest, ApiResponse

from .models import ProjectionQueryReq, QueryConcreteReq, QueryReq


class PersonApiClient:
    """Typed endpoints for the Person Query API.

    Usage:

        api = ApiClient("http://localhost:5251")
        person_api = PersonApiClient(api)
        res = person_api.query_person(baseline_query(start=0, amount=10))
    """

    def __init__(self, api_client: ApiClient) -> None:
        self.api_client = api_client

    def query_person(self, request: QueryConcreteReq) -> ApiResponse:
        """POST /person/QueryConcrete — full entity results (QueryRes payload)."""
        return self.api_client.request(ApiRequest(method="POST", path="/person/QueryConcrete", body=request))

    def query_person_projected(self, request: ProjectionQueryReq) -> ApiResponse:
        """POST /person/QueryProject — projected rows (ProjectedQueryRes payload)."""
        return self.api_client.request(ApiRequest(method="POST", path="/person/QueryProject", body=request))

    def query_root(self, request: QueryReq) -> ApiResponse:
        """Root From/Joins query: POST /Query (not under /person)."""
        return self.api_client.request(ApiRequest(method="POST", path="/Query", body=request))

    def get_person(self, person_id: str, include: list[str] | None = None) -> ApiResponse:
        """GET /person/{id} with optional include navigation paths."""
        from urllib.parse import quote, urlencode

        path = f"/person/{quote(person_id, safe='')}"
        if include:
            qs = urlencode([("include", item) for item in include if item and item.strip()])
            if qs:
                path = f"{path}?{qs}"
        return self.api_client.request(ApiRequest(method="GET", path=path))
