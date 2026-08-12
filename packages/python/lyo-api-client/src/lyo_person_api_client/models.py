"""Typed request contracts for the Person Query API.

Models keep snake_case attribute names and serialize (via ``to_dict()``) to
the exact wire shape the API expects: PascalCase keys, a lowercase
``whereClause`` key, and ``$type`` discriminators on where-clause nodes —
matching the TypeScript lyo-person-api-client package.

Optional fields default to UNSET and are omitted from the payload entirely;
pass None explicitly to serialize a JSON null (mirroring the undefined/null
distinction in the TypeScript client).
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Literal, Union


class _Unset:
    """Sentinel for 'field not provided' (omitted from the serialized payload)."""

    _instance: "_Unset | None" = None

    def __new__(cls) -> "_Unset":
        if cls._instance is None:
            cls._instance = super().__new__(cls)
        return cls._instance

    def __repr__(self) -> str:
        return "UNSET"

    def __bool__(self) -> bool:
        return False


UNSET = _Unset()

QueryTotalCountMode = Literal["None", "HasMore", "Exact"]
QueryIncludeFilterMode = Literal["Full", "MatchedOnly"]
SortDirection = Literal["Asc", "Desc"]
JoinType = Literal["Inner", "Left", "Right", "FullOuter"]
ComparisonOperator = Literal[
    "Equals",
    "NotEquals",
    "In",
    "NotIn",
    "Contains",
    "StartsWith",
    "EndsWith",
    "GreaterThan",
    "GreaterThanOrEqual",
    "LessThan",
    "LessThanOrEqual",
    "Regex",
]


def _to_wire(value: Any) -> Any:
    if hasattr(value, "to_dict"):
        return value.to_dict()
    if isinstance(value, list):
        return [_to_wire(v) for v in value]
    return value


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if isinstance(value, _Unset):
        return
    out[key] = _to_wire(value)


@dataclass
class SortBy:
    property_name: str
    direction: SortDirection
    priority: int | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"PropertyName": self.property_name, "Direction": self.direction}
        _put(out, "Priority", self.priority)
        return out


@dataclass
class QueryRequestOptions:
    total_count_mode: QueryTotalCountMode = "None"
    include_filter_mode: QueryIncludeFilterMode = "Full"
    zip_sibling_collection_selections: bool | None | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "TotalCountMode": self.total_count_mode,
            "IncludeFilterMode": self.include_filter_mode,
        }
        _put(out, "ZipSiblingCollectionSelections", self.zip_sibling_collection_selections)
        return out


@dataclass
class ConditionClause:
    field: str
    comparison: ComparisonOperator
    value: Any
    sub_clause: "WhereClause | _Unset" = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "$type": "condition",
            "Field": self.field,
            "Comparison": self.comparison,
            "Value": self.value,
        }
        _put(out, "subClause", self.sub_clause)
        return out


@dataclass
class GroupClause:
    operator: Literal["And", "Or"]
    children: list["WhereClause"]

    def to_dict(self) -> dict[str, Any]:
        return {
            "$type": "group",
            "Operator": self.operator,
            "Children": [child.to_dict() for child in self.children],
        }


WhereClause = Union[ConditionClause, GroupClause]


@dataclass
class ComputedField:
    name: str
    template: str

    def to_dict(self) -> dict[str, Any]:
        return {"Name": self.name, "Template": self.template}


@dataclass(kw_only=True)
class _QueryRequestBase:
    start: int | _Unset = UNSET
    amount: int | _Unset = UNSET
    keys: list[list[Any]] | _Unset = UNSET
    where_clause: WhereClause | None | _Unset = UNSET
    include: list[str] | _Unset = UNSET
    sort_by: list[SortBy] | _Unset = UNSET

    def _base_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "Start", self.start)
        _put(out, "Amount", self.amount)
        _put(out, "Keys", self.keys)
        _put(out, "whereClause", self.where_clause)
        _put(out, "Include", self.include)
        _put(out, "SortBy", self.sort_by)
        return out


@dataclass(kw_only=True)
class QueryConcreteReq(_QueryRequestBase):
    """POST /person/QueryConcrete — full entity results."""

    options: QueryRequestOptions = field(default_factory=QueryRequestOptions)

    def to_dict(self) -> dict[str, Any]:
        return {"Options": self.options.to_dict(), **self._base_dict()}


@dataclass(kw_only=True)
class ProjectionQueryReq(_QueryRequestBase):
    """POST /person/QueryProject — projected rows via Select/ComputedFields."""

    options: QueryRequestOptions = field(default_factory=QueryRequestOptions)
    select: list[str] = field(default_factory=list)
    computed_fields: list[ComputedField] | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"Options": self.options.to_dict(), **self._base_dict()}
        out["Select"] = list(self.select)
        _put(out, "ComputedFields", self.computed_fields)
        return out


@dataclass
class JoinOn:
    from_field: str
    to_field: str

    def to_dict(self) -> dict[str, Any]:
        return {"From": self.from_field, "To": self.to_field}


@dataclass(kw_only=True)
class SourceQueryScope:
    where_clause: WhereClause | None | _Unset = UNSET
    keys: list[list[Any]] | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {}
        _put(out, "whereClause", self.where_clause)
        _put(out, "Keys", self.keys)
        return out


@dataclass(kw_only=True)
class FromClause:
    alias: str
    entity_type: str
    query: SourceQueryScope | None | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"Alias": self.alias, "EntityType": self.entity_type}
        _put(out, "Query", self.query)
        return out


@dataclass(kw_only=True)
class JoinClause(FromClause):
    type: JoinType = "Inner"
    on: list[JoinOn] = field(default_factory=list)
    as_name: str | None | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out = super().to_dict()
        out["Type"] = self.type
        out["On"] = [j.to_dict() for j in self.on]
        _put(out, "As", self.as_name)
        return out


@dataclass(kw_only=True)
class QueryReq(_QueryRequestBase):
    """Root POST /Query — From/Joins + Select (projected rows)."""

    options: QueryRequestOptions = field(default_factory=QueryRequestOptions)
    from_clause: FromClause
    joins: list[JoinClause] | _Unset = UNSET
    select: list[str] = field(default_factory=list)
    computed_fields: list[ComputedField] | _Unset = UNSET

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {"Options": self.options.to_dict(), **self._base_dict()}
        out["From"] = self.from_clause.to_dict()
        _put(out, "Joins", self.joins)
        out["Select"] = list(self.select)
        _put(out, "ComputedFields", self.computed_fields)
        return out
