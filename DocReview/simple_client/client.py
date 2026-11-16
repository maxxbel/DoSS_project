"""
Minimal TUI client for the DocReview server.

Layout (simple, not fancy):
- top: server + auth status line
- middle: scrollable output/log area
- bottom: command text box where you type commands

Commands:
  register <username> <password> <role>
  login <username> <password>
  logout
  create <title> <description> <documentReference>
  list
  my                              list my authored requests
  available                       list requests available to reviewer
  assigned                        list requests assigned to me
  get <id>
  take <id>
  comment <id> <text> [--Rework]
  approve <id>
  version <id> <fileReference> <fileName> [<fileSize>] [<mediaType>]

  admin commands (requires admin login):
  admin users create <username> <password> <role>
  admin requests reassign <requestId> <newReviewerId>
  admin requests delete <requestId>

  help
  quit / exit

Environment:
  DOCREVIEW_BASE_URL  default http://localhost:5178
  DOCREVIEW_USERNAME
  DOCREVIEW_PASSWORD
"""

from __future__ import annotations

import os
import sys
import json
import datetime
from typing import Optional

import httpx

BASE_URL = os.environ.get("DOCREVIEW_BASE_URL", "http://localhost:5178").rstrip("/")
TIMEOUT = 10.0


class DocReviewClient:
    def __init__(self) -> None:
        self.token: Optional[str] = None
        self.username: Optional[str] = None
        self._client = httpx.Client(base_url=BASE_URL, timeout=TIMEOUT)
        self._output: list[str] = []
        self._append(f"Client ready. Base URL: {BASE_URL}")

    # ------------------------------------------------------------------
    # output
    # ------------------------------------------------------------------

    def _append(self, line: str) -> None:
        now = datetime.datetime.now().strftime("%H:%M:%S")
        self._output.append(f"[{now}] {line}")
        if len(self._output) > 400:
            self._output = self._output[-300:]

    def _status_line(self) -> str:
        if self.token and self.username:
            return f"logged in as {self.username}  |  token={self.token[:8]}...  |  {BASE_URL}"
        return f"not logged in  |  {BASE_URL}"

    def render(self) -> str:
        lines = [
            "============================================================",
            f" DocReview CLI  -  {self._status_line()}",
            "============================================================",
            "",
        ]
        visible = self._output[-22:] if self._output else ["(no output yet)"]
        lines.extend(visible)
        lines.append("")
        lines.append("------------------------------------------------------------")
        lines.append(" example: create My Doc \"desc\" doc://1   |   help   |   quit")
        lines.append("------------------------------------------------------------")
        lines.append("> ")
        return "\n".join(lines)

    # ------------------------------------------------------------------
    # auth
    # ------------------------------------------------------------------

    def register(self, username: str, password: str, role: str) -> str:
        # Server expects UserRole string values exactly as defined in UserRole enum:
        # Admin, Reviewer, Author. Client sends title-cased form to match that.
        role = role.strip().title()
        if role not in {"Admin", "Reviewer", "Author"}:
            raise ApiError(400, f"unsupported role: {role!r}; expected Admin, Reviewer, or Author")
        body = {"username": username, "password": password, "role": role}
        resp = self._client.post("/api/auth/register", json=body)
        self._handle(resp)
        data = self._ensure_json(resp)
        token = None
        uname = username
        if isinstance(data, dict):
            token = data.get("token")
            user = data.get("user") or data.get("User")
            if isinstance(user, dict):
                uname = user.get("username") or username
        self.token = token
        self.username = uname
        return f"registered + logged in as {uname}"

    def login(self, username: str, password: str) -> str:
        body = {"username": username, "password": password}
        resp = self._client.post("/api/auth/login", json=body)
        self._handle(resp)
        data = self._ensure_json(resp)
        token = None
        uname = username
        if isinstance(data, dict):
            token = data.get("token")
            user = data.get("user") or data.get("User")
            if isinstance(user, dict):
                uname = user.get("username") or username
        self.token = token
        self.username = uname
        return f"logged in as {uname}"

    def logout(self) -> str:
        had = self.token is not None
        self.token = None
        self.username = None
        return "logged out" if had else "was not logged in"

    # ------------------------------------------------------------------
    # requests
    # ------------------------------------------------------------------

    def create(self, title: str, description: str, document_reference: str) -> str:
        body = {
            "title": title,
            "description": description,
            "documentReference": document_reference,
        }
        resp = self._auth_post("/api/requests", json=body)
        self._handle(resp)
        data = self._ensure_json(resp)
        # Responses may be RequestDto directly, or wrapped in { request: ... }.
        rid = None
        if isinstance(data, dict):
            wrapped = data.get("request") or data.get("Request")
            target = wrapped if wrapped is not None else data
            rid = target.get("id") or target.get("Id")
        elif isinstance(data, list) and data:
            rid = data[0].get("id") or data[0].get("Id")
        if rid is None:
            raise ApiError(500, "create returned no visible request id")
        return f"created request id={rid}"

    def list_all(self) -> str:
        resp = self._auth_get("/api/requests")
        self._handle(resp)
        data = self._ensure_json(resp)
        items = data if isinstance(data, list) else []
        return self._summary_lines(items, "no requests found")

    def list_my(self) -> str:
        resp = self._auth_get("/api/requests")
        self._handle(resp)
        data = self._ensure_json(resp)
        items = data if isinstance(data, list) else []
        return self._summary_lines(items, "no requests found")

    def list_available(self) -> str:
        resp = self._auth_get("/api/requests/available")
        self._handle(resp)
        data = self._ensure_json(resp)
        items = data if isinstance(data, list) else []
        return self._summary_lines(items, "no available requests")

    def list_assigned(self) -> str:
        resp = self._auth_get("/api/requests/assigned")
        self._handle(resp)
        data = self._ensure_json(resp)
        items = data if isinstance(data, list) else []
        return self._summary_lines(items, "no assigned requests")

    @staticmethod
    def _summary_lines(items: object, empty_message: str) -> str:
        if not isinstance(items, list) or not items:
            return empty_message
        lines: list[str] = []
        for item in items:
            lines.append(DocReviewClient._request_summary_static(item))
        return "\n".join(lines)

    @staticmethod
    def _request_summary_static(item) -> str:
        if not isinstance(item, dict):
            return str(item)
        rid = item.get("id") or item.get("Id")
        title = item.get("title") or item.get("Title") or ""
        status = item.get("status") or item.get("Status") or ""
        author = item.get("authorUsername") or item.get("AuthorUsername") or ""
        reviewer = item.get("assignedReviewerUsername") or item.get("AssignedReviewerUsername") or ""
        parts = [f"[{status}] {title}"]
        if rid:
            parts.insert(0, f"id={rid}")
        if author:
            parts.append(f"by {author}")
        if reviewer:
            parts.append(f"reviewer={reviewer}")
        return "  ".join(parts)

    def get_one(self, request_id: str) -> str:
        resp = self._auth_get(f"/api/requests/{request_id}")
        self._handle(resp)
        data = self._ensure_json(resp)
        return self._request_summary(data)

    def take(self, request_id: str) -> str:
        resp = self._auth_post(f"/api/requests/{request_id}/take")
        self._handle(resp)
        return f"took request {request_id} into work"

    def comment(self, request_id: str, text: str, rework: bool = False) -> str:
        body = {"text": text, "sendToRework": rework}
        resp = self._auth_post(f"/api/requests/{request_id}/comment", json=body)
        self._handle(resp)
        target = "rework" if rework else "comment"
        return f"added {target} to request {request_id}"

    def approve(self, request_id: str) -> str:
        resp = self._auth_post(f"/api/requests/{request_id}/approve")
        self._handle(resp)
        return f"approved request {request_id}"

    def add_version(
        self,
        request_id: str,
        file_reference: str,
        file_name: str,
        file_size: Optional[str] = None,
        media_type: Optional[str] = None,
    ) -> str:
        body: dict = {
            "fileReference": file_reference,
            "fileName": file_name,
        }
        if file_size is not None:
            try:
                body["fileSize"] = int(file_size)
            except ValueError:
                body["fileSize"] = None
        if media_type:
            body["mediaType"] = media_type
        resp = self._auth_post(f"/api/requests/{request_id}/version", json=body)
        self._handle(resp)
        return f"added version to request {request_id}"

    # ------------------------------------------------------------------
    # admin commands
    # ------------------------------------------------------------------

    def admin_create_user(self, username: str, password: str, role: str) -> str:
        role = role.strip().title()
        if role not in {"Admin", "Reviewer", "Author"}:
            raise ApiError(400, f"unsupported role: {role!r}; expected Admin, Reviewer, or Author")
        body = {"username": username, "password": password, "role": role}
        resp = self._auth_post("/api/admin/users", json=body)
        self._handle(resp)
        data = self._ensure_json(resp)
        uid = None
        uname = username
        if isinstance(data, dict):
            uid = data.get("id") or data.get("Id")
            uname = data.get("username") or data.get("Username") or username
        elif isinstance(data, list) and data:
            uid = data[0].get("id") or data[0].get("Id")
            uname = data[0].get("username") or data[0].get("Username") or username
        return f"created user id={uid} as {role}" + (f" ({uname})" if uid else "")

    def admin_reassign_reviewer(self, request_id: str, new_reviewer_id: str) -> str:
        body = {"newReviewerId": new_reviewer_id}
        resp = self._auth_post(f"/api/admin/requests/{request_id}/reassign", json=body)
        self._handle(resp)
        return f"reassigned request {request_id} to reviewer {new_reviewer_id}"

    def admin_delete_request(self, request_id: str) -> str:
        resp = self._auth_post(f"/api/admin/requests/{request_id}", json={}, method="DELETE")
        self._handle(resp)
        return f"deleted request {request_id}"

    # ------------------------------------------------------------------
    # helpers
    # ------------------------------------------------------------------

    def _auth_get(self, path: str, **kwargs) -> httpx.Response:
        headers: dict = {}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return self._client.get(path, headers=headers, **kwargs)

    def _auth_post(self, path: str, **kwargs) -> httpx.Response:
        headers: dict = {"Content-Type": "application/json"}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return self._client.post(path, headers=headers, **kwargs)

    def _auth_delete(self, path: str, **kwargs) -> httpx.Response:
        headers: dict = {}
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"
        return self._client.delete(path, headers=headers, **kwargs)

    def _handle(self, resp: httpx.Response) -> None:
        try:
            body = resp.json()
        except Exception:
            body = None
        if resp.status_code >= 400:
            detail = ""
            if isinstance(body, dict):
                detail = body.get("detail") or body.get("error") or body.get("message") or ""
            if not detail:
                detail = resp.text.strip() or f"HTTP {resp.status_code}"
            raise ApiError(resp.status_code, str(detail))

    @staticmethod
    def _ensure_json(resp: httpx.Response):
        try:
            return resp.json()
        except Exception:
            raise ApiError(resp.status_code, f"non-json response: {resp.text[:200]}")




