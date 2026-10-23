"""``lyo`` command line. Query bodies are API JSON. ``--select`` and ``--amount`` fill a body that omits them."""

from __future__ import annotations

import argparse
import getpass
import json
import sys
import webbrowser
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from typing import Any, Callable
from urllib.parse import parse_qs, urlparse

from lyo_api_client import ApiClientError
from lyo_api_client.transport import UrllibTransport
from lyo_auth_api_client import login_url

from .commands import (
    create_command,
    delete_command,
    finish_login,
    get_command,
    logout_profile,
    metadata_command,
    patch_command,
    query_command,
    refresh_profile,
    save_token,
    status_command,
    update_command,
)
from .profile import MissingProfileError, default_profile_path, resolve_profile
from .session import connect

CALLBACK_HOST = "127.0.0.1"
CALLBACK_PORT = 8734
CALLBACK_ORIGIN = f"http://{CALLBACK_HOST}:{CALLBACK_PORT}"


def build_parser() -> argparse.ArgumentParser:
    """Parser for ``lyo``. Help text names the same commands as the package README."""
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument("--base-url", help="API base URL. Overrides LYO_API_BASE_URL and the profile file.")
    common.add_argument("--token", help="Bearer token. Overrides LYO_API_TOKEN and the profile file.")

    parser = argparse.ArgumentParser(
        prog="lyo",
        description="Query and change a Lyo API. Credentials come from flags, LYO_API_BASE_URL / LYO_API_TOKEN, then ~/.config/lyo/profile.json.",
    )
    sub = parser.add_subparsers(dest="command", required=True)

    auth = sub.add_parser("auth", help="Sign in, show the current user, refresh, or log out.")
    auth_sub = auth.add_subparsers(dest="auth_command", required=True)
    login = auth_sub.add_parser(
        "login",
        parents=[common],
        help="Browser sign-in on http://127.0.0.1:8734. The API must allow-list that origin.",
    )
    login.add_argument("--provider", required=True, help="Auth provider name, such as google.")
    auth_sub.add_parser(
        "token",
        parents=[common],
        help="Save a pasted PAT or CLI token. Opaque lyo_ tokens do not refresh.",
    )
    auth_sub.add_parser("status", parents=[common], help="Print the signed-in user from GET /auth/me. Does not print the token.")
    auth_sub.add_parser("refresh", parents=[common], help="Rotate the refresh token and save the new access token.")
    auth_sub.add_parser("logout", parents=[common], help="Revoke the refresh token and clear stored credentials.")

    metadata = sub.add_parser("metadata", parents=[common], help="GET {route}/Metadata, or one entity when --entity is set.")
    metadata.add_argument("--route", required=True, help="Entity route, such as /api/Job/Definition.")
    metadata.add_argument("--entity", help="Entity type for GET {route}/{entity}/Metadata.")

    query = sub.add_parser(
        "query",
        parents=[common],
        help="POST {route}/QueryProject. --select and --amount fill the body when it omits them.",
    )
    query.add_argument("--route", required=True, help="Entity route, such as /api/Job/Definition.")
    query.add_argument("--select", help="Comma-separated Select fields, used when the body omits Select.")
    query.add_argument("--amount", type=int, help="Page size. Default 20 when the body omits Amount.")
    query.add_argument("--body", help="JSON file of a ProjectionQueryReq, or - for stdin.")

    get = sub.add_parser("get", parents=[common], help="GET {route}/{id}.")
    get.add_argument("--route", required=True, help="Entity route.")
    get.add_argument("--id", required=True, help="Entity id.")
    get.add_argument("--include", help="Comma-separated include paths.")

    create = sub.add_parser("create", parents=[common], help="POST {route}.")
    create.add_argument("--route", required=True, help="Entity route.")
    create.add_argument("--body", required=True, help="JSON file, or - for stdin.")

    patch = sub.add_parser("patch", parents=[common], help="PATCH {route}.")
    patch.add_argument("--route", required=True, help="Entity route.")
    patch.add_argument("--body", required=True, help="JSON PatchRequest file, or - for stdin.")

    update = sub.add_parser("update", parents=[common], help="POST {route}/Update.")
    update.add_argument("--route", required=True, help="Entity route.")
    update.add_argument("--id", required=True, help="Entity id.")
    update.add_argument("--body", required=True, help="JSON file of the replacement, or - for stdin.")

    delete = sub.add_parser("delete", parents=[common], help="DELETE {route}/{id}.")
    delete.add_argument("--route", required=True, help="Entity route.")
    delete.add_argument("--id", required=True, help="Entity id.")
    return parser


