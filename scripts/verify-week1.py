#!/usr/bin/env python3
"""HTTP integration checks for Soul Signal's local Development demo.

Uses only Python's standard library. Start the app before running this script.
One uniquely named listener account may be created per run. Its email is printed
and written to the optional JSON report for deliberate cleanup by the developer.
These checks do not verify audible playback, browser codecs, or visual layout.
"""

from __future__ import annotations

import argparse
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from html.parser import HTMLParser
from http.cookiejar import CookieJar
import json
from pathlib import Path
import re
import sys
import time
import unicodedata
from urllib.error import HTTPError
from urllib.parse import parse_qs, urlencode, urljoin, urlparse
from urllib.request import (
    build_opener, HTTPCookieProcessor, HTTPRedirectHandler, Request,
)
from uuid import uuid4


LOCAL_ZONE = timezone(timedelta(hours=7))
DEMO_PASSWORD = "123456"
APPROVED_IDS = {1, 2, 3}
ALL_SEED_IDS = {1, 2, 3, 4}
VOID_TAGS = {
    "area", "base", "br", "col", "embed", "hr", "img", "input", "link",
    "meta", "param", "source", "track", "wbr",
}


def now_iso() -> str:
    return datetime.now(LOCAL_ZONE).isoformat(timespec="seconds")


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


class Page(HTMLParser):
    """Extract rendered form tokens, links, media and small test markers."""

    def __init__(self, html: str):
        super().__init__(convert_charrefs=True)
        self.inputs: dict[str, str] = {}
        self.links: list[str] = []
        self.audio: list[dict[str, str | None]] = []
        self.sources: list[str] = []
        self.script_count = 0
        self.has_validation_error = False
        self.test_values: dict[str, str] = {}
        self.text_parts: list[str] = []
        self.table_body_rows = 0
        self._stack: list[dict] = []
        self.feed(html)
        self.close()

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        values = dict(attrs)
        if tag == "input" and values.get("name"):
            self.inputs[values["name"]] = values.get("value") or ""
        if tag == "a" and values.get("href"):
            self.links.append(values["href"])
        if tag == "audio":
            self.audio.append(values)
            if values.get("src"):
                self.sources.append(values["src"])
        if tag == "source" and values.get("src"):
            self.sources.append(values["src"])
        if tag == "script":
            self.script_count += 1
        if tag == "tr" and any(frame["tag"] == "tbody" for frame in self._stack):
            self.table_body_rows += 1
        classes = (values.get("class") or "").split()
        if any(item in classes for item in (
            "validation-summary-errors", "field-validation-error", "input-validation-error",
        )):
            self.has_validation_error = True
        if tag not in VOID_TAGS:
            self._stack.append({"tag": tag, "testid": values.get("data-testid"), "text": []})

    def handle_startendtag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self.handle_starttag(tag, attrs)
        if tag not in VOID_TAGS:
            self.handle_endtag(tag)

    def handle_endtag(self, tag: str) -> None:
        match = next((i for i in range(len(self._stack) - 1, -1, -1)
                      if self._stack[i]["tag"] == tag), None)
        if match is None:
            return
        for frame in self._stack[match:]:
            if frame["testid"]:
                self.test_values[frame["testid"]] = " ".join(frame["text"]).strip()
        del self._stack[match:]

    def handle_data(self, data: str) -> None:
        self.text_parts.append(data)
        for frame in self._stack:
            if frame["testid"]:
                frame["text"].append(data)

    @property
    def song_ids(self) -> set[int]:
        result = set()
        for href in self.links:
            parsed = urlparse(href)
            match = re.fullmatch(r"/Songs/Details/(\d+)/?", parsed.path, re.IGNORECASE)
            if match:
                result.add(int(match.group(1)))
            elif parsed.path.rstrip("/").lower() == "/songs/details":
                value = parse_qs(parsed.query).get("id", [""])[0]
                if value.isdigit():
                    result.add(int(value))
        return result

    def count(self, marker: str) -> int:
        require(marker in self.test_values, f"Thiếu data-testid={marker!r} trong HTML.")
        value = self.test_values[marker].strip()
        require(bool(re.fullmatch(r"[\d\s,.]+", value)),
                f"Marker {marker} không chỉ chứa số: {value!r}")
        return int(re.sub(r"\D", "", value))


