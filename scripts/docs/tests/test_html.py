"""Document tooling: styles, tables, grouping, blocks, builder, dual emit."""

from __future__ import annotations

from builder import HtmlBuilder
from document import resolve
from html import build, build_table


SALES = {
    "quarter": "Q1",
    "urgent": True,
    "items": [
        {"region": "West", "name": "W1", "amount": 10, "url": "/w1"},
        {"region": "North", "name": "N2", "amount": -2, "url": "/n2"},
        {"region": "North", "name": "N1", "amount": 3, "url": "/n1"},
        {"region": "East", "name": "E1", "amount": 4, "url": "/e1"},
        {"region": "West", "name": "W2", "amount": 5, "url": "/w2"},
        {"region": "North", "name": "N3", "amount": 6, "url": "/n3"},
    ],
}


def test_style_kebab_extends_and_rules():
    config = {
        "id": "t1",
        "styles": {
            "cell": {"border_bottom": "1px solid #ccc", "backgroundColor": "#fff"},
            "cell_red": {"extends": "cell", "color": "#dc2626"},
        },
        "base_css": 'body{color:red}#{{table_id}}{color: {{urgent ? "red" : "#333"}}}',
        "headers": [{"cells": [{"value": "Amt", "style_name": "cell"}]}],
        "rows": [
            {
                "repeat_for": "items",
                "filter_when": "{{item.amount > 0}}",
                "cells": [
                    {
                        "value": "{{item.amount}}",
                        "style_name": "cell",
                        "style_rules": [{"when": "{{item.amount > 0}}", "style_name": "cell_red"}],
                    }
                ],
            }
        ],
    }
    html = build_table(config, SALES)
    assert "border-bottom:1px solid #ccc" in html
    assert "background-color:#fff" in html
    assert "color:#dc2626" in html
    assert "#t1{" in html
    assert "color: red" in html
    assert ">-2<" not in html
    assert ">10<" in html


def test_table_headers_repeat_sort_limit_empty_link():
    config = {
        "id": "t2",
        "title": "Sales {{quarter}}",
        "headers": [
            {
                "cells": [
                    {"value": "Name", "rowspan": 1},
                    {"value": "Amt", "colspan": 1},
                ]
            }
        ],
        "columns": [{"empty_text": "—"}],
        "rows": [
            {
                "repeat_for": "items",
                "sort_by": "name",
                "limit": 3,
                "cells": [
                    {"value": "{{item.name}}", "link": "{{item.url}}"},
                    {"value": "{{item.amount}}"},
                ],
            }
        ],
    }
    html = build(config, SALES)
    assert "<h1>Sales Q1</h1>" in html
    assert "<a href=" in html
    assert html.count("<tr>") >= 3
    names = ["E1", "N1", "N2", "N3", "W1", "W2"]
    present = [n for n in names if f">{n}<" in html or f">{n}</a>" in html]
    assert present[0] == "E1"
    assert "W2" not in html or html.find("E1") < html.find("W2")


def test_grouping_sort_rowspan_and_left_column():
    config = {
        "id": "g1",
        "headers": [{"cells": [{"value": "Name"}, {"value": "Region"}, {"value": "Amt"}]}],
        "rows": [
            {
                "repeat_for": "items",
                "group_by": "region",
                "sort_by": "name",
                "cells": [
                    {"value": "{{item.name}}"},
                    {"value": "{{item.region}}"},
                    {"value": "{{item.amount}}"},
                ],
            }
        ],
    }
    resolved = resolve(config, SALES)
    table = resolved.blocks[0]["table"]
    header_texts = [c.text for c in table.thead[0].cells]
    assert header_texts[0] == "Region"
    body = table.tbody
    first_cells = [row.cells[0].text for row in body]
    assert first_cells[0] == "East"
    north = [row for row in body if row.cells and row.cells[0].text == "North"]
    assert len(north) == 1
    assert north[0].cells[0].rowspan == 3
    follow = body[body.index(north[0]) + 1]
    assert follow.cells[0].text != "North"
    html = build(config, SALES, format="html")
    assert 'rowspan="3"' in html
    md = build(config, SALES, format="md")
    assert "North" in md
    lines = [ln for ln in md.splitlines() if ln.startswith("|")]
    data_lines = [ln for ln in lines[2:] if ln.strip()]
    north_rows = [ln for ln in data_lines if "North" in ln]
    assert len(north_rows) == 1


