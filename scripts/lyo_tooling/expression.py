#!/usr/bin/env python3
"""Evaluate expressions against a dict/object context.

Config/template text is inert unless wrapped in ``{{ ... }}``.
``a > 0`` is literal; ``{{a > 0}}`` is an expression.

Inside ``{{ }}``: Python-like eval (and/or/not, ``()``, ``in``, calls,
``item.rows.count()`` / ``count(item.rows)``), ``??`` coalesce, ``cond ? a : b``,
and ``:format`` specs. Context keys may contain spaces (``{{Account Status}}``).
"""

from __future__ import annotations

import ast
import logging
import operator
import re
from collections.abc import Mapping
from contextvars import ContextVar
from datetime import date, datetime
from typing import Any, Callable

logger = logging.getLogger(__name__)

_strict_mode: ContextVar[bool] = ContextVar("expression_strict", default=False)


class StrictModeError(ValueError):
    """Raised in strict mode for problems that are otherwise warnings."""


class _Missing:
    __slots__ = ()

    def __repr__(self) -> str:
        return "MISSING"

    def __bool__(self) -> bool:
        return False


MISSING = _Missing()

_NUMBER_RE = re.compile(r"^[+-]?(\d+\.\d*|\.\d+|\d+)$")
_IDENT_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
_AGGREGATES = frozenset({"sum", "avg", "min", "max", "count"})
_NAME_ALIASES = {"true": True, "false": False, "null": None, "none": None, "None": None}
_BINOPS: dict[type, Callable[[Any, Any], Any]] = {
    ast.Add: operator.add,
    ast.Sub: operator.sub,
    ast.Mult: operator.mul,
    ast.Div: operator.truediv,
    ast.FloorDiv: operator.floordiv,
    ast.Mod: operator.mod,
    ast.Pow: operator.pow,
    ast.BitAnd: operator.and_,
    ast.BitOr: operator.or_,
    ast.BitXor: operator.xor,
    ast.LShift: operator.lshift,
    ast.RShift: operator.rshift,
}


def in_strict_mode() -> bool:
    return _strict_mode.get()


def set_strict(value: bool):
    return _strict_mode.set(value)


def reset_strict(token: Any) -> None:
    _strict_mode.reset(token)


def fail_or_warn(message: str, *args: Any) -> None:
    if in_strict_mode():
        raise StrictModeError(message % args if args else message)
    logger.warning(message, *args)


def _escape_html(text: str, *, quote: bool = True) -> str:
    out = (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
    )
    if quote:
        out = out.replace('"', "&quot;")
    return out


def _is_word_boundary(text: str, start: int, end: int) -> bool:
    before_ok = start == 0 or not (text[start - 1].isalnum() or text[start - 1] == "_")
    after_ok = end >= len(text) or not (text[end].isalnum() or text[end] == "_")
    return before_ok and after_ok


def _find_top_level_keyword(text: str, keyword: str) -> int:
    quote: str | None = None
    depth = 0
    i = 0
    n = len(keyword)
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            i += 1
            continue
        if ch in "([{":
            depth += 1
            i += 1
            continue
        if ch in ")]}":
            depth = max(0, depth - 1)
            i += 1
            continue
        if depth == 0 and text.startswith(keyword, i) and _is_word_boundary(text, i, i + n):
            return i
        i += 1
    return -1


def _truthy(value: Any) -> bool:
    return value is not MISSING and bool(value)


def _is_mapping(value: Any) -> bool:
    return isinstance(value, Mapping) and not isinstance(value, (str, bytes))