@dataclass
class Response:
    status: int
    headers: dict[str, str]
    body: bytes
    url: str

    @property
    def text(self) -> str:
        return self.body.decode("utf-8", errors="replace")

    @property
    def page(self) -> Page:
        return Page(self.text)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


class SameOriginRedirect(HTTPRedirectHandler):
    def __init__(self, base_url: str):
        self.origin = urlparse(base_url)[:2]
        super().__init__()

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        destination = urljoin(req.full_url, newurl)
        require(urlparse(destination)[:2] == self.origin,
                f"Server định chuyển ra ngoài ứng dụng: {destination}")
        return super().redirect_request(req, fp, code, msg, headers, destination)


class Session:
    def __init__(self, base_url: str, timeout: float):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout
        jar = CookieJar()
        self.follow = build_opener(HTTPCookieProcessor(jar), SameOriginRedirect(base_url))
        self.raw = build_opener(HTTPCookieProcessor(jar), NoRedirect())

    def request(self, path: str, method: str = "GET", data: dict | None = None,
                headers: dict | None = None, follow: bool = True) -> Response:
        require(path.startswith("/"), "Đường dẫn kiểm thử phải là đường dẫn nội bộ.")
        payload = urlencode(data).encode("utf-8") if data is not None else None
        request_headers = {"User-Agent": "SoulSignal-Week1-HTTP-Verification/1.0"}
        if data is not None:
            request_headers["Content-Type"] = "application/x-www-form-urlencoded"
        request_headers.update(headers or {})
        request = Request(self.base_url + path, data=payload,
                          headers=request_headers, method=method)
        opener = self.follow if follow else self.raw
        try:
            response = opener.open(request, timeout=self.timeout)
        except HTTPError as error:
            response = error
        with response:
            return Response(response.code, {k.lower(): v for k, v in response.headers.items()},
                            response.read(), response.geturl())

    def token(self, path: str) -> str:
        response = self.request(path)
        require(response.status == 200, f"GET {path}: cần 200, nhận {response.status}.")
        token = response.page.inputs.get("__RequestVerificationToken")
        require(bool(token), f"Không tìm thấy antiforgery token trên {path}.")
        return token


class Runner:
    def __init__(self):
        self.started_at = now_iso()
        self.results: list[dict] = []

    def check(self, test_id: str, name: str, action) -> bool:
        started = time.perf_counter()
        try:
            detail = action() or "Đạt tiêu chí HTTP."
            status = "passed"
        except Exception as error:
            status = "failed"
            detail = f"{type(error).__name__}: {error}"
        self.results.append({
            "id": test_id, "name": name, "status": status, "detail": str(detail),
            "timestamp": now_iso(), "durationMs": round((time.perf_counter() - started) * 1000),
        })
        print(f"{'PASS' if status == 'passed' else 'FAIL'} {test_id} — {name}")
        if status == "failed":
            print(f"  {detail}")
        return status == "passed"


def status_is(response: Response, expected: int) -> str:
    require(response.status == expected, f"Cần HTTP {expected}, nhận {response.status}: {response.url}")
    return f"HTTP {expected}."


def catalog(session: Session, path: str, expected: set[int]) -> str:
    response = session.request(path)
    status_is(response, 200)
    actual = response.page.song_ids
    require(actual == expected, f"Cần ID bài {sorted(expected)}, nhận {sorted(actual)}.")
    return f"Danh sách ID bài: {sorted(actual)}."


def login(session: Session, email: str, expected_area: str, return_url: str = "") -> str:
    token = session.token("/Account/Login")
    response = session.request("/Account/Login", "POST", {
        "Email": email, "Password": DEMO_PASSWORD, "RememberMe": "false",
        "ReturnUrl": return_url, "__RequestVerificationToken": token,
    }, follow=False)
    require(response.status in (302, 303), f"Đăng nhập không redirect thành công: {response.status}.")
    location = response.headers.get("location", "")
    require(bool(location), "Thiếu Location sau đăng nhập.")
    target = urlparse(urljoin(session.base_url + "/", location))
    require(target[:2] == urlparse(session.base_url)[:2], f"Redirect ngoài ứng dụng: {location}")
    if expected_area == "Home":
        require(target.path.rstrip("/").lower() in ("", "/home", "/home/index"),
                f"Người nghe chuyển đến sai trang: {location}")
    else:
        require(target.path.rstrip("/").lower() in (
            f"/{expected_area.lower()}", f"/{expected_area.lower()}/index"),
            f"Sai khu vực sau đăng nhập: {location}")
    return f"Đăng nhập {email}; Location={location}."


