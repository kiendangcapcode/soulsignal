#!/usr/bin/env python3
"""Verify the local distributor-account upload and moderation workflow.

Uses existing demo accounts and creates two uniquely named songs (one approved,
one rejected). Existing songs are not changed. No rows or files are deleted.
Requires a running Development app with FFmpeg/ffprobe configured. HTTP code
uses only Python's standard library; successful uploads send FLAC only.
"""
from __future__ import annotations

import argparse
from html.parser import HTMLParser
import importlib.util
import json
from pathlib import Path
import re
import sys
from urllib.parse import urlencode, urlparse
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("soulsignal_account_test_helpers", ROOT / "scripts/verify-distributor.py")
assert SPEC and SPEC.loader
HELPERS = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = HELPERS
SPEC.loader.exec_module(HELPERS)
HTTP = HELPERS.HTTP
require, status_is = HTTP.require, HTTP.status_is


def submission_ids(response) -> set[int]:
    return {int(value) for value in re.findall(r"/Distributor/Details/(\d+)", response.text, re.I)}


def submission_redirect(response) -> int:
    require(response.status in (302, 303), f"Upload không redirect: HTTP {response.status}.")
    location = response.headers.get("location", "")
    match = re.fullmatch(r"/Distributor/Details/(\d+)", urlparse(location).path, re.I)
    require(match is not None, f"Sai trang sau khi gửi: {location!r}.")
    return int(match.group(1))


