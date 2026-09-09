#!/usr/bin/env python3
"""Move inline Razor `@code` blocks into `.razor.cs` code-behind partial classes.

Every component in the repo is meant to be a markup file plus a code-behind. This walks the tree, lifts each `@code { ... }` body into
`<Component>.razor.cs`, and leaves the directives and markup behind.

The namespace has to match what the Razor compiler generates for the markup half, or the two partials never meet:

1. an `@namespace` directive in the file wins,
2. else a sibling `.razor.cs` already in that folder shows what the folder uses,
3. else the Razor default, the project's root namespace plus the folder path.

Usings are kept deliberately thin — `Microsoft.AspNetCore.Components` plus whatever the file itself imported. A code-behind does not
inherit `_Imports.razor`, so anything else it needs shows up as a build error; `--add-imports` then tops up only the files named in one.

Usage:
    python3 scripts/razor/split_code_behind.py [--check] [paths...]
    python3 scripts/razor/split_code_behind.py --add-imports <file.razor.cs> [...]
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

# Razor adds these to markup automatically. A .cs file does not get them, but ImplicitUsings and the project's GlobalUsings.cs cover
# part of the list, so only the components namespace is worth emitting by default.
DEFAULT_USINGS = ["Microsoft.AspNetCore.Components"]

IMPLICIT_USINGS = {
    "System",
    "System.Collections.Generic",
    "System.IO",
    "System.Linq",
    "System.Net.Http",
    "System.Threading",
    "System.Threading.Tasks",
}

SKIP_NAMES = {"_Imports.razor"}

SKIP_DIRS = {"bin", "obj", "node_modules"}


class CodeBlock:
    def __init__(self, start: int, end: int, body: str) -> None:
        self.start = start
        self.end = end
        self.body = body


def find_code_block(text: str) -> CodeBlock | None:
    """Locates `@code { ... }` and returns its span plus the raw body, matching braces with a C#-aware scan."""
    match = re.search(r"^@code\s*\{", text, re.MULTILINE)
    if not match:
        return None

    open_index = text.index("{", match.start())
    close_index = match_brace(text, open_index)
    if close_index is None:
        raise ValueError("unbalanced @code block")

    return CodeBlock(match.start(), close_index + 1, text[open_index + 1 : close_index])


def match_brace(text: str, open_index: int) -> int | None:
    """Index of the `}` closing the `{` at open_index, skipping braces inside comments, strings, and interpolations."""
    depth = 0
    i = open_index
    n = len(text)
    while i < n:
        ch = text[i]
        pair = text[i : i + 2]

        if pair == "//":
            i = text.find("\n", i)
            if i < 0:
                return None
            continue
        if pair == "/*":
            i = text.find("*/", i)
            if i < 0:
                return None
            i += 2
            continue
        if text[i : i + 3] == '"""':
            i = skip_raw_string(text, i)
            continue
        if ch == '"' or pair in ('@"', '$"') or text[i : i + 3] == '$@"' or text[i : i + 3] == '@$"':
            i = skip_string(text, i)
            continue
        if ch == "'":
            i = skip_char(text, i)
            continue

        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                return i
        i += 1

    return None


def skip_string(text: str, i: int) -> int:
    verbatim = False
    while text[i] in "@$":
        verbatim = verbatim or text[i] == "@"
        i += 1

    i += 1  # opening quote
    n = len(text)
    while i < n:
        ch = text[i]
        if verbatim:
            if ch == '"':
                if text[i : i + 2] == '""':
                    i += 2
                    continue
                return i + 1
        else:
            if ch == "\\":
                i += 2
                continue
            if ch == '"':
                return i + 1
        i += 1

    return n


def skip_raw_string(text: str, i: int) -> int:
    fence = 0
    while i + fence < len(text) and text[i + fence] == '"':
        fence += 1

    closing = '"' * fence
    end = text.find(closing, i + fence)
    return len(text) if end < 0 else end + fence


def skip_char(text: str, i: int) -> int:
    i += 1
    n = len(text)
    while i < n:
        if text[i] == "\\":
            i += 2
            continue
        if text[i] == "'":
            return i + 1
        i += 1

    return n


# A member holding inline markup (`RenderFragment X => __builder => { <div/> }`) is Razor syntax, not C#, so it has to stay in the
# markup file. Everything else in the same block still moves.
MARKUP_MEMBER = re.compile(r"__builder|@</?[a-zA-Z]|^\s*</?[a-zA-Z]", re.MULTILINE)

MEMBER_TAIL = re.compile(r"[ \t]*(?:=[^;]*;|;)?[ \t]*(?://[^\n]*)?(?=\n|\Z)")