def redirects_to_login(session: Session, path: str) -> str:
    response = session.request(path, follow=False)
    require(response.status in (302, 303), f"Cần redirect đăng nhập, nhận {response.status}.")
    destination = urlparse(response.headers.get("location", ""))
    require(destination.path.lower() == "/account/login", f"Sai Location: {destination.geturl()}")
    require(parse_qs(destination.query).get("ReturnUrl", [""])[0] == path,
            "Không giữ đúng ReturnUrl nội bộ.")
    return "Bị yêu cầu đăng nhập trước khi cấp nội dung."


def range_audio(session: Session, path: str, mime: str) -> str:
    response = session.request(path, headers={"Range": "bytes=0-1023"})
    status_is(response, 206)
    require(response.headers.get("content-type", "").split(";")[0] == mime,
            f"Sai Content-Type: {response.headers.get('content-type')}")
    content_range = response.headers.get("content-range", "")
    require(bool(re.fullmatch(r"bytes 0-1023/\d+", content_range)),
            f"Sai Content-Range: {content_range!r}")
    require(len(response.body) == 1024, f"Cần 1024 byte, nhận {len(response.body)}.")
    require(response.headers.get("content-length") == "1024", "Content-Length không khớp range.")
    return f"HTTP 206; {mime}; {content_range}; 1024 byte."


def native_audio(session: Session) -> str:
    response = session.request("/Songs/Details/1")
    status_is(response, 200)
    page = response.page
    require(len(page.audio) == 1 and "controls" in page.audio[0],
            "Cần một thẻ audio dùng controls mặc định.")
    require(any(urlparse(src).path.lower() == "/songs/stream/1" for src in page.sources),
            "Audio không dùng endpoint Stream/1.")
    require(page.script_count == 0, f"Trang vẫn chứa {page.script_count} thẻ script.")
    return "Một audio controls, nguồn Stream/1, không có thẻ script; chưa kiểm tra phát nghe thực tế."


def head_mp3(session: Session) -> str:
    response = session.request("/Songs/Stream/1", method="HEAD")
    status_is(response, 200)
    require(response.headers.get("content-type", "").split(";")[0] == "audio/mpeg",
            "HEAD không trả audio/mpeg.")
    length = int(response.headers.get("content-length", "0"))
    require(length > 1024, f"Content-Length MP3 không hợp lệ: {length}.")
    require(not response.body, "HEAD không được có nội dung response.")
    return f"HEAD audio/mpeg; {length} byte; không có body."


def get_logout_no_change(session: Session) -> str:
    response = session.request("/Account/Logout")
    require(response.status in (404, 405),
            f"GET đăng xuất phải không có route hoặc không cho phép method, nhận {response.status}.")
    home = session.request("/")
    status_is(home, 200)
    role = home.page.test_values.get("current-role", "")
    normalized_role = "".join(char for char in unicodedata.normalize("NFD", role.lower())
                              if unicodedata.category(char) != "Mn")
    require("listener" in normalized_role or "nguoi nghe" in normalized_role,
            f"GET đăng xuất làm mất phiên Listener: current-role={role!r}.")
    status_is(session.request("/Songs/Details/4"), 403)
    return f"HTTP {response.status}; current-role={role!r}; phiên Listener vẫn được giữ."


def bad_login(session: Session) -> str:
    token = session.token("/Account/Login")
    response = session.request("/Account/Login", "POST", {
        "Email": f"missing-{uuid4().hex}@example.test", "Password": "WrongPassword@123!",
        "__RequestVerificationToken": token,
    }, follow=False)
    status_is(response, 200)
    require(response.page.has_validation_error, "Đăng nhập sai không hiển thị lỗi validation.")
    return "Thông tin sai trả 200 với lỗi validation; không tạo phiên đăng nhập."


