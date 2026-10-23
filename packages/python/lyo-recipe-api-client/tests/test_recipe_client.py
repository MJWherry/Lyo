from __future__ import annotations

import json

from lyo_api_client import ApiClient, ApiResponse
from lyo_person_api_client import QueryConcreteReq
from lyo_recipe_api_client import (
    RecipeApiClient,
    RecipeCategoryReq,
    RecipeIngredientLineReq,
    RecipeNoteCreateReq,
    RecipeReq,
    RecipeStepReq,
    RecipeTagAddReq,
    RecipeUpload,
    join_route_prefix,
)

from conftest import StubTransport

_ID = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee"


def test_join_route_prefix():
    assert join_route_prefix(None, "Recipe/Recipes") == "/Recipe/Recipes"
    assert join_route_prefix("api", "Recipe/Recipes") == "/api/Recipe/Recipes"


def test_create_replace_and_query_routes(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"isSuccess": True, "items": []})
    recipes = RecipeApiClient(ApiClient("http://x", transport=stub_transport))
    recipes.recipes.create(RecipeReq(title="Soup", slug="soup"))
    recipes.replace_ingredient_lines(_ID, [RecipeIngredientLineReq(ingredient_text="salt", sort_order=0)])
    recipes.replace_steps(_ID, [RecipeStepReq(instruction="Mix", sort_order=0)])
    recipes.recipes.query_concrete(QueryConcreteReq(amount=10))
    recipes.categories.create(RecipeCategoryReq(name="Mains", slug="mains"))

    calls = stub_transport.requests
    assert calls[0].method == "POST"
    assert calls[0].url == "http://x/Recipe/Recipes"
    assert json.loads(calls[0].body) == {"title": "Soup", "slug": "soup"}
    assert calls[1].method == "PUT"
    assert calls[1].url == f"http://x/Recipe/Recipes/{_ID}/IngredientLines"
    assert json.loads(calls[1].body) == [{"ingredientText": "salt", "sortOrder": 0}]
    assert calls[2].method == "PUT"
    assert calls[2].url == f"http://x/Recipe/Recipes/{_ID}/Steps"
    assert calls[3].method == "POST"
    assert calls[3].url == "http://x/Recipe/Recipes/QueryConcrete"
    assert json.loads(calls[3].body)["Amount"] == 10
    assert calls[4].url == "http://x/Recipe/Categories"


def test_upload_posts_multipart(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data={"id": "att-1"})
    recipes = RecipeApiClient(ApiClient("http://x", transport=stub_transport))
    recipes.attachments.upload(
        RecipeUpload(entity_type="Recipe", entity_id=_ID, file=b"scan-bytes", file_name="scan.bin", role="Scan")
    )
    sent = stub_transport.last
    assert sent.method == "POST"
    assert sent.url == "http://x/Recipe/Attachments/upload"
    assert isinstance(sent.body, bytes)
    assert b"scan-bytes" in sent.body
    assert b"scan.bin" in sent.body
    assert b"Scan" in sent.body
    assert sent.headers["Content-Type"].startswith("multipart/form-data")


def test_notes_and_tags(stub_transport: StubTransport):
    stub_transport.response = ApiResponse(status=200, ok=True, data=[])
    recipes = RecipeApiClient(ApiClient("http://x", transport=stub_transport))
    recipes.get_notes("Recipe/Recipes", "r1")
    recipes.add_note("Recipe/Recipes", "r1", RecipeNoteCreateReq(content="hi"))
    recipes.delete_note("Recipe/Recipes", "r1", "n1")
    recipes.get_tags("Recipe/Recipes", "r1")
    recipes.add_tag("Recipe/Recipes", "r1", RecipeTagAddReq(name="spicy"))
    recipes.remove_tag("Recipe/Recipes", "r1", "hot pepper", tag_type="Diet")
    urls = [(request.method, request.url) for request in stub_transport.requests]
    assert urls[0] == ("GET", "http://x/Recipe/Recipes/r1/Notes")
    assert urls[1] == ("POST", "http://x/Recipe/Recipes/r1/Notes")
    assert json.loads(stub_transport.requests[1].body) == {"content": "hi"}
    assert urls[2] == ("DELETE", "http://x/Recipe/Recipes/r1/Notes/n1")
    assert urls[3] == ("GET", "http://x/Recipe/Recipes/r1/Tags")
    assert urls[4] == ("POST", "http://x/Recipe/Recipes/r1/Tags")
    assert urls[5] == ("DELETE", "http://x/Recipe/Recipes/r1/Tags/hot%20pepper?tagType=Diet")
