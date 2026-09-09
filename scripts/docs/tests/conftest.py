"""Put scripts/docs ahead of stdlib so ``import html`` is the emitter."""

from __future__ import annotations

import sys
from pathlib import Path

_DOCS = Path(__file__).resolve().parents[1]
_SCRIPTS = _DOCS.parent

for p in (str(_SCRIPTS), str(_DOCS)):
    if p in sys.path:
        sys.path.remove(p)
sys.path.insert(0, str(_SCRIPTS))
sys.path.insert(0, str(_DOCS))

_html = sys.modules.get("html")
if _html is not None and not hasattr(_html, "document_to_html"):
    del sys.modules["html"]