def split_members(body: str) -> list[str]:
    """Splits a class body into top-level members, keeping each member's own attributes, comments, and leading whitespace attached."""
    members: list[str] = []
    start = 0
    i = 0
    n = len(body)
    while i < n:
        ch = body[i]
        pair = body[i : i + 2]

        if pair == "//":
            i = body.find("\n", i)
            if i < 0:
                break
            continue
        if pair == "/*":
            i = body.find("*/", i)
            if i < 0:
                break
            i += 2
            continue
        if body[i : i + 3] == '"""':
            i = skip_raw_string(body, i)
            continue
        if ch == '"' or pair in ('@"', '$"') or body[i : i + 3] in ('$@"', '@$"'):
            i = skip_string(body, i)
            continue
        if ch == "'":
            i = skip_char(body, i)
            continue

        if ch == "{":
            close = match_brace(body, i)
            if close is None:
                break

            # A brace can also be a property pattern (`is { } x`) or an object initializer mid-expression, so it only ends the member
            # when nothing but an optional `;`, `= value;`, or comment follows it on the line.
            tail = MEMBER_TAIL.match(body, close + 1)
            if tail is None:
                i = close + 1
                continue

            members.append(body[start : tail.end()])
            start = i = tail.end()
            continue

        if ch == ";":
            members.append(body[start : i + 1])
            start = i + 1
            i += 1
            continue

        i += 1

    if body[start:].strip():
        members.append(body[start:])

    return members


def dedent_body(body: str) -> str:
    lines = body.strip("\n").rstrip().split("\n")
    indents = [len(line) - len(line.lstrip()) for line in lines if line.strip()]
    trim = min(indents) if indents else 0
    return "\n".join(line[trim:] if line.strip() else "" for line in lines)


def project_file(path: Path) -> Path | None:
    for parent in path.parents:
        candidates = sorted(parent.glob("*.csproj"))
        if candidates:
            return candidates[0]
        if parent == REPO_ROOT:
            break
    return None


def root_namespace(csproj: Path) -> str:
    text = csproj.read_text(encoding="utf-8")
    match = re.search(r"<RootNamespace>(.*?)</RootNamespace>", text)
    return match.group(1).strip() if match else csproj.stem


def sibling_namespace(path: Path) -> str | None:
    for neighbour in sorted(path.parent.glob("*.razor.cs")):
        match = re.search(r"^namespace\s+([\w.]+)\s*;", neighbour.read_text(encoding="utf-8"), re.MULTILINE)
        if match:
            return match.group(1)
    return None


def resolve_namespace(path: Path, text: str) -> str:
    match = re.search(r"^@namespace\s+([\w.]+)", text, re.MULTILINE)
    if match:
        return match.group(1)

    inherited = sibling_namespace(path)
    if inherited:
        return inherited

    csproj = project_file(path)
    if csproj is None:
        raise ValueError(f"no project file above {path}")

    parts = path.parent.relative_to(csproj.parent).parts
    return ".".join([root_namespace(csproj), *(p.replace(" ", "_").replace("-", "_") for p in parts)])


def type_parameters(text: str) -> tuple[str, list[str]]:
    """Returns the `<T, U>` suffix for the class declaration plus any `where` constraints declared on `@typeparam`."""
    names: list[str] = []
    constraints: list[str] = []
    for line in re.findall(r"^@typeparam\s+(.+)$", text, re.MULTILINE):
        parts = line.strip().split(None, 1)
        names.append(parts[0])
        if len(parts) > 1 and parts[1].strip().startswith("where"):
            constraints.append(parts[1].strip())

    return (f"<{', '.join(names)}>" if names else "", constraints)


def file_usings(text: str, code_start: int) -> list[str]:
    usings = []
    for match in re.finditer(r"^@using\s+(.+?)\s*;?\s*$", text[:code_start], re.MULTILINE):
        usings.append(match.group(1).strip())
    return usings


def keeps_alias(using: str, body: str) -> bool:
    """
    An alias (`@using Foo = Some.Long.Name`) only comes across when the moved code names it. Aliases resolve from the global namespace in
    a `.cs` file but from the enclosing namespace in markup, so copying an unused one can fail to compile on its own.
    """
    alias = re.match(r"(\w+)\s*=", using)
    return alias is None or re.search(rf"\b{alias.group(1)}\b", body) is not None


def global_usings(csproj: Path) -> set[str]:
    path = csproj.parent / "GlobalUsings.cs"
    if not path.exists():
        return set()
    return {m.group(1) for m in re.finditer(r"^global using\s+(?:static\s+)?([\w.]+)\s*;", path.read_text(encoding="utf-8"), re.MULTILINE)}


