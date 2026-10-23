"""REST paths from ``Lyo.Recipe.Models.Constants.Rest.Recipe`` (no leading slash)."""

ROUTE = "Recipe"
CATEGORIES = "Recipe/Categories"
INGREDIENTS = "Recipe/Ingredients"
RECIPES = "Recipe/Recipes"
ATTACHMENTS = "Recipe/Attachments"
ATTACHMENTS_UPLOAD = "Recipe/Attachments/upload"


def ingredient_lines_path(recipe_id: str) -> str:
    return f"{RECIPES}/{recipe_id}/IngredientLines"


def steps_path(recipe_id: str) -> str:
    return f"{RECIPES}/{recipe_id}/Steps"


def notes_path(parent_route: str, entity_id: str) -> str:
    parent = parent_route.strip().strip("/")
    return f"{parent}/{entity_id}/Notes"


def tags_path(parent_route: str, entity_id: str) -> str:
    parent = parent_route.strip().strip("/")
    return f"{parent}/{entity_id}/Tags"


def join_route_prefix(route_prefix: str | None, relative: str) -> str:
    relative = relative.lstrip("/")
    prefix = (route_prefix or "").strip().strip("/")
    return f"/{prefix}/{relative}" if prefix else f"/{relative}"
