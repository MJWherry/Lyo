"""Builder payloads must match the TypeScript builders' JSON output exactly."""

from __future__ import annotations

from lyo_person_api_client import (
    DEFAULT_SOURCE_FILTER_VALUES,
    baseline_query,
    computed_scalar_template_query,
    filter_sort_query,
    heavy_include_query,
    projection_unified_collection_query,
    select_projection_query,
    two_phase_sub_query,
)

DEFAULT_OPTIONS = {"TotalCountMode": "None", "IncludeFilterMode": "Full"}


def test_baseline_query():
    assert baseline_query().to_dict() == {
        "Options": DEFAULT_OPTIONS,
        "Start": 0,
        "Amount": 1000,
        "Include": [],
        "SortBy": [],
    }


def test_filter_sort_query():
    assert filter_sort_query().to_dict() == {
        "Options": DEFAULT_OPTIONS,
        "Start": 0,
        "Amount": 1000,
        "whereClause": {
            "$type": "group",
            "Operator": "Or",
            "Children": [
                {
                    "$type": "group",
                    "Operator": "And",
                    "Children": [
                        {"$type": "condition", "Field": "FirstName", "Comparison": "NotEquals", "Value": None},
                        {"$type": "condition", "Field": "LastName", "Comparison": "NotEquals", "Value": None},
                    ],
                },
                {
                    "$type": "condition",
                    "Field": "SourceEntityType",
                    "Comparison": "In",
                    "Value": DEFAULT_SOURCE_FILTER_VALUES,
                },
            ],
        },
        "SortBy": [
            {"PropertyName": "LastName", "Direction": "Asc", "Priority": 0},
            {"PropertyName": "FirstName", "Direction": "Asc", "Priority": 1},
            {"PropertyName": "Id", "Direction": "Desc", "Priority": 2},
        ],
    }


def test_two_phase_sub_query_nests_sub_clause():
    payload = two_phase_sub_query().to_dict()
    assert payload["whereClause"]["$type"] == "condition"
    assert payload["whereClause"]["Field"] == "IsActive"
    assert payload["whereClause"]["Value"] is True
    sub = payload["whereClause"]["subClause"]
    assert sub["$type"] == "group" and sub["Operator"] == "And"
    assert sub["Children"][1]["Children"][1] == {
        "$type": "condition",
        "Field": "LastName",
        "Comparison": "Regex",
        "Value": "^[A-Z][a-z]+$",
    }
    assert payload["SortBy"] == [{"PropertyName": "Id", "Direction": "Asc", "Priority": 0}]


def test_heavy_include_query_defaults():
    payload = heavy_include_query().to_dict()
    assert payload["Amount"] == 1998
    assert payload["Include"] == [
        "contactphonenumbers.phonenumber",
        "contactemailaddresses.emailaddress",
        "contactaddresses.address",
    ]


def test_select_projection_query():
    assert select_projection_query().to_dict() == {
        "Options": DEFAULT_OPTIONS,
        "Start": 0,
        "Amount": 1200,
        "Keys": [],
        "whereClause": None,
        "Include": [],
        "Select": ["Id", "FirstName", "LastName", "SourceEntityType", "contactaddresses.address.city"],
        "ComputedFields": [],
        "SortBy": [
            {"PropertyName": "LastName", "Direction": "Asc", "Priority": 0},
            {"PropertyName": "FirstName", "Direction": "Asc", "Priority": 1},
        ],
    }


def test_projection_unified_collection_query_includes_zip_option():
    payload = projection_unified_collection_query().to_dict()
    assert payload["Options"] == {
        "TotalCountMode": "None",
        "IncludeFilterMode": "Full",
        "ZipSiblingCollectionSelections": True,
    }
    assert payload["Select"] == [
        "contactaddresses.id",
        "contactaddresses.address.streettype",
        "contactaddresses.address.streetname",
    ]


def test_computed_scalar_template_query():
    payload = computed_scalar_template_query().to_dict()
    assert payload["ComputedFields"] == [{"Name": "fullName", "Template": "{FirstName} {LastName}"}]
    assert payload["Select"] == ["FirstName", "LastName"]


def test_unset_fields_are_omitted_but_explicit_none_is_null():
    baseline = baseline_query().to_dict()
    assert "whereClause" not in baseline
    assert "Keys" not in baseline

    projected = select_projection_query().to_dict()
    assert projected["whereClause"] is None