class SourceOptions(HTMLParser):
    def __init__(self, html):
        super().__init__(convert_charrefs=True)
        self.in_select = False
        self.value = None
        self.label = []
        self.options = {}
        self.feed(html)

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "select":
            self.in_select = attrs.get("name") == "DistributorId"
        elif tag == "option" and self.in_select:
            self.value = attrs.get("value")
            self.label = []

    def handle_data(self, value):
        if self.in_select and self.value is not None:
            self.label.append(value)

    def handle_endtag(self, tag):
        if tag == "option" and self.in_select and self.value:
            if self.value.isdigit():
                self.options[" ".join(self.label).strip()] = int(self.value)
            self.value = None
        elif tag == "select":
            self.in_select = False


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--timeout", type=float, default=180)
    parser.add_argument("--report", type=Path, default=Path("docs/distributor-account-verification.json"))
    args = parser.parse_args()
    require(urlparse(args.base_url).scheme in ("http", "https") and bool(urlparse(args.base_url).netloc), "Base URL chưa hợp lệ.")
    guest, listener, admin, kite, kore = (HTTP.Session(args.base_url, args.timeout) for _ in range(5))
    runner = HTTP.Runner()
    suffix = uuid4().hex[:12]
    titles = {"kite": f"Distributor QA DistroKite {suffix}", "kore": f"Distributor QA TuneKore {suffix}"}
    recording_code = f"DEMO-ACCOUNT-QA-{suffix}"
    evidence = f"Hồ sơ quyền riêng tư QA {suffix}: nhạc tổng hợp gốc và ảnh tự tạo để kiểm thử đồ án."
    state = {"genre": None, "sources": {}, "kite_id": None, "kore_id": None}
    resources = {"songIds": [], "titles": [], "sourceRecordingCode": recording_code,
                 "testAccountsCreated": [], "existingSongsModified": [], "coverPaths": []}
    files = {
        "FlacFile": ("original.flac", "audio/flac", (ROOT / "Media/Audio/saigon-dawn.flac").read_bytes()),
        "CoverFile": ("original.png", "image/png", HELPERS.png_pixel()),
    }
    fatal = None

    def check(test_id, name, action, critical=False):
        success = runner.check(test_id, name, action)
        if critical and not success:
            raise RuntimeError(f"Dừng ca phụ thuộc vì {test_id} chưa đạt.")

    def index(session):
        response = session.request("/Distributor")
        status_is(response, 200)
        return response

    def review(song_id):
        response = admin.request(f"/Admin/ReviewSong/{song_id}")
        status_is(response, 200)
        return response

    def details(session, song_id):
        response = session.request(f"/Distributor/Details/{song_id}")
        status_is(response, 200)
        return response

    def submit(session, source, overrides=None, file_overrides=None, no_files=False, no_cover=False):
        fields = {
            "Title": titles[source], "Artist": "Soul Signal QA Artist", "GenreId": str(state["genre"]),
            "RightsHolder": "Soul Signal QA Rights Holder", "RightsEvidence": evidence,
            "AcceptRights": "true", "ExternalRecordingCode": recording_code,
            "ExternalReleaseCode": f"DEMO-RELEASE-QA-{suffix}",
            "__RequestVerificationToken": session.token("/Distributor/Upload"),
        }
        fields.update(overrides or {})
        upload_files = {} if no_files else dict(files)
        if no_cover:
            upload_files.pop("CoverFile", None)
        upload_files.update(file_overrides or {})
        return HELPERS.multipart(session, "/Distributor/Upload", fields, upload_files)

    def invalid_upload(action):
        before = submission_ids(index(kite))
        response = action()
        HELPERS.validation(response)
        require(submission_ids(index(kite)) == before, "Upload không hợp lệ vẫn tạo bản ghi.")
        return "Có lỗi validation; danh sách bài của nguồn không tăng."

    def decision(song_id, result, note):
        path = f"/Admin/ReviewSong/{song_id}"
        return admin.request(path, "POST", {
            "Id": str(song_id), "Decision": result, "ReviewNote": note,
            "__RequestVerificationToken": admin.token(path),
        }, follow=False)

    def require_redirect(response):
        require(response.status in (302, 303), f"Cần redirect, nhận {response.status}.")
        return "Quyết định đã được xử lý và redirect."

    def private_cover(song_id):
        match = re.search(r'/Songs/Cover/[^"\'<>\s?]+', review(song_id).text, re.I)
        require(match is not None, "Không có endpoint ảnh của tệp đã tải lên.")
        return match.group(0)

    print(f"Soul Signal — tài khoản phân phối — {args.base_url}; mã test {suffix}")
    try:
        check("DIST-00", "Ứng dụng sẵn sàng", lambda: status_is(guest.request("/"), 200), critical=True)
        def anonymous_routes():
            for path in ("/Distributor", "/Distributor/Upload"):
                HTTP.redirects_to_login(guest, path)
            return "Khu vực phân phối yêu cầu đăng nhập."
        check("DIST-01", "Khách chưa đăng nhập không được tải nhạc", anonymous_routes)
        check("DIST-02", "Đăng nhập Listener", lambda: HTTP.login(listener, "listener@soulsignal.local", "Home"), critical=True)
        def listener_denied():
            for path in ("/Distributor", "/Distributor/Upload", "/Admin"):
                status_is(listener.request(path), 403)
            return "Listener không có quyền phân phối hoặc duyệt."
        check("DIST-03", "Người nghe không được vào khu vực phân phối và quản trị", listener_denied)
        check("DIST-04", "Đăng nhập Admin", lambda: HTTP.login(admin, "admin@soulsignal.local", "Admin"), critical=True)
        check("DIST-05", "Đăng nhập DistroKite đến đúng khu vực", lambda: HTTP.login(kite, "distrokite@soulsignal.local", "Distributor"), critical=True)
        check("DIST-06", "Đăng nhập TuneKore đến đúng khu vực", lambda: HTTP.login(kore, "tunekore@soulsignal.local", "Distributor"), critical=True)
        def forms_and_sources():
            form = kite.request("/Distributor/Upload")
            status_is(form, 200)
            require(form.page.script_count == 1 and '/js/upload-progress.js' in form.text,
                    "Form cần đúng script tiến độ tải nhạc nội bộ.")
            require(not re.search(r'<(?:input|textarea)\b[^>]*name="(?:Description|DurationSeconds|Mp3File)"', form.text, re.I),
                    "Form vẫn yêu cầu giới thiệu, thời lượng hoặc MP3 thủ công.")
            require(not re.search(r'<(?:select|input)\b[^>]*name="DistributorId"', form.text, re.I), "Form cho phép chọn nguồn khác.")
            require("DistroKite Demo" in HELPERS.text(form), "Form không hiển thị nguồn DistroKite cố định.")
            require("TuneKore Demo" in HELPERS.text(kore.request("/Distributor/Upload")), "Form TuneKore thiếu tên nguồn.")
            genre_ids = [value for value in HELPERS.form_options(form, "GenreId") if value > 0]
            require(genre_ids, "Không có thể loại để tải bài.")
            state["genre"] = genre_ids[0]
            admin_form = admin.request("/Admin/Import")
            status_is(admin_form, 200)
            options = SourceOptions(admin_form.text).options
            for source, name in (("kite", "DistroKite Demo"), ("kore", "TuneKore Demo")):
                matching = [value for label, value in options.items() if name in label]
                require(len(matching) == 1, f"Không xác định được ID {name}.")
                state["sources"][source] = matching[0]
            return f"Nguồn cố định theo tài khoản; GenreId={state['genre']}; không script."
        check("DIST-07", "Form nguồn cố định, có dữ liệu và script tiến độ nội bộ", forms_and_sources, critical=True)
        def role_boundaries():
            for session in (kite, kore):
                status_is(session.request("/Admin"), 403)
                status_is(session.request("/Admin/Import"), 403)
                status_is(session.request("/Admin/ReviewSong/4"), 403)
            status_is(admin.request("/Distributor/Upload"), 403)
            return "Nguồn phân phối không có quyền Admin; Admin không giả danh tài khoản nguồn."
        check("DIST-08", "Phân tách quyền tài khoản nguồn và admin", role_boundaries)
        check("DIST-09", "POST tải nhạc thiếu CSRF bị chặn", lambda: status_is(kite.request("/Distributor/Upload", "POST", {}), 400))
        check("DIST-10", "Thiếu tệp không tạo bài", lambda: invalid_upload(lambda: submit(kite, "kite", no_files=True)))
        check("DIST-11", "MP3 đổi đuôi FLAC bị từ chối", lambda: invalid_upload(lambda: submit(kite, "kite", file_overrides={
            "FlacFile": ("fake.flac", "audio/flac", (ROOT / "Media/Audio/saigon-dawn.mp3").read_bytes()),
        })))
        check("DIST-12", "FLAC giả định dạng bị từ chối", lambda: invalid_upload(lambda: submit(kite, "kite", file_overrides={
            "FlacFile": ("fake.flac", "audio/flac", b"not a flac" * 100),
        })))
        check("DIST-13", "Chưa xác nhận quyền sử dụng không tạo bài", lambda: invalid_upload(lambda: submit(kite, "kite", overrides={"AcceptRights": "false"})))
        def first_submission():
            song_id = submission_redirect(submit(kite, "kite", overrides={
                "DistributorId": str(state["sources"]["kore"]), "Status": "Approved", "ImportedById": "forged-user",
                "ReviewedById": "forged-reviewer", "ReviewNote": "forged approval",
            }))
            state["kite_id"] = song_id
            resources["songIds"].append(song_id)
            resources["titles"].append(titles["kite"])
            response = review(song_id)
            require(HELPERS.pending(response), "Nguồn gửi đã vượt qua hàng chờ duyệt.")
            require("DistroKite Demo" in HELPERS.text(response), "Nguồn không lấy từ tài khoản DistroKite.")
            require("TuneKore Demo" not in HELPERS.text(response), "POST giả mạo đã đổi sang nguồn TuneKore.")
            require("forged approval" not in HELPERS.text(response), "Ghi chú giả mạo đã được lưu.")
            own = details(kite, song_id)
            require(titles["kite"] in HELPERS.text(own), "Nguồn không xem được bài vừa gửi.")
            require(re.search(rf"SS-TRACK-0*{song_id}\b", HELPERS.text(own)), "Thiếu mã nội bộ.")
            resources["coverPaths"].append(private_cover(song_id))
            return f"ID={song_id}; Pending; nguồn lấy từ tài khoản bất chấp các trường giả mạo."
        check("DIST-14", "DistroKite tải bài vào chờ duyệt, bỏ qua giả mạo nguồn/trạng thái", first_submission, critical=True)
        check("DIST-15", "Cùng nguồn không gửi trùng mã bản ghi", lambda: invalid_upload(lambda: submit(kite, "kite")))
        def second_submission():
            song_id = submission_redirect(submit(kore, "kore", no_cover=True))
            state["kore_id"] = song_id
            resources["songIds"].append(song_id)
            resources["titles"].append(titles["kore"])
            require(HELPERS.pending(review(song_id)), "Bài TuneKore mới không Pending.")
            require("TuneKore Demo" in HELPERS.text(details(kore, song_id)), "TuneKore không xem được bài của mình.")
            return f"ID={song_id}; nguồn khác được dùng cùng mã nguồn; ảnh bìa không bắt buộc."
        check("DIST-16", "TuneKore tải bài riêng, cùng mã khác nguồn, ảnh tùy chọn", second_submission, critical=True)
        def source_isolation():
            kite_id, kore_id = state["kite_id"], state["kore_id"]
            require(kore_id not in submission_ids(index(kite)), "DistroKite thấy bài TuneKore trong danh mục nguồn.")
            require(kite_id not in submission_ids(index(kore)), "TuneKore thấy bài DistroKite trong danh mục nguồn.")
            status_is(kite.request(f"/Distributor/Details/{kore_id}"), 404)
            status_is(kore.request(f"/Distributor/Details/{kite_id}"), 404)
            status_is(kore.request(f"/Songs/Stream/{kite_id}"), 403)
            status_is(kite.request(f"/Songs/Stream/{kore_id}"), 403)
            status_is(kore.request(private_cover(kite_id)), 403)
            return "Không lộ danh mục, hồ sơ, MP3 hoặc ảnh bìa chưa duyệt của nguồn khác."
        check("DIST-17", "Cách ly dữ liệu chưa công khai giữa hai nguồn", source_isolation)
        def own_preview():
            response = details(kite, state["kite_id"])
            require(len(response.page.audio) == 1 and "controls" in response.page.audio[0], "Không có audio controls nghe thử.")
            require(response.page.script_count == 0, "Trang xem bài có script.")
            HTTP.range_audio(kite, f"/Songs/Stream/{state['kite_id']}", "audio/mpeg")
            status_is(kite.request(private_cover(state["kite_id"])), 200)
            return "Chủ nguồn nghe thử MP3 qua HTTP range, xem ảnh riêng và dùng audio controls."
        check("DIST-18", "Nguồn nghe thử bài đang chờ của mình", own_preview)
        def pending_private():
            for song_id in (state["kite_id"], state["kore_id"]):
                HTTP.redirects_to_login(guest, f"/Songs/Details/{song_id}")
                status_is(listener.request(f"/Songs/Details/{song_id}"), 403)
                status_is(listener.request(f"/Songs/Stream/{song_id}"), 403)
            HTTP.redirects_to_login(guest, private_cover(state["kite_id"]))
            status_is(listener.request(private_cover(state["kite_id"])), 403)
            HTTP.catalog(guest, "/?" + urlencode({"q": suffix}), set())
            return "Bài chờ, âm thanh và ảnh riêng chưa công khai cho người nghe."
        check("DIST-19", "Người nghe không truy cập bài chờ duyệt", pending_private)
        check("DIST-20", "Admin nghe thử MP3 và FLAC trước khi duyệt", lambda: (
            HTTP.range_audio(admin, f"/Songs/Stream/{state['kite_id']}", "audio/mpeg"),
            HTTP.range_audio(admin, f"/Songs/FlacPreview/{state['kite_id']}", "audio/flac")))
        check("DIST-21", "Từ chối thiếu lý do phải báo lỗi", lambda: HELPERS.validation(decision(state["kore_id"], "Reject", "")))
        def approve_public():
            require_redirect(decision(state["kite_id"], "Approve", f"Đã kiểm tra hồ sơ QA {suffix}."))
            HTTP.catalog(guest, "/?" + urlencode({"q": titles["kite"]}), {state["kite_id"]})
            public = guest.request(f"/Songs/Details/{state['kite_id']}")
            status_is(public, 200)
            require(evidence not in HELPERS.text(public), "Hồ sơ quyền riêng tư bị công khai.")
            require(recording_code not in HELPERS.text(public), "Mã bản ghi nguồn riêng bị công khai.")
            status_is(guest.request(private_cover(state["kite_id"])), 200)
            require(not HELPERS.pending(review(state["kite_id"])), "Bài vẫn còn form Pending sau duyệt.")
            require("Đã duyệt" in HELPERS.text(details(kite, state["kite_id"])), "Nguồn chưa thấy trạng thái được duyệt.")
            return HTTP.range_audio(guest, f"/Songs/Stream/{state['kite_id']}", "audio/mpeg")
        check("DIST-22", "Admin duyệt rồi người nghe mới thấy, nguồn thấy kết quả", approve_public, critical=True)
        def reject_feedback():
            note = f"QA {suffix}: chưa đủ căn cứ quyền cho tải nhạc trong bản nộp."
            require_redirect(decision(state["kore_id"], "Reject", note))
            require(note in HELPERS.text(details(kore, state["kore_id"])), "Nguồn không thấy lý do từ chối.")
            require(not HELPERS.pending(review(state["kore_id"])), "Bài bị từ chối vẫn còn Pending.")
            status_is(listener.request(f"/Songs/Stream/{state['kore_id']}"), 403)
            HTTP.catalog(guest, "/?" + urlencode({"q": titles["kore"]}), set())
            return "TuneKore nhận đúng lý do từ chối; bài không công khai."
        check("DIST-23", "Nguồn thấy phản hồi từ chối và bài vẫn riêng tư", reject_feedback)
        def review_replay():
            require_redirect(decision(state["kite_id"], "Reject", "Quyết định gửi lại sau khi duyệt."))
            require_redirect(decision(state["kore_id"], "Approve", "Quyết định gửi lại sau khi từ chối."))
            HTTP.catalog(guest, "/?" + urlencode({"q": suffix}), {state["kite_id"]})
            return "Quyết định gửi lặp không đảo trạng thái đã xử lý."
        check("DIST-24", "Không thể gửi lại quyết định để đảo kết quả xét duyệt", review_replay)
        check("DIST-25", "Listener chưa có quyền FLAC kỹ thuật", lambda: status_is(listener.request(f"/Songs/FlacPreview/{state['kite_id']}"), 403))
    except Exception as error:
        fatal = f"{type(error).__name__}: {error}"
        print("STOP " + fatal)
    finally:
        passed = sum(item["status"] == "passed" for item in runner.results)
        report = {
            "model": "distributor-accounts", "startedAt": runner.started_at, "completedAt": HTTP.now_iso(),
            "timezone": "Asia/Saigon (UTC+07:00)", "baseUrl": args.base_url,
            "summary": {"total": len(runner.results), "passed": passed, "failed": len(runner.results) - passed},
            "resourcesCreated": resources, "fatalError": fatal,
            "limitations": [
                "Chỉ dùng tài khoản demo có sẵn; không tạo tài khoản và không sửa bài trước lần chạy.",
                "Hai bản ghi kiểm thử được giữ lại với ID chính xác; script không xóa dữ liệu hay tệp.",
                "Không gọi nhà phân phối thật, không kiểm tra âm thanh qua loa hoặc bố cục giao diện.",
            ], "tests": runner.results,
        }
        path = args.report if args.report.is_absolute() else ROOT / args.report
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Báo cáo: {path}")
        print(json.dumps(resources, ensure_ascii=False))
        print(f"Kết quả: {passed} PASS, {len(runner.results) - passed} FAIL, tổng {len(runner.results)} ca.")
    return int(bool(fatal) or any(item["status"] != "passed" for item in runner.results))


if __name__ == "__main__":
    raise SystemExit(main())
