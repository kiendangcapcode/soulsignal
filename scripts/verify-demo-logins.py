#!/usr/bin/env python3
"""Check the four Development demo usernames and their existing email aliases.

Uses only existing accounts and creates no songs, users or media files.
"""
from __future__ import annotations

import argparse
from html.parser import HTMLParser
import importlib.util
import json
from pathlib import Path
import sys


ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("soulsignal_demo_login_helpers", ROOT / "scripts/verify-week1.py")
assert SPEC and SPEC.loader
HTTP = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = HTTP
SPEC.loader.exec_module(HTTP)


class LoginFields(HTMLParser):
    def __init__(self, html: str):
        super().__init__(convert_charrefs=True)
        self.identifier_type = None
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        values = dict(attrs)
        if tag == "input" and values.get("name") == "Email":
            self.identifier_type = values.get("type")


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--timeout", type=float, default=15)
    parser.add_argument("--report", type=Path, default=Path("docs/demo-login-verification.json"))
    args = parser.parse_args()
    runner = HTTP.Runner()

    def login_form():
        page = HTTP.Session(args.base_url, args.timeout).request("/Account/Login")
        HTTP.status_is(page, 200)
        HTTP.require(LoginFields(page.text).identifier_type == "text", "Login identifier must accept plain usernames.")
        HTTP.require("Email hoặc tên đăng nhập" in page.text, "Login label does not describe both identifier types.")
        return "Identifier is a text input and accepts usernames or email addresses."

    runner.check("LOGIN-00", "Form chấp nhận tên đăng nhập ngắn", login_form)
    accounts = (
        ("admin", "Admin", "Quản trị viên", None),
        ("distrokite", "Distributor", "Nhà phân phối", "DistroKite Demo"),
        ("tunekore", "Distributor", "Nhà phân phối", "TuneKore Demo"),
        ("listener", "Home", "Người nghe", None),
    )
    count = 1
    for alias, area, expected_role, expected_source in accounts:
        for identifier in (alias, f"{alias}@soulsignal.local"):
            def check_login(identifier=identifier, area=area, expected_role=expected_role, expected_source=expected_source):
                session = HTTP.Session(args.base_url, args.timeout)
                detail = HTTP.login(session, identifier, area)
                page = session.request("/" if area == "Home" else f"/{area}")
                HTTP.status_is(page, 200)
                HTTP.require(page.page.test_values.get("current-role") == expected_role,
                             f"Expected role {expected_role!r}, got {page.page.test_values.get('current-role')!r}.")
                if expected_source:
                    HTTP.require(expected_source in page.text, "Distributor logged in to the wrong source.")
                return detail + f" Role={expected_role}."
            runner.check(f"LOGIN-{count:02}", f"Đăng nhập {identifier}", check_login)
            count += 1
    passed = sum(item["status"] == "passed" for item in runner.results)
    report = {
        "model": "development-demo-login-aliases",
        "startedAt": runner.started_at,
        "completedAt": HTTP.now_iso(),
        "timezone": "Asia/Saigon (UTC+07:00)",
        "baseUrl": args.base_url,
        "summary": {"total": len(runner.results), "passed": passed, "failed": len(runner.results) - passed},
        "resourcesCreated": {"users": [], "songs": [], "mediaFiles": []},
        "limitations": ["Uses existing Development demo accounts; does not exercise registration, failed login or lockout."],
        "tests": runner.results,
    }
    path = args.report if args.report.is_absolute() else ROOT / args.report
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Report: {path}")
    print(f"Result: {passed} PASS, {len(runner.results) - passed} FAIL.")
    return int(passed != len(runner.results))


if __name__ == "__main__":
    raise SystemExit(main())
