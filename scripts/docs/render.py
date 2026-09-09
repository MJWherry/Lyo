#!/usr/bin/env python3
"""Render a document/table JSON config + data as HTML or Markdown.

  python3 scripts/docs/render.py config.json --data data.json --format html|md
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

_DOCS = Path(__file__).resolve().parent
_SCRIPTS = _DOCS.parent
for _p in (str(_SCRIPTS), str(_DOCS)):
    if _p not in sys.path:
        sys.path.insert(0, _p)

from html import build  # noqa: E402


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Render document tooling config to HTML or Markdown.")
    parser.add_argument("config", help="JSON config (document or table)")
    parser.add_argument("--data", help="JSON data file (default: empty object)")
    parser.add_argument("--format", choices=("html", "md"), default="html")
    parser.add_argument("-o", "--output", help="Write to this path instead of stdout")
    parser.add_argument("--strict", action="store_true", help="Raise on missing paths / bad expressions")
    args = parser.parse_args(argv)

    config = json.loads(Path(args.config).read_text(encoding="utf-8"))
    data: object = {}
    if args.data:
        data = json.loads(Path(args.data).read_text(encoding="utf-8"))
    out = build(config, data, format=args.format, strict=True if args.strict else None)
    if args.output:
        Path(args.output).write_text(out, encoding="utf-8")
    else:
        sys.stdout.write(out)
        if not out.endswith("\n"):
            sys.stdout.write("\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