def scan_mustache(text: str) -> list[tuple[int, int, str]]:
    """Find ``{{ ... }}`` spans with quote / ``()`` / ``[]`` / ``{}`` depth.

    CSS rule braces stay single (``body{margin:0}``). Interpolate with ``#{{table_id}}``.
    """

    spans: list[tuple[int, int, str]] = []
    i = 0
    n = len(text)
    while i < n:
        start = text.find("{{", i)
        if start == -1:
            break
        j = start + 2
        quote: str | None = None
        depth_paren = 0
        depth_brack = 0
        depth_brace = 0
        closed = False
        while j < n:
            ch = text[j]
            if quote:
                if ch == quote:
                    quote = None
                j += 1
                continue
            if ch in "\"'":
                quote = ch
                j += 1
                continue
            if ch == "(":
                depth_paren += 1
            elif ch == ")":
                depth_paren = max(0, depth_paren - 1)
            elif ch == "[":
                depth_brack += 1
            elif ch == "]":
                depth_brack = max(0, depth_brack - 1)
            elif ch == "{":
                depth_brace += 1
            elif ch == "}":
                if (
                    depth_paren == 0
                    and depth_brack == 0
                    and depth_brace == 0
                    and j + 1 < n
                    and text[j + 1] == "}"
                ):
                    spans.append((start, j + 2, text[start + 2 : j]))
                    i = j + 2
                    closed = True
                    break
                if depth_brace > 0:
                    depth_brace -= 1
            j += 1
        if not closed:
            break
    return spans


def extract_token(text: str) -> str | None:
    """Return the inner expression if ``text`` is a single ``{{ ... }}`` token."""

    stripped = text.strip()
    spans = scan_mustache(stripped)
    if len(spans) == 1 and spans[0][0] == 0 and spans[0][1] == len(stripped):
        return spans[0][2].strip()
    return None


def _split_top_level(text: str, sep: str) -> list[str]:
    parts: list[str] = []
    buf: list[str] = []
    quote: str | None = None
    depth = 0
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            buf.append(ch)
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            buf.append(ch)
            i += 1
            continue
        if ch in "([{":
            depth += 1
            buf.append(ch)
            i += 1
            continue
        if ch in ")]}":
            depth = max(0, depth - 1)
            buf.append(ch)
            i += 1
            continue
        if depth == 0 and text.startswith(sep, i):
            parts.append("".join(buf))
            buf = []
            i += len(sep)
            continue
        buf.append(ch)
        i += 1
    parts.append("".join(buf))
    return parts


def _find_ternary_question(text: str) -> int:
    quote: str | None = None
    depth = 0
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            i += 1
            continue
        if ch in "([{":
            depth += 1
            i += 1
            continue
        if ch in ")]}":
            depth = max(0, depth - 1)
            i += 1
            continue
        if depth == 0 and ch == "?":
            if i + 1 < len(text) and text[i + 1] == "?":
                i += 2
                continue
            return i
        i += 1
    return -1


def _find_ternary_colon(text: str) -> int:
    quote: str | None = None
    depth = 0
    ternary_depth = 0
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            i += 1
            continue
        if ch in "([{":
            depth += 1
            i += 1
            continue
        if ch in ")]}":
            depth = max(0, depth - 1)
            i += 1
            continue
        if depth == 0:
            if ch == "?":
                if i + 1 < len(text) and text[i + 1] == "?":
                    i += 2
                    continue
                ternary_depth += 1
                i += 1
                continue
            if ch == ":":
                if ternary_depth == 0:
                    return i
                ternary_depth -= 1
        i += 1
    return -1


def _find_format_colon(text: str) -> int:
    quote: str | None = None
    depth = 0
    ternary_depth = 0
    i = 0
    while i < len(text):
        ch = text[i]
        if quote:
            if ch == quote:
                quote = None
            i += 1
            continue
        if ch in "\"'":
            quote = ch
            i += 1
            continue
        if ch in "([{":
            depth += 1
            i += 1
            continue
        if ch in ")]}":
            depth = max(0, depth - 1)
            i += 1
            continue
        if depth == 0:
            if ch == "?":
                if i + 1 < len(text) and text[i + 1] == "?":
                    i += 2
                    continue
                ternary_depth += 1
                i += 1
                continue
            if ch == ":":
                if ternary_depth > 0:
                    ternary_depth -= 1
                    i += 1
                    continue
                return i
        i += 1
    return -1


