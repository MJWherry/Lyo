"""Query builders for the Person API, ported from the TypeScript package.

Each builder returns a request model whose ``to_dict()`` output is identical
to the JSON payload produced by the corresponding TypeScript builder.
"""

from __future__ import annotations

from .models import (
    UNSET,
    ComputedField,
    ConditionClause,
    GroupClause,
    ProjectionQueryReq,
    QueryConcreteReq,
    QueryRequestOptions,
    SortBy,
    _Unset,
)

ENDATO_PS_PERSON_ENTITY_TYPE = "Lyo.Endato.Postgres.Database.EndatoPsPersonEntity"

ENDATO_CE_PERSON_ENTITY_TYPE = "Lyo.Endato.Postgres.Database.EndatoCePersonEntity"

PERSON_SOURCE_ENTITY_TYPES = (
    ENDATO_PS_PERSON_ENTITY_TYPE,
    ENDATO_CE_PERSON_ENTITY_TYPE,
)

DEFAULT_SOURCE_FILTER_VALUES = ",".join(PERSON_SOURCE_ENTITY_TYPES)

DEFAULT_PERSON_INCLUDES = [
    "contactphonenumbers.phonenumber",
    "contactemailaddresses.emailaddress",
    "contactaddresses.address",
]

DEFAULT_PERSON_SELECT_FIELDS = [
    "Id",
    "FirstName",
    "LastName",
    "SourceEntityType",
    "contactaddresses.address.city",
]

QUERY_FIELD_SOURCE = "SourceEntityType"

_DEFAULT_NAME_SORT = [
    SortBy("LastName", "Asc", priority=0),
    SortBy("FirstName", "Asc", priority=1),
]


def build_options(
    *,
    total_count_mode: str = "None",
    include_filter_mode: str = "Full",
    zip_sibling_collection_selections: bool | None | _Unset = UNSET,
) -> QueryRequestOptions:
    return QueryRequestOptions(
        total_count_mode=total_count_mode,  # type: ignore[arg-type]
        include_filter_mode=include_filter_mode,  # type: ignore[arg-type]
        zip_sibling_collection_selections=zip_sibling_collection_selections,
    )


def baseline_query(*, start: int = 0, amount: int = 1000) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        include=[],
        sort_by=[],
    )


def filter_sort_query(
    *,
    start: int = 0,
    amount: int = 1000,
    source_filter_values: str = DEFAULT_SOURCE_FILTER_VALUES,
) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        where_clause=GroupClause(
            "Or",
            [
                GroupClause(
                    "And",
                    [
                        ConditionClause("FirstName", "NotEquals", None),
                        ConditionClause("LastName", "NotEquals", None),
                    ],
                ),
                ConditionClause(QUERY_FIELD_SOURCE, "In", source_filter_values),
            ],
        ),
        sort_by=[
            SortBy("LastName", "Asc", priority=0),
            SortBy("FirstName", "Asc", priority=1),
            SortBy("Id", "Desc", priority=2),
        ],
    )


def complex_where_clause(
    *,
    include: list[str] | None = None,
    start: int = 0,
    amount: int = 1200,
) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        include=include if include is not None else [],
        sort_by=list(_DEFAULT_NAME_SORT),
        where_clause=GroupClause(
            "And",
            [
                ConditionClause("FirstName", "NotEquals", None),
                GroupClause(
                    "Or",
                    [
                        ConditionClause("LastName", "NotEquals", None),
                        ConditionClause(QUERY_FIELD_SOURCE, "In", DEFAULT_SOURCE_FILTER_VALUES),
                    ],
                ),
            ],
        ),
    )


def two_phase_sub_query(
    *,
    include: list[str] | None = None,
    start: int = 0,
    amount: int = 1000,
) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        include=include if include is not None else [],
        where_clause=ConditionClause(
            "IsActive",
            "Equals",
            True,
            sub_clause=GroupClause(
                "And",
                [
                    ConditionClause("FirstName", "NotEquals", None),
                    GroupClause(
                        "Or",
                        [
                            ConditionClause(QUERY_FIELD_SOURCE, "NotEquals", None),
                            ConditionClause("LastName", "Regex", "^[A-Z][a-z]+$"),
                        ],
                    ),
                ],
            ),
        ),
        sort_by=[SortBy("Id", "Asc", priority=0)],
    )


def heavy_include_query(
    *,
    include: list[str] | None = None,
    start: int = 0,
    amount: int = 1998,
) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        include=include if include is not None else list(DEFAULT_PERSON_INCLUDES),
        sort_by=[],
    )


def realistic_include_query(*, start: int = 0, amount: int = 200) -> QueryConcreteReq:
    return QueryConcreteReq(
        options=build_options(),
        start=start,
        amount=amount,
        include=["contactaddresses.address"],
        sort_by=[],
    )


def select_projection_query(
    *,
    start: int = 0,
    amount: int = 1200,
    include: list[str] | None = None,
    fields: list[str] | None = None,
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=include if include is not None else [],
        select=fields if fields is not None else list(DEFAULT_PERSON_SELECT_FIELDS),
        computed_fields=[],
        sort_by=list(_DEFAULT_NAME_SORT),
    )


def projection_root_scalars_query(
    *,
    start: int = 0,
    amount: int = 200,
    fields: list[str] | None = None,
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=[],
        select=fields if fields is not None else ["Id", "FirstName", "LastName", "SourceEntityType", "IsActive"],
        computed_fields=[],
        sort_by=list(_DEFAULT_NAME_SORT),
    )


def projection_nested_select_query(
    *,
    start: int = 0,
    amount: int = 200,
    fields: list[str] | None = None,
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=[],
        select=fields
        if fields is not None
        else ["Id", "contactaddresses.address.city", "contactaddresses.address.postalcode"],
        computed_fields=[],
        sort_by=list(_DEFAULT_NAME_SORT),
    )


def projection_unified_collection_query(
    *,
    start: int = 0,
    amount: int = 200,
    fields: list[str] | None = None,
    zip_sibling_collection_selections: bool | None = True,
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(zip_sibling_collection_selections=zip_sibling_collection_selections),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=[],
        select=fields
        if fields is not None
        else [
            "contactaddresses.id",
            "contactaddresses.address.streettype",
            "contactaddresses.address.streetname",
        ],
        computed_fields=[],
        sort_by=list(_DEFAULT_NAME_SORT),
    )


def computed_collection_parallel_query(
    *,
    start: int = 0,
    amount: int = 200,
    name: str = "streetLine",
    template: str = "{contactaddresses.address.streettype} {contactaddresses.address.streetname}",
    zip_sibling_collection_selections: bool | None = True,
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(zip_sibling_collection_selections=zip_sibling_collection_selections),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=[],
        select=["contactaddresses.id"],
        computed_fields=[ComputedField(name, template)],
        sort_by=list(_DEFAULT_NAME_SORT),
    )


def computed_scalar_template_query(
    *,
    start: int = 0,
    amount: int = 200,
    name: str = "fullName",
    template: str = "{FirstName} {LastName}",
) -> ProjectionQueryReq:
    return ProjectionQueryReq(
        options=build_options(),
        start=start,
        amount=amount,
        keys=[],
        where_clause=None,
        include=[],
        select=["FirstName", "LastName"],
        computed_fields=[ComputedField(name, template)],
        sort_by=list(_DEFAULT_NAME_SORT),
    )
