from __future__ import annotations

from lyo_person_api_client import is_projected_query_res, is_query_res


def test_query_res_accepts_valid_payload():
    assert is_query_res({"isSuccess": True, "queryRequest": {}, "items": [{"Id": "1"}]})


def test_query_res_accepts_missing_or_null_items():
    assert is_query_res({"isSuccess": False, "queryRequest": {}})
    assert is_query_res({"isSuccess": True, "queryRequest": {}, "items": None})


def test_query_res_rejects_bad_shapes():
    assert not is_query_res(None)
    assert not is_query_res("nope")
    assert not is_query_res({"isSuccess": "yes", "queryRequest": {}})
    assert not is_query_res({"isSuccess": True})
    assert not is_query_res({"isSuccess": True, "queryRequest": {}, "items": "rows"})


def test_projected_query_res_requires_dict_rows():
    assert is_projected_query_res({"isSuccess": True, "queryRequest": {}, "items": [{"a": 1}, None]})
    assert not is_projected_query_res({"isSuccess": True, "queryRequest": {}, "items": [1, 2]})


def test_projected_query_res_accepts_missing_or_null_items():
    assert is_projected_query_res({"isSuccess": True, "queryRequest": {}})
    assert is_projected_query_res({"isSuccess": True, "queryRequest": {}, "items": None})