def main(argv: list[str] | None = None) -> None:
    """Run one ``lyo`` command. Exits with a message on a missing profile or an API error."""
    args = build_parser().parse_args(argv)
    path = default_profile_path()
    try:
        _dispatch(args, path)
    except MissingProfileError as exc:
        raise SystemExit(str(exc)) from exc
    except ApiClientError as exc:
        raise SystemExit(str(exc)) from exc


def login_browser(provider: str, base_url: str, path: Path, opener: Callable[[str], object] | None = None) -> None:
    """Open the provider login page and exchange the handoff code on ``127.0.0.1:8734``.

    The API host must allow-list ``http://127.0.0.1:8734``. If that port is taken, exit.
    """
    open_browser = opener or webbrowser.open
    target = base_url.rstrip("/") + login_url(provider, f"{CALLBACK_ORIGIN}/callback")
    found: dict[str, str] = {}

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:  # noqa: N802
            parsed = urlparse(self.path)
            code = (parse_qs(parsed.query).get("lyo_handoff") or [None])[0]
            if parsed.path != "/callback" or not code:
                self.send_response(400)
                self.end_headers()
                self.wfile.write(b"Missing handoff code.")
                return
            found["code"] = code
            self.send_response(200)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self.end_headers()
            self.wfile.write(b"Signed in. You can close this tab.")

        def log_message(self, format: str, *args: object) -> None:
            return

    try:
        server = HTTPServer((CALLBACK_HOST, CALLBACK_PORT), Handler)
    except OSError as exc:
        raise SystemExit(f"Port {CALLBACK_PORT} is in use ({CALLBACK_ORIGIN}). Free it and retry.") from exc
    open_browser(target)
    try:
        server.handle_request()
    finally:
        server.server_close()
    code = found.get("code")
    if not code:
        raise SystemExit("Login did not return a handoff code.")
    finish_login(base_url, code, CALLBACK_ORIGIN, path, UrllibTransport())


def _dispatch(args: argparse.Namespace, path: Path) -> None:
    if args.command == "auth" and args.auth_command == "login":
        base_url = _require_base_url(args, "lyo auth login")
        login_browser(args.provider, base_url, path)
        print(f"Saved profile at {path}")
        return
    if args.command == "auth" and args.auth_command == "token":
        base_url = _require_base_url(args, "lyo auth token")
        token = getpass.getpass("Token: ").strip()
        if not token:
            raise SystemExit("A token is required.")
        save_token(base_url, token, path)
        print(f"Saved profile at {path}")
        return

    profile = resolve_profile(base_url=getattr(args, "base_url", None), token=getattr(args, "token", None), path=path)
    transport = UrllibTransport()
    if args.command == "auth" and args.auth_command == "refresh":
        refresh_profile(profile, path, transport)
        print("Refreshed.")
        return
    if args.command == "auth" and args.auth_command == "logout":
        logout_profile(profile, path, transport)
        print("Logged out.")
        return

    client = connect(profile, transport=transport, profile_path=path)
    if args.command == "auth" and args.auth_command == "status":
        print(status_command(client))
        return
    if args.command == "metadata":
        print(metadata_command(client, args.route, args.entity))
        return
    if args.command == "query":
        select = [item.strip() for item in args.select.split(",") if item.strip()] if args.select else None
        print(query_command(client, args.route, select=select, amount=args.amount, body=_load_body(args.body)))
        return
    if args.command == "get":
        include = [item.strip() for item in args.include.split(",") if item.strip()] if args.include else None
        print(get_command(client, args.route, args.id, include))
        return
    if args.command == "create":
        print(create_command(client, args.route, _require_body(args.body)))
        return
    if args.command == "patch":
        print(patch_command(client, args.route, _require_body(args.body)))
        return
    if args.command == "update":
        print(update_command(client, args.route, args.id, _require_body(args.body)))
        return
    if args.command == "delete":
        print(delete_command(client, args.route, args.id))
        return
    raise SystemExit(f"Unknown command {args.command}.")


def _require_base_url(args: argparse.Namespace, command: str) -> str:
    profile = resolve_profile(base_url=args.base_url, token=args.token)
    if not profile.base_url:
        raise SystemExit(f"{command} requires --base-url or LYO_API_BASE_URL.")
    return profile.base_url


def _load_body(source: str | None) -> dict[str, Any] | None:
    if not source:
        return None
    raw = sys.stdin.read() if source == "-" else Path(source).read_text()
    if not raw.strip():
        return None
    loaded = json.loads(raw)
    if not isinstance(loaded, dict):
        raise SystemExit("--body must be a JSON object.")
    return loaded


def _require_body(source: str) -> dict[str, Any]:
    body = _load_body(source)
    if body is None:
        raise SystemExit("--body is required.")
    return body