def invalid_registration(session: Session, email: str, display_name: str,
                         confirm_password: str = DEMO_PASSWORD) -> str:
    token = session.token("/Account/Register")
    response = session.request("/Account/Register", "POST", {
        "Email": email, "DisplayName": display_name, "Password": DEMO_PASSWORD,
        "ConfirmPassword": confirm_password, "__RequestVerificationToken": token,
    }, follow=False)
    status_is(response, 200)
    page = response.page
    require("ConfirmPassword" in page.inputs and page.has_validation_error,
            "Cần trả lại form đăng ký kèm lỗi validation.")
    return "HTTP 200, form có lỗi validation; số tài khoản được đối chiếu ở bước riêng."


def user_count(admin: Session) -> int:
    response = admin.request("/Admin")
    status_is(response, 200)
    return response.page.count("user-count")


def blocked_legacy_publisher(session: Session) -> str:
    token = session.token("/Account/Login")
    response = session.request("/Account/Login", "POST", {
        "Email": "publisher@soulsignal.local", "Password": DEMO_PASSWORD,
        "__RequestVerificationToken": token,
    }, follow=False)
    status_is(response, 200)
    require(response.page.has_validation_error, "Tài khoản Publisher cũ vẫn đăng nhập được.")
    redirects_to_login(session, "/Admin")
    return "Tài khoản Publisher cũ không tạo phiên đăng nhập; nguồn phân phối không phải tài khoản website."