def import_usings(path: Path) -> list[str]:
    """`@using` lines from every `_Imports.razor` between the file and its project root, nearest last."""
    csproj = project_file(path)
    if csproj is None:
        return []

    chain = [path.parent]
    while chain[-1] != csproj.parent and csproj.parent in chain[-1].parents:
        chain.append(chain[-1].parent)

    found: list[str] = []
    for folder in reversed(chain):
        imports = folder / "_Imports.razor"
        if imports.exists():
            for match in re.finditer(r"^@using\s+(.+?)\s*;?\s*$", imports.read_text(encoding="utf-8"), re.MULTILINE):
                found.append(match.group(1).strip())

    return found


def order_usings(usings: list[str]) -> list[str]:
    seen: dict[str, None] = {}
    for using in usings:
        seen.setdefault(using, None)

    entries = list(seen)
    aliases = sorted(u for u in entries if "=" in u)
    system = sorted(u for u in entries if u == "System" or u.startswith("System."))
    rest = sorted(u for u in entries if u not in system and u not in aliases)
    return system + rest + aliases


def render_code_behind(namespace: str, class_name: str, suffix: str, constraints: list[str], usings: list[str], body: str) -> str:
    lines = [f"using {u};" for u in usings]
    if lines:
        lines.append("")

    lines.append(f"namespace {namespace};")
    lines.append("")

    declaration = f"public partial class {class_name}{suffix}"
    if constraints:
        lines.append(declaration)
        lines.extend(f"    {c}" for c in constraints)
    else:
        lines.append(declaration)

    lines.append("{")
    lines.extend(f"    {line}" if line else "" for line in body.split("\n"))
    lines.append("}")
    return "\n".join(lines) + "\n"


def strip_code_block(text: str, block: CodeBlock, kept: str = "") -> str:
    before = text[: block.start].rstrip()
    after = text[block.end :].strip()
    parts = [before]
    if kept:
        indented = "\n".join(f"    {line}" if line else "" for line in kept.split("\n"))
        parts.append("@code {\n" + indented + "\n}")
    if after:
        parts.append(after)

    return "\n\n".join(part for part in parts if part) + "\n"


def split_file(path: Path, check: bool) -> str | None:
    text = path.read_text(encoding="utf-8")
    block = find_code_block(text)
    if block is None:
        return None

    body = dedent_body(block.body)
    if not body:
        if not check:
            path.write_text(strip_code_block(text, block), encoding="utf-8")
        return f"{path}: removed empty @code block"

    if check:
        movable = [m for m in split_members(block.body) if m.strip() and not MARKUP_MEMBER.search(m)]
        return f"{path}: has {len(movable)} member(s) that belong in a code-behind" if movable else None

    kept = ""
    if MARKUP_MEMBER.search(body):
        members = split_members(block.body)
        kept = dedent_body("".join(m for m in members if MARKUP_MEMBER.search(m)))
        body = dedent_body("".join(m for m in members if not MARKUP_MEMBER.search(m)))
        if not body:
            return f"{path}: left in place, every member renders markup"

    csproj = project_file(path)
    globals_ = global_usings(csproj) if csproj else set()
    usings = [
        u
        for u in DEFAULT_USINGS + file_usings(text, block.start)
        if u not in globals_ and u not in IMPLICIT_USINGS and not u.startswith("static ") and keeps_alias(u, body)
    ]

    namespace = resolve_namespace(path, text)
    suffix, constraints = type_parameters(text)
    target = Path(f"{path}.cs")

    if target.exists():
        merge_into_existing(target, body)
    else:
        target.write_text(render_code_behind(namespace, path.stem, suffix, constraints, order_usings(usings), body), encoding="utf-8")

    path.write_text(strip_code_block(text, block, kept), encoding="utf-8")
    return f"{path} -> {target.name}" + (" (markup members kept in @code)" if kept else "")


def merge_into_existing(target: Path, body: str) -> None:
    """Appends the lifted members to the last type in an existing code-behind rather than clobbering it."""
    text = target.read_text(encoding="utf-8").rstrip()
    close = text.rfind("}")
    if close < 0:
        raise ValueError(f"{target} has no closing brace to merge into")

    members = "\n".join(f"    {line}" if line else "" for line in body.split("\n"))
    target.write_text(f"{text[:close].rstrip()}\n\n{members}\n{text[close:]}\n", encoding="utf-8")