def test_group_injects_missing_field_cell():
    config = {
        "headers": [{"cells": [{"value": "Name"}]}],
        "rows": [
            {
                "repeat_for": "items",
                "group_by": "region",
                "cells": [{"value": "{{item.name}}"}],
            }
        ],
    }
    table = resolve(config, SALES).blocks[0]["table"]
    assert table.thead[0].cells[0].text in ("Region", "region")
    assert table.tbody[0].cells[0].text in {"East", "North", "West"}


def test_document_blocks_html_and_md():
    config = {
        "name": "Report {{quarter}}",
        "tagline": "hello",
        "blocks": [
            {"type": "heading", "level": 2, "value": "Intro"},
            {"type": "paragraph", "value": "Quarter {{quarter}}"},
            {"type": "list", "repeat_for": "items", "item": "{{item.name}}"},
            {"type": "code", "language": "python", "value": "print({{quarter ?? \"x\"}})"},
            {"type": "hr"},
            {"type": "html", "value": "<b>{{quarter}}</b>", "raw": True},
            {
                "type": "section",
                "title": "More",
                "blocks": [{"type": "paragraph", "value": "nested"}],
            },
        ],
    }
    html = build(config, SALES, format="html")
    assert "<h1>Report Q1</h1>" in html
    assert "<h2>Intro</h2>" in html
    assert "<p>Quarter Q1</p>" in html
    assert "<ul>" in html and "<li>W1</li>" in html
    assert "<pre>" in html and "<code" in html
    assert "<hr>" in html
    assert "<b>Q1</b>" in html
    assert "<section>" in html
    md = build(config, SALES, format="md")
    assert "# Report Q1" in md
    assert "## Intro" in md
    assert "Quarter Q1" in md
    assert "- W1" in md
    assert "```python" in md
    assert "---" in md
    assert "## More" in md


def test_build_accepts_bare_table_or_document():
    table = {
        "headers": [{"cells": [{"value": "N"}]}],
        "rows": [{"cells": [{"value": "{{quarter}}"}]}],
    }
    html = build(table, SALES)
    assert "<table" in html
    assert ">Q1<" in html
    doc = {"blocks": [{"type": "paragraph", "value": "{{quarter}}"}]}
    assert "Q1" in build(doc, SALES, format="md")


def test_fluent_builder_html_and_md_and_to_config():
    data = SALES
    builder = (
        HtmlBuilder()
        .style("cell", border="1px solid #ccc", padding="6px 12px")
        .style("cell_red", color="#dc2626")
        .style("header", font_weight="bold")
        .table(id="t1", striped=True, default_cell_style_name="cell")
        .header_row()
        .cell("Account Status", style_name="header")
        .end_row()
        .body_row(repeat_for="items", group_by="region")
        .cell("{{item.region}}")
        .cell("{{item.name}}", style={"text_align": "left"})
        .cell("{{item.amount}}", style_rules=[("{{item.amount > 0}}", "cell_red")])
        .end_row()
    )
    html = builder.build(data)
    assert 'id="t1"' in html
    assert "text-align:left" in html or "text-align: left" in html
    assert "border:1px solid #ccc" in html
    cfg = builder.to_config()
    dumped = cfg.model_dump()
    assert dumped["rows"][0]["group_by"] == "region"
    md = builder.build(data, format="md")
    assert "East" in md and "North" in md and "West" in md


def test_builder_document_section_ul():
    html = (
        HtmlBuilder()
        .name("Doc")
        .section("S1")
        .paragraph("p {{quarter}}")
        .ul(repeat_for="items", item="{{item.name}}")
        .end_section()
        .hr()
        .build(SALES)
    )
    assert "<section>" in html
    assert "p Q1" in html
    assert "<hr>" in html
    assert "<li>N1</li>" in html