def _unwrap_parens(expr: str) -> str:
    expr = expr.strip()
    while expr.startswith("(") and expr.endswith(")"):
        depth = 0
        quote: str | None = None
        wraps = True
        for i, ch in enumerate(expr):
            if quote:
                if ch == quote:
                    quote = None
                continue
            if ch in "\"'":
                quote = ch
                continue
            if ch == "(":
                depth += 1
            elif ch == ")":
                depth -= 1
                if depth == 0 and i != len(expr) - 1:
                    wraps = False
                    break
        if not wraps or depth != 0:
            break
        expr = expr[1:-1].strip()
    return expr


def _parse_call(expr: str) -> tuple[str, str] | None:
    expr = expr.strip()
    open_paren = expr.find("(")
    if open_paren <= 0 or not expr.endswith(")"):
        return None
    name = expr[:open_paren].strip()
    if not _IDENT_RE.match(name):
        return None
    depth = 0
    quote: str | None = None
    for i in range(open_paren, len(expr)):
        ch = expr[i]
        if quote:
            if ch == quote:
                quote = None
            continue
        if ch in "\"'":
            quote = ch
            continue
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
            if depth == 0:
                if i != len(expr) - 1:
                    return None
                return name, expr[open_paren + 1 : i]
    return None


def resolve_path(path: str, context: Any) -> Any:
    """Walk ``a.b.c`` where any segment may be a dict key with spaces."""

    current: Any = context
    for segment in path.split("."):
        current = _attr(current, segment)
        if current is MISSING:
            return MISSING
    return current


def _mapping_lookup(context: Any, key: str) -> Any:
    if _is_mapping(context) and key in context:
        return context[key]
    return MISSING


def _attr(obj: Any, name: str) -> Any:
    if obj is MISSING or obj is None:
        return MISSING
    if name.startswith("_"):
        fail_or_warn("Access to %r is not allowed", name)
        return MISSING
    if _is_mapping(obj) and name in obj:
        return obj[name]
    if isinstance(obj, (list, tuple)) and name.lstrip("-").isdigit():
        idx = int(name)
        if -len(obj) <= idx < len(obj):
            return obj[idx]
        return MISSING
    if hasattr(obj, name):
        return getattr(obj, name)
    return MISSING


def _is_seq(value: Any) -> bool:
    return isinstance(value, (list, tuple))


def _looks_like_records(items: list | tuple) -> bool:
    for element in items:
        if element is None:
            continue
        if isinstance(element, dict):
            return True
        if isinstance(element, (str, bytes, int, float, bool, list, tuple)):
            return False
        return True
    return False


def _to_number(value: Any) -> float | None:
    if value is MISSING or value is None or isinstance(value, bool):
        return None
    if isinstance(value, (int, float)):
        return float(value)
    if isinstance(value, str) and _NUMBER_RE.match(value.strip()):
        return float(value)
    return None


def _field_values(items: Any, field: str | None) -> list[Any]:
    if not _is_seq(items):
        return []
    if field is None:
        return list(items)
    return [resolve_path(field, element) for element in items]


def _aggregate(name: str, items: Any, field: str | None) -> Any:
    if name == "count":
        if field is None:
            if items is MISSING or items is None:
                return 0
            if _is_seq(items):
                return len(items)
            try:
                return len(items)
            except TypeError:
                return 0
        values = _field_values(items, field)
        return sum(1 for v in values if v is not MISSING and v is not None)

    values = _field_values(items, field)
    nums = [n for n in (_to_number(v) for v in values) if n is not None]
    if name == "sum":
        return sum(nums) if nums else 0
    if not nums:
        return MISSING
    if name == "avg":
        return sum(nums) / len(nums)
    if name == "min":
        return min(nums)
    if name == "max":
        return max(nums)
    return MISSING