class ApiError(Exception):
    def __init__(self, status: int, message: str) -> None:
        self.status = status
        super().__init__(f"[{status}] {message}")


def parse_command(line: str):
    line = line.strip()
    if not line:
        return None
    if line in {"quit", "exit", "q"}:
        return ("quit", [])
    if line == "help":
        return ("help", [])
    if line == "logout":
        return ("logout", [])
    if line == "list":
        return ("list", [])
    if line == "my":
        return ("my", [])
    if line == "available":
        return ("available", [])
    if line == "assigned":
        return ("assigned", [])
    if line.startswith("get "):
        return ("get", [line[4:].strip()])
    if line.startswith("take "):
        return ("take", [line[5:].strip()])
    if line.startswith("approve "):
        return ("approve", [line[8:].strip()])
    if line.startswith("comment "):
        rest = line[8:].strip()
        rework = "--Rework" in rest or "--rework" in rest
        text = rest.replace("--Rework", "").replace("--rework", "").strip()
        if not text:
            return None
        return ("comment", [text, rework])
    if line.startswith("create "):
        return ("create", [x.strip() for x in _split3(line[7:].strip())])
    if line.startswith("register "):
        return ("register", [x.strip() for x in _split3(line[9:].strip())])
    if line.startswith("login "):
        parts = line[6:].strip().split()
        if len(parts) >= 2:
            return ("login", [parts[0], parts[1]])
        return None
    if line.startswith("version "):
        return ("version", [x.strip() for x in line[8:].strip().split(None, 4)])
    return (" unknown ", [line])