def add_imports(paths: list[Path]) -> None:
    """Tops a generated code-behind up with the `_Imports.razor` usings its markup half had."""
    for path in paths:
        razor = Path(str(path)[: -len(".cs")])
        if not razor.exists():
            print(f"skip {path}: no matching .razor", file=sys.stderr)
            continue

        csproj = project_file(path)
        globals_ = global_usings(csproj) if csproj else set()
        text = path.read_text(encoding="utf-8")
        existing = re.findall(r"^using\s+((?:static\s+)?[^;]+?)\s*;", text, re.MULTILINE)
        markup = razor.read_text(encoding="utf-8")
        body = text[text.find("namespace ") :]

        def wanted(using: str) -> bool:
            if using in existing or using in globals_ or using in IMPLICIT_USINGS:
                return False
            # An alias only helps if the code names it, and only if the file has not already aliased that name to something else.
            alias = re.match(r"(\w+)\s*=", using)
            return keeps_alias(using, body) and not any(u.startswith(f"{alias.group(1)} ") for u in existing) if alias else True

        extra = [u for u in dict.fromkeys(import_usings(razor) + file_usings(markup, len(markup))) if wanted(u)]
        if not extra:
            continue

        merged = order_usings(existing + extra)
        body = re.sub(r"\A(?:using\s+[^;\n]+;\n)+\n?", "", text)
        path.write_text("\n".join(f"using {u};" for u in merged) + "\n\n" + body, encoding="utf-8")
        print(f"{path}: +{len(extra)} using(s)")


TYPE_DECL = re.compile(r"\b(?:class|struct|record|interface|enum|delegate)\s+(\w+)")

EXTENSION_METHOD = re.compile(r"\b(\w+)\s*(?:<[^()]*>)?\s*\(\s*this\s+")

NAMESPACE_DECL = re.compile(r"^namespace\s+([\w.]+)", re.MULTILINE)

IDENTIFIER = re.compile(r"\b\w+\b")


def repo_symbol_index() -> dict[str, set[str]]:
    """Maps each namespace declared in the repo to the type and extension-method names it offers."""
    index: dict[str, set[str]] = {}
    for path in REPO_ROOT.rglob("*.cs"):
        if SKIP_DIRS.intersection(path.parts):
            continue

        # utf-8-sig because a BOM would otherwise push `namespace` off the start of the file and hide every type it declares.
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
        match = NAMESPACE_DECL.search(text)
        if not match:
            continue

        names = index.setdefault(match.group(1), set())
        names.update(TYPE_DECL.findall(text))
        names.update(EXTENSION_METHOD.findall(text))

    return index


def prune(paths: list[Path]) -> None:
    """Drops usings for repo namespaces the file never touches. Namespaces outside the repo are left alone — they cannot be checked."""
    index = repo_symbol_index()
    for path in paths:
        text = path.read_text(encoding="utf-8")
        header = re.match(r"\A(?:using\s+[\w.]+\s*;\n)+", text)
        if not header:
            continue

        usings = re.findall(r"^using\s+([\w.]+)\s*;", header.group(0), re.MULTILINE)
        body = text[header.end() :]
        own = re.search(r"^namespace\s+([\w.]+)", body, re.MULTILINE)
        used = set(IDENTIFIER.findall(body))
        kept = [u for u in usings if u != (own.group(1) if own else None) and (u not in index or index[u] & used)]
        if len(kept) == len(usings):
            continue

        path.write_text("".join(f"using {u};\n" for u in kept) + body, encoding="utf-8")
        print(f"{path}: -{len(usings) - len(kept)} using(s)")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("paths", nargs="*", type=Path, help="Files or directories to process. Defaults to the whole repo.")
    parser.add_argument("--check", action="store_true", help="Report components with an inline @code block without changing them.")
    parser.add_argument("--add-imports", action="store_true", help="Add _Imports.razor usings to the named code-behind files.")
    parser.add_argument("--prune", action="store_true", help="Drop usings for repo namespaces the named code-behind files never reference.")
    args = parser.parse_args()

    if args.add_imports:
        add_imports([p.resolve() for p in args.paths])
        return 0

    if args.prune:
        roots = [p.resolve() for p in args.paths] or [REPO_ROOT]
        files = [p for root in roots for p in ([root] if root.is_file() else root.rglob("*.razor.cs")) if not SKIP_DIRS.intersection(p.parts)]
        prune(sorted(set(files)))
        return 0

    roots = [p.resolve() for p in args.paths] or [REPO_ROOT]
    targets: list[Path] = []
    for root in roots:
        if root.is_file():
            targets.append(root)
        else:
            targets.extend(
                sorted(p for p in root.rglob("*.razor") if p.name not in SKIP_NAMES and not SKIP_DIRS.intersection(p.parts)))

    changed = [result for path in targets if (result := split_file(path, args.check))]
    for line in changed:
        print(line)

    print(f"{len(changed)} component(s) {'need splitting' if args.check else 'split'}")
    return 1 if args.check and changed else 0


if __name__ == "__main__":
    raise SystemExit(main())
