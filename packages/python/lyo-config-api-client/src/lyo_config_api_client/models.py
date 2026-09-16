"""Config records. Clients send plaintext. The API encrypts when the definition says so."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Literal


def _put(out: dict[str, Any], key: str, value: Any) -> None:
    if value is None:
        return
    out[key] = value.to_dict() if hasattr(value, "to_dict") else value


@dataclass
class ConfigValue:
    """Typed JSON payload."""

    type_name: str
    json: str

    def to_dict(self) -> dict[str, str]:
        return {"typeName": self.type_name, "json": self.json}


@dataclass
class ConfigDefinitionRecord:
    """Allowed key for an entity type."""

    subject_entity_type: str
    key: str
    for_value_type: str
    id: str | None = None
    description: str | None = None
    is_required: bool | None = None
    default_value: ConfigValue | None = None
    is_encrypted: bool | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "subjectEntityType": self.subject_entity_type,
            "key": self.key,
            "forValueType": self.for_value_type,
        }
        _put(out, "id", self.id)
        _put(out, "description", self.description)
        if self.is_required is not None:
            out["isRequired"] = self.is_required
        _put(out, "defaultValue", self.default_value)
        if self.is_encrypted is not None:
            out["isEncrypted"] = self.is_encrypted
        return out


@dataclass
class ConfigBindingRecord:
    """Value bound to one entity instance. Plaintext in ``value.json``."""

    definition_id: str
    key: str
    subject_entity_type: str
    subject_entity_id: str
    value: ConfigValue
    id: str | None = None

    def to_dict(self) -> dict[str, Any]:
        out: dict[str, Any] = {
            "definitionId": self.definition_id,
            "key": self.key,
            "subjectEntityType": self.subject_entity_type,
            "subjectEntityId": self.subject_entity_id,
            "value": self.value.to_dict(),
        }
        _put(out, "id", self.id)
        return out


@dataclass
class ConfigResolveResult:
    """``304`` means the caller's etag still matches. There is no record."""

    outcome: Literal[200, 304]
    record: Any = None
    etag: str | None = None
