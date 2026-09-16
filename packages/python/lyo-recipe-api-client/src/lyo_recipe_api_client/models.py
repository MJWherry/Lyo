"""Recipe request models. Snake_case attributes; camelCase ``to_dict()``."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is not None:
        out[key] = value


@dataclass
class RecipeCategoryReq:
    name: str
    slug: str
    parent_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"name": self.name, "slug": self.slug}
        _put(out, "parentId", self.parent_id)
        return out


@dataclass
class IngredientReq:
    name: str
    slug: str
    default_unit: str | None = None
    notes: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"name": self.name, "slug": self.slug}
        _put(out, "defaultUnit", self.default_unit)
        _put(out, "notes", self.notes)
        return out


@dataclass
class RecipeReq:
    title: str
    slug: str
    description: str | None = None
    servings: int | None = None
    prep_minutes: int | None = None
    cook_minutes: int | None = None
    difficulty: str | int | None = None
    status: str | int | None = None
    source_name: str | None = None
    source_url: str | None = None
    yield_label: str | None = None
    category_id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"title": self.title, "slug": self.slug}
        _put(out, "description", self.description)
        _put(out, "servings", self.servings)
        _put(out, "prepMinutes", self.prep_minutes)
        _put(out, "cookMinutes", self.cook_minutes)
        _put(out, "difficulty", self.difficulty)
        _put(out, "status", self.status)
        _put(out, "sourceName", self.source_name)
        _put(out, "sourceUrl", self.source_url)
        _put(out, "yieldLabel", self.yield_label)
        _put(out, "categoryId", self.category_id)
        return out


@dataclass
class RecipeIngredientLineReq:
    sort_order: int
    ingredient_id: str | None = None
    ingredient_text: str | None = None
    quantity: float | None = None
    unit: str | None = None
    preparation: str | None = None
    is_optional: bool | None = None
    group_label: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"sortOrder": self.sort_order}
        _put(out, "ingredientId", self.ingredient_id)
        _put(out, "ingredientText", self.ingredient_text)
        _put(out, "quantity", self.quantity)
        _put(out, "unit", self.unit)
        _put(out, "preparation", self.preparation)
        if self.is_optional is not None:
            out["isOptional"] = self.is_optional
        _put(out, "groupLabel", self.group_label)
        return out


@dataclass
class RecipeStepReq:
    sort_order: int
    instruction: str
    duration_minutes: int | None = None
    group_label: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"sortOrder": self.sort_order, "instruction": self.instruction}
        _put(out, "durationMinutes", self.duration_minutes)
        _put(out, "groupLabel", self.group_label)
        return out


@dataclass
class RecipeAttachmentReq:
    entity_type: str
    entity_id: str
    file_id: str
    role: str | int | None = None
    caption: str | None = None
    is_primary: bool | None = None
    sort_order: int | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "entityType": self.entity_type,
            "entityId": self.entity_id,
            "fileId": self.file_id,
        }
        _put(out, "role", self.role)
        _put(out, "caption", self.caption)
        if self.is_primary is not None:
            out["isPrimary"] = self.is_primary
        _put(out, "sortOrder", self.sort_order)
        return out


@dataclass
class RecipeNoteCreateReq:
    content: str

    def to_dict(self) -> dict[str, str]:
        return {"content": self.content}


@dataclass
class RecipeTagAddReq:
    name: str
    tag_type: str | None = None
    slug: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"name": self.name}
        _put(out, "tagType", self.tag_type)
        _put(out, "slug", self.slug)
        return out


@dataclass
class RecipeUpload:
    """Multipart attachment upload. ``file`` is the raw bytes."""

    entity_type: str
    entity_id: str
    file: bytes
    file_name: str | None = None
    role: str | None = None
    caption: str | None = None
    is_primary: bool = False