def do_admin(client: DocReviewClient, args: list[str]) -> str:
    if not args:
        return "usage: admin <subcommand> ...\n\nsubcommands:\n  users create <username> <password> <role>\n  requests reassign <requestId> <newReviewerId>\n  requests delete <requestId>"
    sub = args[0]
    if sub == "users":
        if len(args) < 4 or args[1] != "create":
            return "usage: admin users create <username> <password> <role>"
        return client.admin_create_user(args[2], args[3], args[4] if len(args) > 4 else "Author")
    if sub == "requests":
        if len(args) < 2:
            return "usage: admin requests <reassign|delete> ..."
        action = args[1]
        if action == "reassign":
            if len(args) < 4:
                return "usage: admin requests reassign <requestId> <newReviewerId>"
            return client.admin_reassign_reviewer(args[2], args[3])
        if action == "delete":
            if len(args) < 3:
                return "usage: admin requests delete <requestId>"
            return client.admin_delete_request(args[2])
        return f"unknown admin requests action: {action}"
    return f"unknown admin subcommand: {sub}"


def _split3(text: str):
    # split into at most 3 parts: first, second, rest
    if not text:
        return ["", "", ""]
    first, _, rest = text.partition(" ")
    second, _, third = rest.partition(" ")
    return [first, second, third]