def registration(session: Session, email: str) -> str:
    token = session.token("/Account/Register")
    response = session.request("/Account/Register", "POST", {
        "Email": email, "DisplayName": "Week 1 HTTP Test", "Password": DEMO_PASSWORD,
        "ConfirmPassword": DEMO_PASSWORD, "Role": "Admin", "Roles": "Admin",
        "IsAdmin": "true", "IsVip": "true", "VipPlanId": "1",
        "__RequestVerificationToken": token,
    }, follow=False)
    require(response.status in (302, 303), f"Đăng ký hợp lệ không thành công: HTTP {response.status}.")
    home = session.request("/")
    status_is(home, 200)
    role = home.page.test_values.get("current-role", "")
    normalized_role = "".join(char for char in unicodedata.normalize("NFD", role.lower())
                              if unicodedata.category(char) != "Mn")
    require("listener" in normalized_role or "nguoi nghe" in normalized_role,
            f"Không xác nhận được role Listener từ nhãn current-role: {role!r}")
    return f"Tạo đúng một tài khoản {email}; nhãn role={role!r}; quyền được thử ở các ca tiếp theo."


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--report", type=Path, help="Ghi kết quả JSON, ví dụ docs/verification-results.json")
    parser.add_argument("--timeout", type=float, default=15, help="Timeout mỗi request, tính bằng giây")
    args = parser.parse_args()
    require(urlparse(args.base_url).scheme in ("http", "https"), "base-url phải dùng HTTP/HTTPS.")
    require(bool(urlparse(args.base_url).netloc), "base-url thiếu hostname.")
    runner = Runner()
    test_email = f"week1-test-{uuid4().hex}@example.test"
    sessions = {name: Session(args.base_url, args.timeout)
                for name in ("guest", "listener", "legacy", "admin", "external", "register", "bad")}
    guest, listener, admin = (sessions[name] for name in ("guest", "listener", "admin"))
    print(f"Soul Signal — kiểm chứng HTTP tuần 1 — {args.base_url}")
    print(f"Tài khoản test duy nhất có thể được tạo: {test_email}")
    print("Không kiểm tra âm thanh nghe thực tế, codec trình duyệt hoặc bố cục giao diện.\n")

    ready = runner.check("HTTP-00", "Ứng dụng trả trang chủ", lambda: status_is(guest.request("/"), 200))
    if ready:
        runner.check("CAT-01", "Khách chỉ thấy ba bài Approved", lambda: catalog(guest, "/", APPROVED_IDS))
        runner.check("CAT-02", "Tìm Afterglow trả đúng một bài", lambda: catalog(guest, "/?q=Afterglow", {2}))
        runner.check("CAT-03", "Từ khóa không tồn tại trả danh sách rỗng",
                     lambda: catalog(guest, "/?q=" + uuid4().hex, set()))
        runner.check("CAT-04", "Từ khóa chỉ khoảng trắng được bỏ qua",
                     lambda: catalog(guest, "/?q=%20%20%20", APPROVED_IDS))
        runner.check("AUDIO-01", "Trang nghe dùng audio mặc định, không script", lambda: native_audio(guest))
        runner.check("AUDIO-02", "HEAD MP3 có MIME và chiều dài đúng", lambda: head_mp3(guest))
        runner.check("AUDIO-03", "MP3 hỗ trợ byte range", lambda: range_audio(guest, "/Songs/Stream/1", "audio/mpeg"))
        runner.check("AUDIO-04", "Không có URL FLAC tĩnh công khai",
                     lambda: status_is(guest.request("/Media/Audio/saigon-dawn.flac"), 404))
        runner.check("AUDIO-05", "Không có URL MP3 private tĩnh công khai",
                     lambda: status_is(guest.request("/Media/Audio/saigon-dawn.mp3"), 404))
        runner.check("SONG-01", "Chi tiết ID không tồn tại trả 404",
                     lambda: status_is(guest.request("/Songs/Details/99999999"), 404))
        runner.check("SONG-02", "Stream ID không tồn tại trả 404",
                     lambda: status_is(guest.request("/Songs/Stream/99999999"), 404))
        runner.check("AUTH-01", "Đăng nhập sai có validation", lambda: bad_login(sessions["bad"]))
        for test_id, route in (("CSRF-01", "/Account/Login"), ("CSRF-02", "/Account/Register")):
            runner.check(test_id, f"POST {route} thiếu CSRF bị chặn",
                         lambda route=route: status_is(guest.request(route, "POST", {}), 400))
        runner.check("AUTH-02", "Đăng nhập Listener mẫu",
                     lambda: login(listener, "listener@soulsignal.local", "Home"))
        for test_id, route in (
            ("ROLE-01", "/Admin"),
            ("ROLE-03", "/Songs/Details/4"), ("ROLE-04", "/Songs/Stream/4"),
            ("ROLE-05", "/Songs/FlacPreview/1"),
        ):
            runner.check(test_id, f"Listener bị chặn khi gọi trực tiếp {route}",
                         lambda route=route: status_is(listener.request(route), 403))
        runner.check("ROLE-06", "Khách mở bài Pending phải đăng nhập",
                     lambda: redirects_to_login(guest, "/Songs/Details/4"))
        runner.check("ROLE-07", "Khách stream Pending phải đăng nhập",
                     lambda: redirects_to_login(guest, "/Songs/Stream/4"))
        runner.check("CSRF-03", "POST đăng xuất thiếu CSRF bị chặn",
                     lambda: status_is(listener.request("/Account/Logout", "POST", {}), 400))
        runner.check("AUTH-03", "GET đăng xuất không được thực hiện",
                     lambda: get_logout_no_change(listener))
        runner.check("AUTH-04", "Tài khoản Publisher cũ đã bị khóa đăng nhập",
                     lambda: blocked_legacy_publisher(sessions["legacy"]))
        runner.check("ROLE-08", "Không còn trang upload dành cho Publisher",
                     lambda: status_is(listener.request("/Publisher/Create"), 404))
        runner.check("ROLE-09", "Không còn trang đăng ký đối tác Publisher",
                     lambda: status_is(guest.request("/Partner/Apply"), 404))
        runner.check("AUTH-05", "Đăng nhập Admin mẫu",
                     lambda: login(admin, "admin@soulsignal.local", "Admin"))
        baseline = {"users": None}

        def admin_counts():
            response = admin.request("/Admin")
            status_is(response, 200)
            page = response.page
            counts = {name: page.count(name) for name in (
                "user-count", "distributor-count", "approved-count", "pending-count",
            )}
            require(counts["user-count"] >= 2, f"Thiếu tài khoản Listener/Admin mẫu: {counts}.")
            require(counts["distributor-count"] == 2 and counts["approved-count"] == 3
                    and counts["pending-count"] == 1, f"Số liệu không khớp seed: {counts}.")
            baseline["users"] = counts["user-count"]
            return json.dumps(counts, ensure_ascii=False)

        runner.check("ROLE-10", "Admin hiển thị số liệu thật của bản seed", admin_counts)
        runner.check("ROLE-11", "Ngay cả Admin cũng không dùng route Publisher cũ",
                     lambda: status_is(admin.request("/Publisher"), 404))
        runner.check("AUDIO-08", "Admin thử FLAC bằng byte range",
                     lambda: range_audio(admin, "/Songs/FlacPreview/1", "audio/flac"))
        runner.check("AUDIO-09", "Admin nghe được MP3 Pending",
                     lambda: range_audio(admin, "/Songs/Stream/4", "audio/mpeg"))
        runner.check("AUTH-06", "ReturnUrl bên ngoài không gây chuyển hướng ngoài ứng dụng",
                     lambda: login(sessions["external"], "listener@soulsignal.local", "Home",
                                   "https://example.invalid/should-never-be-requested"))
        register_session = sessions["register"]
        runner.check("REG-01", "Tên hiển thị trống trả validation, không lỗi 500",
                     lambda: invalid_registration(register_session, test_email, ""))
        runner.check("REG-02", "Tên còn một ký tự sau trim trả validation",
                     lambda: invalid_registration(register_session, test_email, " a "))
        runner.check("REG-03", "Mật khẩu xác nhận khác nhau bị từ chối",
                     lambda: invalid_registration(register_session, test_email, "Week 1 HTTP Test", "Mismatch@123!"))

        def verify_user_count(delta):
            require(baseline["users"] is not None, "Chưa có số tài khoản baseline hợp lệ.")
            actual = user_count(admin)
            expected = baseline["users"] + delta
            require(actual == expected, f"Cần {expected} tài khoản, thực tế {actual}.")
            return f"UserCount={actual}; thay đổi so baseline={delta}."

        runner.check("REG-04", "Ba lần đăng ký lỗi không tạo tài khoản", lambda: verify_user_count(0))
        runner.check("REG-05", "Đăng ký chỉ được Listener dù gửi trường Admin/VIP",
                     lambda: registration(register_session, test_email))
        for test_id, route, expected_status in (
            ("REG-06", "/Admin", 403), ("REG-07", "/Publisher", 404),
            ("REG-08", "/Songs/FlacPreview/1", 403), ("REG-09", "/Songs/Stream/4", 403),
        ):
            runner.check(test_id, f"Tài khoản mới không được tự nâng quyền: {route}",
                         lambda route=route, expected=expected_status: status_is(register_session.request(route), expected))
        runner.check("REG-10", "Chỉ đúng một tài khoản mới được tạo", lambda: verify_user_count(1))

        def valid_logout():
            token = listener.token("/")
            response = listener.request("/Account/Logout", "POST",
                                        {"__RequestVerificationToken": token}, follow=False)
            require(response.status in (302, 303), f"Đăng xuất không thành công: {response.status}.")
            return redirects_to_login(listener, "/Songs/Details/4")

        runner.check("AUTH-07", "POST đăng xuất hợp lệ kết thúc phiên", valid_logout)

    passed = sum(item["status"] == "passed" for item in runner.results)
    failed = len(runner.results) - passed
    report = {
        "model": "admin-import", "startedAt": runner.started_at, "completedAt": now_iso(),
        "timezone": "Asia/Saigon (UTC+07:00)", "baseUrl": args.base_url.rstrip("/"),
        "testAccountEmail": test_email,
        "summary": {"total": len(runner.results), "passed": passed, "failed": failed},
        "limitations": [
            "Chỉ kiểm chứng HTTP, HTML và phân quyền phía server.",
            "Chưa xác nhận âm thanh nghe thực tế, codec trình duyệt, nhạc dừng khi đổi trang hoặc bố cục.",
            "Không suy ra đã chạy trên máy thành viên thứ hai.",
        ],
        "tests": runner.results,
    }
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"\nBáo cáo JSON: {args.report.resolve()}")
    print(f"\nKết quả: {passed} PASS, {failed} FAIL, tổng {len(runner.results)} ca.")
    print(f"Email để đối chiếu/dọn dữ liệu test chính xác: {test_email}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