def _fn_aggregate(name: str):
    def _call(items: Any = None, field: Any = None, *rest: Any) -> Any:
        if rest:
            fail_or_warn("Too many arguments for %s()", name)
        field_name: str | None = None
        if field is not None and field is not MISSING:
            field_name = str(field)
        return _aggregate(name, items, field_name)

    _call.__name__ = name
    return _call


def _fn_coalesce(*values: Any) -> Any:
    for value in values:
        if value is not MISSING and value is not None:
            return value
    return MISSING


def _fn_len(value: Any) -> int:
    if value is MISSING or value is None:
        return 0
    try:
        return len(value)
    except TypeError:
        return 0


def _as_str(value: Any) -> str | None:
    if value is MISSING or value is None:
        return None
    return str(value)


def _fn_lower(value: Any = None) -> str:
    text = _as_str(value)
    return "" if text is None else text.lower()


def _fn_upper(value: Any = None) -> str:
    text = _as_str(value)
    return "" if text is None else text.upper()


def _fn_strip(value: Any = None) -> str:
    text = _as_str(value)
    return "" if text is None else text.strip()


def _fn_contains(haystack: Any = None, needle: Any = None) -> bool:
    left, right = _as_str(haystack), _as_str(needle)
    return bool(left is not None and right is not None and right in left)


_BUILTINS: dict[str, Any] = {
    "sum": _fn_aggregate("sum"),
    "avg": _fn_aggregate("avg"),
    "min": _fn_aggregate("min"),
    "max": _fn_aggregate("max"),
    "count": _fn_aggregate("count"),
    "len": _fn_len,
    "abs": abs,
    "round": round,
    "str": str,
    "int": int,
    "float": float,
    "bool": bool,
    "list": list,
    "tuple": tuple,
    "dict": dict,
    "set": set,
    "sorted": sorted,
    "any": any,
    "all": all,
    "coalesce": _fn_coalesce,
    "lower": _fn_lower,
    "upper": _fn_upper,
    "strip": _fn_strip,
    "contains": _fn_contains,
}


def _lookup_name(name: str, context: Any) -> Any:
    if name in _NAME_ALIASES:
        return _NAME_ALIASES[name]
    found = _mapping_lookup(context, name)
    if found is not MISSING:
        return found
    if name in _BUILTINS:
        return _BUILTINS[name]
    if hasattr(context, name) and not name.startswith("_"):
        return getattr(context, name)
    return MISSING


def _call_method(obj: Any, name: str, args: list[Any], kwargs: dict[str, Any]) -> Any:
    if name.startswith("_"):
        fail_or_warn("Call to %r is not allowed", name)
        return MISSING
    if obj is MISSING or obj is None:
        if name in ("sum", "count"):
            return 0
        if name in ("lower", "upper", "strip"):
            return ""
        return MISSING

    if name in _AGGREGATES and _is_seq(obj):
        if name == "count" and args and not _looks_like_records(obj):
            try:
                return list(obj).count(*args, **kwargs)
            except TypeError:
                fail_or_warn("count() failed on sequence")
                return MISSING
        field = None
        if args:
            field = None if args[0] is MISSING or args[0] is None else str(args[0])
        elif "field" in kwargs:
            field = str(kwargs["field"])
        return _aggregate(name, obj, field)

    meth = _attr(obj, name)
    if meth is MISSING or not callable(meth):
        fail_or_warn("Unknown method %r", name)
        return MISSING
    try:
        return meth(*args, **kwargs)
    except TypeError as exc:
        fail_or_warn("Call to %r failed: %s", name, exc)
        return MISSING


def _coerce_comparable(value: Any) -> Any:
    if isinstance(value, (datetime, date, int, float, bool)):
        return value
    if isinstance(value, str) and _NUMBER_RE.match(value.strip()):
        return float(value)
    return value


def _norm(value: Any) -> Any:
    return None if value is MISSING else value


