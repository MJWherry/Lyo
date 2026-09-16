# lyo-recipe-api-client

Recipe client: categories, ingredients, recipes, attachments, notes, and tags.

Depends on `lyo-api-client`, `lyo-person-api-client`. Ships `py.typed`.

## Install

Install dependencies from this repo first (they are separate packages), then this one:

```bash
cd packages/python/lyo-recipe-api-client
pip install -e ../lyo-api-client
pip install -e ../lyo-person-api-client
pip install -e ".[dev]"
```

## Tests

```bash
cd packages/python/lyo-recipe-api-client
pytest
```

Import the client as `lyo_recipe_api_client`.
