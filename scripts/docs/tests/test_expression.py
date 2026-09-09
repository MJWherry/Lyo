"""Expression runner: {{ }} gating, comparators, contains, string helpers."""

from __future__ import annotations

from lyo_tooling.expression import (
    MISSING,
    ExpressionRunner,
    eval_condition,
    eval_expr,
    render_template,
    scan_mustache,
)


def test_bare_comparison_is_not_a_condition():
    assert eval_condition("a > 0", {"a": 2}) is False
    assert eval_condition("{{a > 0}}", {"a": 2}) is True
    assert eval_condition("{{a > 0}}", {"a": 0}) is False


def test_comparators():
    ctx = {"a": 2, "b": 2, "region": "North", "tags": ["a", "b"]}
    assert eval_condition("{{a == 2}}", ctx)
    assert eval_condition("{{a != 3}}", ctx)
    assert eval_condition("{{a < 3}}", ctx)
    assert eval_condition("{{a <= 2}}", ctx)
    assert eval_condition("{{a > 1}}", ctx)
    assert eval_condition("{{a >= 2}}", ctx)
    assert eval_condition("{{region in [\"North\", \"South\"]}}", ctx)
    assert eval_condition("{{region not in [\"East\"]}}", ctx)
    assert eval_condition("{{1 < a < 3}}", ctx)
    assert eval_condition("{{1 < a < 2}}", ctx) is False


def test_contains_function_and_infix():
    ctx = {"item": {"name": "North Shore", "region": "North"}}
    assert eval_condition('{{contains(item.name, "North")}}', ctx)
    assert eval_condition('{{item.region contains "North"}}', ctx)
    assert eval_condition('{{item.region contains "South"}}', ctx) is False


def test_lower_upper_strip_none_and_missing():
    ctx = {"name": "  Hello ", "empty": None}
    runner = ExpressionRunner()
    assert runner.eval(ctx, "{{lower(name)}}") == "  hello "
    assert runner.eval(ctx, "{{upper(name)}}") == "  HELLO "
    assert runner.eval(ctx, "{{strip(name)}}") == "Hello"
    assert runner.eval(ctx, "{{name.lower()}}") == "  hello "
    assert runner.eval(ctx, "{{lower(empty)}}") == ""
    assert runner.eval(ctx, "{{upper(missing)}}") == ""
    assert runner.eval(ctx, "{{empty.lower()}}") == ""


def test_spaced_keys():
    ctx = {"Account Status": [{"name": "open"}]}
    runner = ExpressionRunner()
    assert runner.eval_path(ctx, "Account Status")[0]["name"] == "open"
    assert "open" in render_template("{{Account Status.0.name}}", ctx, escape=False)


def test_ternary_and_coalesce_inside_tokens():
    ctx = {"a": 1, "label": None}
    assert render_template('{{a > 0 ? "yes" : "no"}}', ctx, escape=False) == "yes"
    assert render_template("{{label ?? \"fallback\"}}", ctx, escape=False) == "fallback"
    html = render_template('{{a > 0 ? "<div class=ok>" : "<div>"}}', ctx, escape=False)
    assert html == "<div class=ok>"


def test_and_or_not_parens():
    ctx = {"a": 1, "b": 0}
    assert eval_condition("{{a > 0 and b == 0}}", ctx)
    assert eval_condition("{{a > 0 or b > 0}}", ctx)
    assert eval_condition("{{not b}}", ctx)
    assert eval_condition("{{(a > 0) and (not b)}}", ctx)


def test_aggregates():
    ctx = {"item": {"rows": [1, 2, 3]}}
    runner = ExpressionRunner()
    assert runner.eval(ctx, "{{count(item.rows)}}") == 3
    assert runner.eval(ctx, "{{item.rows.count()}}") == 3
    assert runner.eval(ctx, "{{sum(item.rows)}}") == 6
    assert runner.eval(ctx, "{{avg(item.rows)}}") == 2


def test_css_and_prose_share_scanner():
    ctx = {"table_id": "t1", "item": {"urgent": True, "region": "North"}}
    css = 'body{color:red}#{{table_id}}{color: {{item.urgent ? "red" : "#333"}}}'
    out = render_template(css, ctx, escape=False)
    assert "body{color:red}" in out
    assert "#t1{" in out
    assert "color: red" in out
    assert "#{{table_id}}" not in out
    prose = 'Region {{item.region in ["North"] ? "N" : "other"}}'
    assert render_template(prose, ctx, escape=False) == "Region N"


def test_quoted_closers_inside_token():
    ctx = {"flag": True}
    spans = scan_mustache('{{flag ? "}}" : "ok"}}')
    assert len(spans) == 1
    assert render_template('{{flag ? "}}" : "ok"}}', ctx, escape=False) == "}}"


def test_eval_expr_raw_for_tests():
    assert eval_expr({"a": 2}, "a > 0") is True
    assert eval_expr({"x": None}, "lower(x)") == ""
    assert eval_expr({"x": None}, "x") is None
    assert eval_expr({}, "missing") is MISSING
