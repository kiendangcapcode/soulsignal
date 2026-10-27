#!/usr/bin/env python3
"""Verify the admin-only simulated distribution inbox with standard-library HTTP.

Uses the existing Admin/Listener demo accounts. Creates no accounts and at most
three songs: one manual delivery to approve, one to reject and one fixed demo
delivery. Reports exact newly created song IDs; never deletes data itself.
"""
from __future__ import annotations

import argparse
from html.parser import HTMLParser
import importlib.util
import json
from pathlib import Path
import re
import struct
import sys
from urllib.error import HTTPError
from urllib.parse import urlencode, urlparse
from urllib.request import Request
from uuid import uuid4
import zlib

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("soulsignal_http", Path(__file__).with_name("verify-week1.py"))
assert SPEC and SPEC.loader
HTTP = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = HTTP
SPEC.loader.exec_module(HTTP)
require, status_is = HTTP.require, HTTP.status_is


class TextCollector(HTMLParser):
    def __init__(self, html: str):
        super().__init__(convert_charrefs=True)
        self.parts: list[str] = []
        self.feed(html)
    def handle_data(self, data):
        self.parts.append(data)


def text(response) -> str:
    return " ".join(TextCollector(response.text).parts)


def ids(response) -> set[int]:
    result = set(response.page.song_ids)
    result.update(int(value) for value in re.findall(r"/Admin/ReviewSong/(\d+)", response.text, re.I))
    result.update(int(value) for value in re.findall(r'data-song-id=["\'](\d+)["\']', response.text, re.I))
    return result


def pending(response) -> bool:
    # Only Pending records expose the decision selector; navigation labels do
    # not determine a record's state and may mention the review queue anywhere.
    return bool(re.search(r'<select\b[^>]*name="Decision"', response.text, re.I))


def form_options(response, field: str) -> list[int]:
    match = re.search(rf'<select\b[^>]*name="{field}"[^>]*>(.*?)</select>', response.text, re.I | re.S)
    require(match is not None, f"Thiếu select {field}.")
    return [int(value) for value in re.findall(r'<option\b[^>]*value="(\d+)"', match.group(1), re.I)]


def png_pixel() -> bytes:
    def chunk(kind, payload):
        return (struct.pack(">I", len(payload)) + kind + payload
                + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF))
    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 1, 1, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(b"\x00\xd5\xf7\x96\xff")) + chunk(b"IEND", b""))


def multipart(session, path, fields, files):
    boundary = "SoulSignalAdminImport" + uuid4().hex
    body = bytearray()
    for name, value in fields.items():
        body.extend((f"--{boundary}\r\nContent-Disposition: form-data; "
                     f'name="{name}"\r\n\r\n{value}\r\n').encode("utf-8"))
    for name, (filename, mime, value) in files.items():
        body.extend((f"--{boundary}\r\nContent-Disposition: form-data; "
                     f'name="{name}"; filename="{filename}"\r\nContent-Type: {mime}\r\n\r\n').encode("utf-8"))
        body.extend(value)
        body.extend(b"\r\n")
    body.extend(f"--{boundary}--\r\n".encode("ascii"))
    request = Request(session.base_url + path, data=bytes(body), method="POST", headers={
        "Content-Type": f"multipart/form-data; boundary={boundary}",
        "User-Agent": "SoulSignal-AdminImport-Verification/1.0",
    })
    try:
        response = session.raw.open(request, timeout=session.timeout)
    except HTTPError as error:
        response = error
    with response:
        return HTTP.Response(response.code, {k.lower(): v for k, v in response.headers.items()},
                             response.read(), response.geturl())


def validation(response):
    status_is(response, 200)
    require(response.page.has_validation_error, "Form không có thông báo validation.")
    return "HTTP 200, form có lỗi validation."