def main() -> None:
    client = DocReviewClient()
    print(client.render(), end="")

    while True:
        try:
            import sys as _sys
            line = _sys.stdin.readline()
        except EOFError:
            break
        except KeyboardInterrupt:
            print()
            continue

        cmd, args = parse_command(line)
        if cmd is None:
            print(" unknown command (type help)")
            continue

        if cmd == "quit":
            print("bye")
            break

        if cmd == "help":
            print_help()
            print(client.render(), end="")
            continue

        if cmd == "logout":
            print(client.logout())
            print(client.render(), end="")
            continue

        try:
            if cmd == "register":
                if len(args) < 3:
                    print(" usage: register <username> <password> <role>")
                else:
                    out = client.register(args[0], args[1], args[2])
                    print(out)
                    client._append(out)
            elif cmd == "login":
                if len(args) < 2:
                    print(" usage: login <username> <password>")
                else:
                    out = client.login(args[0], args[1])
                    print(out)
                    client._append(out)
            elif cmd == "create":
                if len(args) < 3:
                    print(" usage: create <title> <description> <documentReference>")
                else:
                    out = client.create(args[0], args[1], args[2])
                    print(out)
                    client._append(out)
            elif cmd == "list":
                print(client.list_all())
                client._append("listed all requests")
            elif cmd == "my":
                print(client.list_my())
                client._append("listed my requests")
            elif cmd == "available":
                print(client.list_available())
                client._append("listed available requests")
            elif cmd == "assigned":
                print(client.list_assigned())
                client._append("listed assigned requests")
            elif cmd == "get":
                if not args:
                    print(" usage: get <id>")
                else:
                    print(client.get_one(args[0]))
                    client._append(f"got request {args[0]}")
            elif cmd == "take":
                if not args:
                    print(" usage: take <id>")
                else:
                    print(client.take(args[0]))
                    client._append(f"took request {args[0]}")
            elif cmd == "comment":
                if len(args) < 1:
                    print(" usage: comment <id> <text> [--Rework]")
                else:
                    text = args[0]
                    rework = args[1] if len(args) > 1 else False
                    print(client.comment(args[0], text, bool(rework)))
                    client._append(f"comment on {args[0]}")
            elif cmd == "approve":
                if not args:
                    print(" usage: approve <id>")
                else:
                    print(client.approve(args[0]))
                    client._append(f"approved {args[0]}")
            elif cmd == "version":
                if len(args) < 3:
                    print(" usage: version <id> <fileReference> <fileName> [fileSize] [mediaType]")
                else:
                    print(client.add_version(args[0], args[1], args[2], args[3] if len(args) > 3 else None, args[4] if len(args) > 4 else None))
                    client._append(f"added version to {args[0]}")
            elif cmd == "admin":
                if len(args) < 1:
                    print(" usage: admin <subcommand> ...")
                else:
                    print(do_admin(client, args))
                    client._append(f"admin {args[0]}")
            else:
                print(" unknown command (type help)")
        except ApiError as e:
            print(f" server error: {e}")
            client._append(f"ERROR: {e}")
        except Exception as e:
            print(f" error: {e}")
            client._append(f"ERROR: {e}")

        print(client.render(), end="")


def print_help() -> None:
    print("""   register / login
   register <username> <password> <role>
   login <username> <password>
   logout

   request management
   create <title> <description> <documentReference>
   list              list all requests
   my               list my authored requests
   available        list available requests
   assigned         list assigned requests
   get <id>         get request details
   take <id>         take request into work
   comment <id> <text> [--Rework]
   approve <id>      approve request
   version <id> <fileReference> <fileName> [fileSize] [mediaType]

   admin commands (admin login required)
   admin users create <username> <password> <role>
   admin requests reassign <requestId> <newReviewerId>
   admin requests delete <requestId>

   other
   help
   quit
""")


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print()
        sys.exit(0)
