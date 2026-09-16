"""Sync Recipe API client built on ``lyo_api_client``."""

from __future__ import annotations

from typing import Any
from urllib.parse import quote

from lyo_api_client import ApiClient, ApiRequest, ApiResponse, PatchRequest, multipart_form
from lyo_person_api_client.models import QueryConcreteReq

from . import routes
from .models import (
    RecipeIngredientLineReq,
    RecipeNoteCreateReq,
    RecipeStepReq,
    RecipeTagAddReq,
    RecipeUpload,
)
from .routes import join_route_prefix


def _wire(body: Any) -> Any:
    if isinstance(body, list):
        return [_wire(item) for item in body]
    if hasattr(body, "to_dict"):
        return body.to_dict()
    return body


class _Crud:
    def __init__(self, api: ApiClient, route: str) -> None:
        self._api = api
        self.route = route

    def query_concrete(self, request: QueryConcreteReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryConcrete", body=request))

    def query_project(self, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QueryProject", body=request))

    def query_sql(self, request: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=f"{self.route}/QuerySql", body=request))

    def get(self, entity_id: str, include: list[str] | None = None) -> ApiResponse:
        return self._api.get_by_id(self.route, entity_id, include)

    def create(self, body: Any) -> ApiResponse:
        return self._api.request(ApiRequest(method="POST", path=self.route, body=body))

    def update(self, entity_id: str, body: Any) -> ApiResponse:
        data = body.to_dict() if hasattr(body, "to_dict") else body
        return self._api.request(
            ApiRequest(method="POST", path=f"{self.route}/Update", body={"keys": [entity_id], "data": data})
        )

    def patch(self, entity_id: str, body: PatchRequest | dict[str, Any]) -> ApiResponse:
        if isinstance(body, PatchRequest) and body.keys is None:
            body.keys = [[entity_id]]
        return self._api.patch(self.route, body)

    def delete_by_id(self, entity_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="DELETE", path=f"{self.route}/{entity_id}"))


class _Attachments(_Crud):
    def __init__(self, api: ApiClient, route: str, upload_route: str) -> None:
        super().__init__(api, route)
        self._upload = upload_route

    def upload(self, args: RecipeUpload) -> ApiResponse:
        """``POST Recipe/Attachments/upload`` as multipart form data."""
        parts: list[tuple[str, str] | tuple[str, bytes, str]] = [
            ("entityType", args.entity_type),
            ("entityId", args.entity_id),
            ("role", args.role or "Document"),
        ]
        if args.caption is not None:
            parts.append(("caption", args.caption))
        parts.append(("isPrimary", "true" if args.is_primary else "false"))
        parts.append(("file", args.file, args.file_name or "file.bin"))
        body, content_type = multipart_form(parts)
        return self._api.request(
            ApiRequest(method="POST", path=self._upload, body=body, headers={"Content-Type": content_type})
        )


class RecipeApiClient:
    """Categories, ingredients, recipes, attachments, lines, steps, notes, and tags."""

    def __init__(self, api_client: ApiClient, *, route_prefix: str | None = None) -> None:
        self._api = api_client
        self._p = lambda relative: join_route_prefix(route_prefix, relative)
        self.categories = _Crud(api_client, self._p(routes.CATEGORIES))
        self.ingredients = _Crud(api_client, self._p(routes.INGREDIENTS))
        self.recipes = _Crud(api_client, self._p(routes.RECIPES))
        self.attachments = _Attachments(api_client, self._p(routes.ATTACHMENTS), self._p(routes.ATTACHMENTS_UPLOAD))

    def get_ingredient_lines(self, recipe_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="GET", path=self._p(routes.ingredient_lines_path(recipe_id))))

    def replace_ingredient_lines(self, recipe_id: str, lines: list[RecipeIngredientLineReq | dict[str, Any]]) -> ApiResponse:
        return self._api.request(
            ApiRequest(method="PUT", path=self._p(routes.ingredient_lines_path(recipe_id)), body=_wire(lines))
        )

    def get_steps(self, recipe_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="GET", path=self._p(routes.steps_path(recipe_id))))

    def replace_steps(self, recipe_id: str, steps: list[RecipeStepReq | dict[str, Any]]) -> ApiResponse:
        return self._api.request(ApiRequest(method="PUT", path=self._p(routes.steps_path(recipe_id)), body=_wire(steps)))

    def get_notes(self, parent_route: str, entity_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="GET", path=self._p(routes.notes_path(parent_route, entity_id))))

    def add_note(self, parent_route: str, entity_id: str, request: RecipeNoteCreateReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(
            ApiRequest(method="POST", path=self._p(routes.notes_path(parent_route, entity_id)), body=request)
        )

    def delete_note(self, parent_route: str, entity_id: str, note_id: str) -> ApiResponse:
        path = f"{self._p(routes.notes_path(parent_route, entity_id))}/{note_id}"
        return self._api.request(ApiRequest(method="DELETE", path=path))

    def get_tags(self, parent_route: str, entity_id: str) -> ApiResponse:
        return self._api.request(ApiRequest(method="GET", path=self._p(routes.tags_path(parent_route, entity_id))))

    def add_tag(self, parent_route: str, entity_id: str, request: RecipeTagAddReq | dict[str, Any]) -> ApiResponse:
        return self._api.request(
            ApiRequest(method="POST", path=self._p(routes.tags_path(parent_route, entity_id)), body=request)
        )

    def remove_tag(self, parent_route: str, entity_id: str, name: str, tag_type: str | None = None) -> ApiResponse:
        path = f"{self._p(routes.tags_path(parent_route, entity_id))}/{quote(name, safe='')}"
        query = {"tagType": tag_type} if tag_type else None
        return self._api.request(ApiRequest(method="DELETE", path=path, query=query))