def review_redirect(response) -> int:
    require(response.status in (302, 303), f"Cần redirect thành công, nhận HTTP {response.status}.")
    location = response.headers.get("location", "")
    match = re.fullmatch(r"/Admin/ReviewSong/(\d+)", urlparse(location).path, re.I)
    require(match is not None, f"Sai Location sau nhận gói: {location!r}")
    return int(match.group(1))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--timeout", type=float, default=20)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    require(urlparse(args.base_url).scheme in ("http", "https") and bool(urlparse(args.base_url).netloc), "Base URL chưa hợp lệ.")
    guest, listener, admin = (HTTP.Session(args.base_url, args.timeout) for _ in range(3))
    runner = HTTP.Runner()
    suffix = uuid4().hex[:12]
    title = f"Admin Import QA {suffix}"
    evidence = f"Hồ sơ quyền riêng tư QA-{suffix}: sử dụng tệp tổng hợp riêng cho việc kiểm thử."
    recording_code, release_code = f"DEMO-REC-QA-{suffix}", f"DEMO-REL-QA-{suffix}"
    resources = {"model": "admin-import", "songIds": [], "exercisedExistingSongIds": [],
                 "titles": [], "fileNames": [], "testAccountsCreated": [],
                 "sharedSeedFilesDoNotDelete": ["saigon-dawn.mp3", "saigon-dawn.flac", "/images/covers/saigon-dawn.svg"],
                 "externalRecordingCode": recording_code, "externalReleaseCode": release_code}
    state = {"source_ids": [], "genre": None, "baseline": set(), "import_id": None,
             "reject_id": None, "demo_id": None, "demo_is_new": False}
    fatal = None
    files = {
        "Mp3File": ("original.mp3", "audio/mpeg", (ROOT / "Media/Audio/saigon-dawn.mp3").read_bytes()),
        "FlacFile": ("original.flac", "audio/flac", (ROOT / "Media/Audio/saigon-dawn.flac").read_bytes()),
        "CoverFile": ("original.png", "image/png", png_pixel()),
    }

    def check(test_id, name, fn, critical=False):
        ok = runner.check(test_id, name, fn)
        if critical and not ok:
            raise RuntimeError(f"Dừng ca phụ thuộc vì {test_id} chưa đạt.")

    def dashboard():
        response = admin.request("/Admin")
        status_is(response, 200)
        return response

    def review(song_id):
        response = admin.request(f"/Admin/ReviewSong/{song_id}")
        status_is(response, 200)
        return response

    def capture(song_id, expected_title=None):
        resources["songIds"].append(song_id)
        if expected_title:
            resources["titles"].append(expected_title)
        response = review(song_id)
        for value in re.findall(r'/Songs/Cover/([^"\'<>\s?]+)', response.text, re.I):
            if value not in resources["fileNames"]:
                resources["fileNames"].append(value)
        return response

    def submit(overrides=None, file_overrides=None, omit_files=False, omit_cover=False):
        fields = {
            "Title": title, "Artist": "Soul Signal QA Artist",
            "Description": "Gói nhạc mẫu gốc được nhận bởi admin để kiểm chứng quy trình phân phối giả lập.",
            "DistributorId": str(state["source_ids"][0]), "GenreId": str(state["genre"]),
            "DurationSeconds": "24", "RightsHolder": "Soul Signal QA Rights Holder",
            "RightsEvidence": evidence, "AcceptRights": "true",
            "ExternalRecordingCode": recording_code, "ExternalReleaseCode": release_code,
            "Status": "Approved", "ImportedById": "injected-user-id", "PublisherId": "injected-publisher-id",
            "__RequestVerificationToken": admin.token("/Admin/Import"),
        }
        fields.update(overrides or {})
        upload_files = {} if omit_files else dict(files)
        if omit_cover:
            upload_files.pop("CoverFile", None)
        upload_files.update(file_overrides or {})
        return multipart(admin, "/Admin/Import", fields, upload_files)

    def invalid_import(response):
        validation(response)
        require(ids(dashboard()) == state["baseline"], "Gói không hợp lệ vẫn tạo bản ghi bài hát.")
        return "Có validation, danh sách ID không tăng."

    def decide(song_id, decision, note):
        path = f"/Admin/ReviewSong/{song_id}"
        return admin.request(path, "POST", {
            "Id": song_id, "Decision": decision, "ReviewNote": note,
            "__RequestVerificationToken": admin.token(path),
        }, follow=False)

    def decision_redirect(response):
        require(response.status in (302, 303), f"Quyết định hợp lệ không redirect: {response.status}.")
        return f"HTTP {response.status}; Location={response.headers.get('location', '')}."

    def cover_url(response):
        match = re.search(r'/Songs/Cover/[^"\'<>\s?]+', response.text, re.I)
        require(match is not None, "Không tìm thấy endpoint ảnh riêng của gói import.")
        return match.group(0)

    def receive_demo():
        response = admin.request("/Admin/ReceiveDemo", "POST", {
            "DistributorId": state["source_ids"][1],
            "__RequestVerificationToken": admin.token("/Admin"),
        }, follow=False)
        return review_redirect(response)

    print(f"Soul Signal — mô hình admin nhận gói phân phối — {args.base_url}")
    print(f"Mã dữ liệu test: {suffix}; không tạo tài khoản.")
    try:
        def anonymous_access():
            for path in ("/Admin/Import", "/Admin/Distributors", "/Admin/ReviewSong/4"):
                HTTP.redirects_to_login(guest, path)
            return "Cả ba route yêu cầu đăng nhập."
        check("IMP-01", "Khách không truy cập khu vực nhận gói", anonymous_access)
        check("IMP-02", "Đăng nhập Listener hiện có",
              lambda: HTTP.login(listener, "listener@soulsignal.local", "Home"), critical=True)

        def listener_access():
            for path in ("/Admin/Import", "/Admin/Distributors", "/Admin/ReviewSong/4"):
                status_is(listener.request(path), 403)
            token = listener.token("/")
            for path in ("/Admin/Import", "/Admin/ReceiveDemo", "/Admin/ReviewSong/4"):
                status_is(listener.request(path, "POST", {
                    "DistributorId": "1", "Decision": "Approve", "__RequestVerificationToken": token,
                }), 403)
            return "GET/POST quản trị đều bị chặn phía server."
        check("IMP-03", "Listener không tự nhập, nhận hoặc duyệt bài", listener_access)
        check("IMP-04", "Luồng Partner/Publisher cũ đã được gỡ", lambda: (
            status_is(guest.request("/Partner/Apply"), 404),
            status_is(listener.request("/Publisher/Create"), 404)))

        def admin_form():
            HTTP.login(admin, "admin@soulsignal.local", "Admin")
            response = admin.request("/Admin/Import")
            status_is(response, 200)
            state["source_ids"] = form_options(response, "DistributorId")
            genres = form_options(response, "GenreId")
            require(len(state["source_ids"]) == 2 and bool(genres), "Cần hai nguồn và ít nhất một thể loại.")
            state["genre"] = genres[0]
            require(response.page.script_count == 0, "Form import có tải script.")
            require(bool(response.page.inputs.get("__RequestVerificationToken")), "Form thiếu CSRF.")
            state["baseline"] = ids(dashboard())
            require(dashboard().page.count("distributor-count") == 2, "Dashboard không có đúng hai nguồn mô phỏng.")
            return f"Hai nguồn ID={state['source_ids']}; form có CSRF, không JavaScript."
        check("IMP-05", "Admin có form nhận gói và hai nguồn mô phỏng", admin_form, critical=True)
        check("IMP-06", "POST import/nhận mẫu thiếu CSRF bị chặn", lambda: (
            status_is(admin.request("/Admin/Import", "POST", {}), 400),
            status_is(admin.request("/Admin/ReceiveDemo", "POST", {}), 400)))
        check("IMP-07", "Thiếu MP3/FLAC không tạo bài", lambda: invalid_import(submit(omit_files=True)))
        check("IMP-08", "Nội dung giả đuôi MP3 bị từ chối", lambda: invalid_import(submit(file_overrides={
            "Mp3File": ("fake.mp3", "audio/mpeg", b"not-valid-mp3-content"),
        })))
        check("IMP-09", "Nội dung giả đuôi FLAC bị từ chối", lambda: invalid_import(submit(file_overrides={
            "FlacFile": ("fake.flac", "audio/flac", b"not-valid-flac-content"),
        })))
        check("IMP-10", "Chưa xác nhận quyền thì không tạo bài",
              lambda: invalid_import(submit(overrides={"AcceptRights": "false"})))
        check("IMP-11", "SVG giả đuôi PNG bị từ chối", lambda: invalid_import(submit(file_overrides={
            "CoverFile": ("fake.png", "image/png", b'<svg xmlns="http://www.w3.org/2000/svg"></svg>'),
        })))

        def import_pending():
            state["import_id"] = review_redirect(submit())
            response = capture(state["import_id"], title)
            require(pending(response), "Gói mới không Pending dù người gửi cố đăng Status=Approved.")
            for value in (title, recording_code, release_code, evidence, "Soul Signal QA Rights Holder"):
                require(value in text(response), f"Hồ sơ thiếu {value!r}.")
            require(re.search(rf"SS-TRACK-0*{state['import_id']}\b", text(response)), "Thiếu mã nội bộ SS-TRACK.")
            require(response.page.script_count == 0, "Trang xét duyệt có script.")
            return f"Gói {state['import_id']} Pending, mã nội bộ và mã nguồn hiển thị cho admin."
        check("IMP-12", "Admin nhập gói hợp lệ vào hàng chờ", import_pending, critical=True)
        state["baseline"] = ids(dashboard())
        check("IMP-13", "Cùng nguồn không nhận trùng mã bản ghi",
              lambda: invalid_import(submit()))

        def pending_is_private():
            song_id = state["import_id"]
            HTTP.catalog(guest, "/?" + urlencode({"q": title}), set())
            HTTP.redirects_to_login(guest, f"/Songs/Details/{song_id}")
            status_is(listener.request(f"/Songs/Details/{song_id}"), 403)
            status_is(listener.request(f"/Songs/Stream/{song_id}"), 403)
            path = cover_url(review(song_id))
            HTTP.redirects_to_login(guest, path)
            status_is(listener.request(path), 403)
            return "Metadata, âm thanh và ảnh riêng của Pending đều không công khai."
        check("IMP-14", "Gói Pending và ảnh bìa chỉ admin được kiểm tra", pending_is_private)
        check("IMP-15", "Admin nghe thử MP3/FLAC Pending bằng range", lambda: (
            HTTP.range_audio(admin, f"/Songs/Stream/{state['import_id']}", "audio/mpeg"),
            HTTP.range_audio(admin, f"/Songs/FlacPreview/{state['import_id']}", "audio/flac")))
        check("IMP-16", "Từ chối phải nhập lý do",
              lambda: validation(decide(state["import_id"], "Reject", "")))

        def approve_manual():
            song_id = state["import_id"]
            decision_redirect(decide(song_id, "Approve", "Đã kiểm tra hồ sơ mô phỏng."))
            HTTP.catalog(guest, "/?" + urlencode({"q": title}), {song_id})
            public = guest.request(f"/Songs/Details/{song_id}")
            status_is(public, 200)
            require("Soul Signal QA Artist" in text(public), "Thiếu nghệ sĩ công khai.")
            require(any(name in text(public) for name in ("DistroKite Demo", "TuneKore Demo")), "Thiếu nguồn phân phối.")
            require(evidence not in text(public), "Hồ sơ quyền riêng tư bị công khai.")
            require(public.page.script_count == 0, "Trang nghe công khai có script.")
            cover = guest.request(cover_url(review(song_id)))
            status_is(cover, 200)
            require(cover.headers.get("content-type", "").startswith("image/png"), "Ảnh được duyệt sai MIME.")
            return HTTP.range_audio(guest, f"/Songs/Stream/{song_id}", "audio/mpeg")
        check("IMP-17", "Duyệt xong mới công khai nhạc, giữ riêng hồ sơ quyền", approve_manual, critical=True)

        def replay_approved():
            decision_redirect(decide(state["import_id"], "Reject", "Quyết định cũ gửi lại sau duyệt."))
            return HTTP.catalog(guest, "/?" + urlencode({"q": title}), {state["import_id"]})
        check("IMP-18", "Quyết định lặp không đảo bài đã xử lý", replay_approved)

        def receive_idempotent():
            before = ids(dashboard())
            state["demo_id"] = receive_demo()
            state["demo_is_new"] = state["demo_id"] not in before
            if state["demo_is_new"]:
                response = capture(state["demo_id"])
            else:
                resources["exercisedExistingSongIds"].append(state["demo_id"])
                response = review(state["demo_id"])
            require("SSDEMO-" in text(response) and "-REC-001" in text(response), "Gói mẫu thiếu mã nguồn cố định.")
            if state["demo_is_new"]:
                require(pending(response), "Gói mẫu nhận mới không Pending.")
            count_after_first = ids(dashboard())
            require(receive_demo() == state["demo_id"], "Nhận lại cùng gói trả một ID khác.")
            require(ids(dashboard()) == count_after_first, "Nhận lại gói tạo thêm bản ghi.")
            return f"Gói mẫu ID={state['demo_id']}; mới={state['demo_is_new']}; nhận lặp không tạo trùng."
        check("IMP-19", "Một nút nhận mẫu có mã và hoạt động idempotent", receive_idempotent, critical=True)

        def demo_review():
            song_id = state["demo_id"]
            response = review(song_id)
            if pending(response):
                status_is(listener.request(f"/Songs/Details/{song_id}"), 403)
                decision_redirect(decide(song_id, "Approve", "Duyệt gói mẫu từ nguồn phân phối giả lập."))
                status_is(guest.request(f"/Songs/Details/{song_id}"), 200)
                return HTTP.range_audio(guest, f"/Songs/Stream/{song_id}", "audio/mpeg")
            return "Gói mẫu đã tồn tại và được xử lý trước lần chạy; ReceiveDemo giữ nguyên hồ sơ hiện có."
        check("IMP-20", "Gói mẫu mới cũng phải duyệt trước khi công khai", demo_review)

        def second_import():
            second_title = title + " — reject"
            state["reject_id"] = review_redirect(submit(overrides={
                "Title": second_title, "DistributorId": str(state["source_ids"][1]),
            }, omit_cover=True))
            response = capture(state["reject_id"], second_title)
            require(pending(response), "Gói nguồn thứ hai chưa Pending.")
            return "Nguồn khác dùng cùng mã được nhận; ảnh bìa là tùy chọn."
        check("IMP-21", "Nguồn khác có mã trùng vẫn nhận được, ảnh bìa tùy chọn", second_import, critical=True)

        def reject_manual():
            song_id = state["reject_id"]
            note = f"Chưa đủ phạm vi quyền cho tải — QA {suffix}."
            decision_redirect(decide(song_id, "Reject", note))
            require(note in text(review(song_id)), "Lý do từ chối không được lưu/hiển thị.")
            status_is(listener.request(f"/Songs/Details/{song_id}"), 403)
            HTTP.catalog(guest, "/?" + urlencode({"q": title + " — reject"}), set())
            return "Lưu lý do từ chối; bài vẫn không công khai."
        check("IMP-22", "Admin từ chối có lý do và giữ gói riêng tư", reject_manual)

        def replay_rejected():
            decision_redirect(decide(state["reject_id"], "Approve", "Gửi quyết định cũ sau từ chối."))
            return status_is(listener.request(f"/Songs/Details/{state['reject_id']}"), 403)
        check("IMP-23", "Gửi lại quyết định không khôi phục gói đã từ chối", replay_rejected)
        check("IMP-24", "Listener vẫn không được lấy FLAC sau khi bài công khai",
              lambda: status_is(listener.request(f"/Songs/FlacPreview/{state['import_id']}"), 403))
    except Exception as error:
        fatal = f"{type(error).__name__}: {error}"
        print("STOP " + fatal)
    finally:
        passed = sum(item["status"] == "passed" for item in runner.results)
        report = {
            "model": "admin-import", "startedAt": runner.started_at, "completedAt": HTTP.now_iso(),
            "timezone": "Asia/Saigon (UTC+07:00)", "baseUrl": args.base_url,
            "summary": {"total": len(runner.results), "passed": passed, "failed": len(runner.results) - passed},
            "resourcesCreated": resources, "fatalError": fatal,
            "limitations": [
                "Không tạo tài khoản mới; dùng Admin và Listener mẫu.",
                "Không kết nối công ty thật, không tạo ISRC/UPC thật và không chuyển tiền.",
                "Script không xóa dữ liệu; root đối chiếu songIds và CSDL để dọn đúng tệp/record test.",
                "Chỉ kiểm tra HTTP và phân quyền, chưa xác nhận âm thanh qua loa hay bố cục trình duyệt.",
            ], "tests": runner.results,
        }
        if args.report:
            path = args.report if args.report.is_absolute() else ROOT / args.report
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
            print(f"Báo cáo: {path}")
        print(json.dumps(resources, ensure_ascii=False))
        print(f"Kết quả: {passed} PASS, {len(runner.results) - passed} FAIL, tổng {len(runner.results)} ca.")
    return int(bool(fatal) or any(item["status"] != "passed" for item in runner.results))


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    raise SystemExit(main())