class _SafeEval(ast.NodeVisitor):
    def __init__(self, context: Any) -> None:
        self.context = context

    def visit_Constant(self, node: ast.Constant) -> Any:
        return node.value

    def visit_Name(self, node: ast.Name) -> Any:
        return _lookup_name(node.id, self.context)

    def visit_Attribute(self, node: ast.Attribute) -> Any:
        return _attr(self.visit(node.value), node.attr)

    def visit_Subscript(self, node: ast.Subscript) -> Any:
        obj = self.visit(node.value)
        key = self.visit(node.slice)
        if obj is MISSING or obj is None or key is MISSING:
            return MISSING
        try:
            return obj[key]
        except (KeyError, IndexError, TypeError):
            return MISSING

    def visit_Slice(self, node: ast.Slice) -> slice:
        lower = self.visit(node.lower) if node.lower else None
        upper = self.visit(node.upper) if node.upper else None
        step = self.visit(node.step) if node.step else None
        return slice(lower, upper, step)

    def visit_UnaryOp(self, node: ast.UnaryOp) -> Any:
        operand = self.visit(node.operand)
        if isinstance(node.op, ast.Not):
            return not _truthy(operand)
        if operand is MISSING or operand is None:
            return MISSING
        try:
            if isinstance(node.op, ast.USub):
                return -operand
            if isinstance(node.op, ast.UAdd):
                return +operand
        except TypeError:
            return MISSING
        fail_or_warn("Unsupported unary operator")
        return MISSING

    def visit_BinOp(self, node: ast.BinOp) -> Any:
        left, right = self.visit(node.left), self.visit(node.right)
        if left is MISSING or right is MISSING:
            return MISSING
        fn = _BINOPS.get(type(node.op))
        if fn is None:
            fail_or_warn("Unsupported operator %s", type(node.op).__name__)
            return MISSING
        try:
            return fn(left, right)
        except (TypeError, ZeroDivisionError, ValueError):
            return MISSING

    def visit_BoolOp(self, node: ast.BoolOp) -> Any:
        if isinstance(node.op, ast.And):
            value: Any = True
            for child in node.values:
                value = self.visit(child)
                if not _truthy(value):
                    return False if value is MISSING else value
            return value
        value = False
        for child in node.values:
            value = self.visit(child)
            if _truthy(value):
                return value
        return False if value is MISSING else value

    def visit_Compare(self, node: ast.Compare) -> Any:
        left = self.visit(node.left)
        for op, comparator in zip(node.ops, node.comparators):
            right = self.visit(comparator)
            if not self._compare(op, left, right):
                return False
            left = right
        return True

    def _compare(self, op: ast.cmpop, lhs: Any, rhs: Any) -> bool:
        if isinstance(op, ast.In):
            if rhs is MISSING or rhs is None:
                return False
            try:
                return lhs in rhs
            except TypeError:
                return False
        if isinstance(op, ast.NotIn):
            if rhs is MISSING or rhs is None:
                return True
            try:
                return lhs not in rhs
            except TypeError:
                return True
        left, right = _norm(lhs), _norm(rhs)
        if isinstance(op, ast.Eq):
            return left == right
        if isinstance(op, ast.NotEq):
            return left != right
        if isinstance(op, ast.Is):
            return left is right
        if isinstance(op, ast.IsNot):
            return left is not right
        left_c, right_c = _coerce_comparable(left), _coerce_comparable(right)
        try:
            if isinstance(op, ast.Lt):
                return left_c < right_c
            if isinstance(op, ast.LtE):
                return left_c <= right_c
            if isinstance(op, ast.Gt):
                return left_c > right_c
            if isinstance(op, ast.GtE):
                return left_c >= right_c
        except TypeError:
            return False
        return False

    def visit_IfExp(self, node: ast.IfExp) -> Any:
        branch = node.body if _truthy(self.visit(node.test)) else node.orelse
        return self.visit(branch)

    def visit_List(self, node: ast.List) -> list[Any]:
        return [self.visit(elt) for elt in node.elts]

    def visit_Tuple(self, node: ast.Tuple) -> tuple[Any, ...]:
        return tuple(self.visit(elt) for elt in node.elts)

    def visit_Set(self, node: ast.Set) -> set[Any]:
        return {self.visit(elt) for elt in node.elts}

    def visit_Dict(self, node: ast.Dict) -> dict[Any, Any]:
        out: dict[Any, Any] = {}
        for key_node, val_node in zip(node.keys, node.values):
            if key_node is None:
                continue
            out[self.visit(key_node)] = self.visit(val_node)
        return out

    def visit_Call(self, node: ast.Call) -> Any:
        args = [self.visit(a) for a in node.args]
        kwargs = {kw.arg: self.visit(kw.value) for kw in node.keywords if kw.arg}
        if isinstance(node.func, ast.Attribute):
            obj = self.visit(node.func.value)
            return _call_method(obj, node.func.attr, args, kwargs)
        func = self.visit(node.func)
        if func is MISSING or not callable(func):
            fail_or_warn("Unknown function in expression")
            return MISSING
        try:
            return func(*args, **kwargs)
        except TypeError as exc:
            fail_or_warn("Function call failed: %s", exc)
            return MISSING

    def visit_JoinedStr(self, node: ast.JoinedStr) -> str:
        parts: list[str] = []
        for value in node.values:
            if isinstance(value, ast.FormattedValue):
                inner = self.visit(value.value)
                spec = ""
                if value.format_spec is not None:
                    spec = self.visit(value.format_spec)
                    spec = spec if isinstance(spec, str) else str(spec)
                parts.append(_format_value(inner, spec))
            else:
                visited = self.visit(value)
                parts.append("" if visited is MISSING or visited is None else str(visited))
        return "".join(parts)

    def generic_visit(self, node: ast.AST) -> Any:
        fail_or_warn("Unsupported expression node %s", type(node).__name__)
        return MISSING


def _eval_legacy_call(context: Any, expr: str) -> Any:
    parsed = _parse_call(expr)
    if not parsed:
        return MISSING
    name, args_src = parsed
    func = _BUILTINS.get(name)
    if func is None:
        fail_or_warn("Unknown function %r in expression", name)
        return MISSING
    args = [eval_expr(context, part.strip()) for part in _split_top_level(args_src, ",") if part.strip()]
    try:
        return func(*args)
    except TypeError as exc:
        fail_or_warn("Function call failed: %s", exc)
        return MISSING


def eval_expr(context: Any, expr: str, *, literal_on_syntax_error: bool = False) -> Any:
    """Evaluate an already-extracted expression (no ``{{ }}``, no ``??`` / ternary).

    Call this only after unwrapping a ``{{ ... }}`` token, or from tests that
    pass a raw expression on purpose.
    """

    expr = expr.strip()
    if not expr:
        return MISSING

    unwrapped = _unwrap_parens(expr)
    if unwrapped != expr:
        return eval_expr(context, unwrapped, literal_on_syntax_error=literal_on_syntax_error)

    found = _mapping_lookup(context, expr)
    if found is not MISSING:
        return found

    contains_at = _find_top_level_keyword(expr, "contains")
    if contains_at > 0:
        lhs = eval_expr(context, expr[:contains_at].strip(), literal_on_syntax_error=literal_on_syntax_error)
        rhs = eval_expr(context, expr[contains_at + 8 :].strip(), literal_on_syntax_error=literal_on_syntax_error)
        return _fn_contains(lhs, rhs)

    try:
        tree = ast.parse(expr, mode="eval")
    except SyntaxError:
        resolved = resolve_path(expr, context)
        if resolved is not MISSING:
            return resolved
        called = _eval_legacy_call(context, expr)
        if called is not MISSING:
            return called
        if literal_on_syntax_error:
            return expr
        fail_or_warn("Invalid expression %r", expr)
        return MISSING
    return _SafeEval(context).visit(tree.body)


def _eval_token(content: str, context: Any) -> tuple[Any, str]:
    """Evaluate the inside of one ``{{ ... }}`` token (ternary, ``??``, format, expr)."""

    content = content.strip()
    question = _find_ternary_question(content)
    if question != -1:
        condition = content[:question]
        remainder = content[question + 1 :]
        colon = _find_ternary_colon(remainder)
        when_true, when_false = (remainder, "") if colon == -1 else (remainder[:colon], remainder[colon + 1 :])
        branch = when_true if _truthy(eval_expr(context, condition.strip())) else when_false
        return _eval_token(branch.strip(), context)

    colon = _find_format_colon(content)
    if colon == -1:
        expr, spec = content, ""
    else:
        expr, spec = content[:colon], content[colon + 1 :]

    parts = _split_top_level(expr, "??")
    if len(parts) > 1:
        value: Any = MISSING
        for part in parts:
            value = eval_expr(context, part.strip(), literal_on_syntax_error=True)
            if value is not MISSING and value is not None:
                break
        return value, _format_value(value, spec.strip())

    value = eval_expr(context, expr.strip(), literal_on_syntax_error=True)
    return value, _format_value(value, spec.strip())


def eval_path(path: str, context: Any) -> Any:
    """Resolve a config path.

    ``{{ expr }}`` runs the expression. Anything else is a key/path lookup
    only (``Account Status``, ``report.items``) — not Python.
    """

    path = path.strip()
    if not path:
        return MISSING
    inner = extract_token(path)
    if inner is not None:
        value, _ = _eval_token(inner, context)
        return value
    found = _mapping_lookup(context, path)
    if found is not MISSING:
        return found
    return resolve_path(path, context)


def eval_condition(when: str | None, context: Any) -> bool:
    """True when ``when`` is a ``{{ ... }}`` expression that evaluates truthy.

    ``a > 0`` is not an expression. ``{{a > 0}}`` is.
    """

    if not when:
        return False
    inner = extract_token(when)
    if inner is None:
        return False
    value, _ = _eval_token(inner, context)
    return _truthy(value)


def _format_value(value: Any, spec: str) -> str:
    if value is MISSING or value is None:
        return ""
    if not spec:
        return str(value)
    try:
        return format(value, spec)
    except (ValueError, TypeError):
        return str(value)


def render_template(template: str | None, context: Any, *, escape: bool = True) -> str:
    """Substitute ``{{ ... }}`` tokens. Text outside tokens is copied as-is."""

    if not template:
        return ""
    spans = scan_mustache(template)
    if not spans:
        return _escape_html(template, quote=True) if escape else template

    parts: list[str] = []
    last = 0
    for start, end, inner in spans:
        parts.append(template[last:start])
        _, text = _eval_token(inner.strip(), context)
        parts.append(_escape_html(text, quote=True) if escape else text)
        last = end
    parts.append(template[last:])
    return "".join(parts)


class ExpressionRunner:
    """Evaluate expressions and templates against a context mapping/object."""

    def __init__(self, logger: logging.Logger | None = None) -> None:
        self.logger = logger or globals()["logger"]

    def eval(self, context: Any, expr: str) -> Any:
        """If ``expr`` is ``{{ ... }}``, run that token; otherwise eval as an expression."""

        inner = extract_token(expr)
        if inner is not None:
            value, _ = _eval_token(inner, context)
            return value
        return eval_expr(context, expr)

    def eval_condition(self, context: Any, expr: str | None) -> bool:
        return eval_condition(expr, context)

    def eval_path(self, context: Any, path: str) -> Any:
        return eval_path(path, context)

    def render(self, context: Any, template: str | None, *, escape: bool = True) -> str:
        return render_template(template, context, escape=escape)


__all__ = [
    "MISSING",
    "StrictModeError",
    "ExpressionRunner",
    "eval_expr",
    "eval_path",
    "eval_condition",
    "extract_token",
    "scan_mustache",
    "render_template",
    "resolve_path",
    "fail_or_warn",
    "set_strict",
    "reset_strict",
    "in_strict_mode",
]
