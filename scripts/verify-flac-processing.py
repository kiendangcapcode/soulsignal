#!/usr/bin/env python3
"""Verify FLAC-only upload, server-derived duration and real MP3 renditions.

Run against the local Development app. Creates and retains exactly two uniquely
named QA songs; existing songs and files are never changed or deleted. Requires
Python 3 and ffprobe on PATH (or --ffprobe). Playback through speakers is outside
this HTTP/codec verification.
"""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
from urllib.parse import urlencode, urlparse
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("soulsignal_flac_helpers", ROOT / "scripts/verify-distributor.py")
assert SPEC and SPEC.loader
HELPERS = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = HELPERS
SPEC.loader.exec_module(HELPERS)
HTTP = HELPERS.HTTP
require, status_is = HTTP.require, HTTP.status_is


def song_ids(response) -> set[int]:
    return {int(value) for value in re.findall(r"/(?:Distributor/Details|Admin/ReviewSong)/(\d+)", response.text, re.I)}


def media_files() -> set[str]:
    return {str(path.relative_to(ROOT)) for area in ("Audio", "Covers")
            for path in (ROOT / "Media" / area).glob("*") if path.is_file()}


def created_id(response, controller: str, action: str) -> int:
    require(response.status in (302, 303), f"Upload cần redirect, nhận HTTP {response.status}.")
    location = response.headers.get("location", "")
    match = re.fullmatch(rf"/{controller}/{action}/(\d+)", urlparse(location).path, re.I)
    require(match is not None, f"Sai trang sau tải lên: {location!r}.")
    return int(match.group(1))


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--timeout", type=float, default=180)
    parser.add_argument("--ffprobe", default="ffprobe")
    parser.add_argument("--report", type=Path, default=Path("docs/flac-processing-verification.json"))
    args = parser.parse_args()
    require(urlparse(args.base_url).scheme in ("http", "https") and bool(urlparse(args.base_url).netloc), "Base URL chưa hợp lệ.")
    probe_binary = shutil.which(args.ffprobe)
    require(bool(probe_binary), "Không tìm thấy ffprobe để kiểm chứng codec/bitrate thực tế.")
    guest, listener, admin, kite, kore = (HTTP.Session(args.base_url, args.timeout) for _ in range(5))
    runner = HTTP.Runner()
    suffix = uuid4().hex[:12]
    original = (ROOT / "Media/Audio/saigon-dawn.flac").read_bytes()
    mp3_original = (ROOT / "Media/Audio/saigon-dawn.mp3").read_bytes()
    forged_description = f"FORGED-DESCRIPTION-{suffix}"
    titles = {"kite": f"FLAC QA DistroKite {suffix}", "admin": f"FLAC QA Admin {suffix}"}
    state = {"genre": None, "source": None, "kite_id": None, "admin_id": None}
    resources = {"songIds": [], "titles": [], "filesCreated": [], "existingSongsModified": [], "accountsCreated": []}
    measurements = {}
    fatal = None
    initial_files = media_files()

    def check(test_id, name, action, critical=False):
        ok = runner.check(test_id, name, action)
        if critical and not ok:
            raise RuntimeError(f"Dừng ca phụ thuộc vì {test_id} chưa đạt.")

    def probe_bytes(payload: bytes, extension: str) -> dict:
        with tempfile.TemporaryDirectory(prefix="SoulSignalCodecQA-") as temporary:
            target = Path(temporary) / ("sample" + extension)
            target.write_bytes(payload)
            process = subprocess.run([
                probe_binary, "-v", "error", "-select_streams", "a:0",
                "-show_entries", "stream=codec_name,bit_rate,sample_rate,channels:format=duration",
                "-of", "json", str(target),
            ], capture_output=True, text=True, encoding="utf-8", timeout=30)
            require(process.returncode == 0, f"ffprobe không đọc được tệp trả về: {process.stderr[:250]}")
            result = json.loads(process.stdout)
            require(bool(result.get("streams")), "Không có stream âm thanh.")
            return {**result["streams"][0], "duration": float(result["format"]["duration"])}

    def dashboard():
        response = admin.request("/Admin")
        status_is(response, 200)
        return response

    def review(song_id):
        response = admin.request(f"/Admin/ReviewSong/{song_id}")
        status_is(response, 200)
        return response

    def submit(session, mode="kite", overrides=None, payload=None, cover=False):
        path = "/Distributor/Upload" if mode == "kite" else "/Admin/Import"
        fields = {
            "Title": titles[mode], "Artist": "Soul Signal Studio", "GenreId": str(state["genre"]),
            "RightsHolder": "Soul Signal Studio — QA", "RightsEvidence": "Tệp nhạc tổng hợp gốc riêng của Soul Signal để kiểm thử chuyển mã.",
            "AcceptRights": "true", "ExternalRecordingCode": f"FLAC-QA-{mode}-{suffix}",
            "__RequestVerificationToken": session.token(path),
        }
        if mode == "admin":
            fields["DistributorId"] = str(state["source"])
        fields.update(overrides or {})
        files = {"FlacFile": payload or ("original.flac", "audio/flac", original)}
        if cover:
            files["CoverFile"] = ("cover.png", "image/png", HELPERS.png_pixel())
        return HELPERS.multipart(session, path, fields, files)

    def invalid_upload(payload):
        before_ids = song_ids(dashboard())
        before_files = media_files()
        result = submit(kite, payload=payload, cover=True)
        HELPERS.validation(result)
        require(song_ids(dashboard()) == before_ids, "Tệp không hợp lệ vẫn tạo bài hát.")
        require(media_files() == before_files, "Xử lý thất bại để lại tệp mới trong Media.")
        return "Form báo lỗi; không thêm bài hoặc để lại FLAC/MP3/ảnh mới."

    def displayed_duration(response):
        rendered = HELPERS.text(response)
        require(bool(re.search(r"(?<!\d)0:24(?!\d)", rendered)), "Không hiển thị thời lượng tự đọc 0:24.")
        require("16:39" not in rendered, "Thời lượng giả 999 giây được sử dụng.")
        require(forged_description not in rendered, "Giới thiệu giả từ POST được lưu/hiển thị.")
        return "Hiển thị 0:24 từ FLAC; không dùng 999 giây hoặc giới thiệu giả."

    def rendition(song_id, path, bitrate):
        response = admin.request(path)
        status_is(response, 200)
        require(response.headers.get("content-type", "").split(";")[0] == "audio/mpeg", "Sai MIME cho MP3.")
        result = probe_bytes(response.body, ".mp3")
        require(result["codec_name"] == "mp3", "Âm thanh trả về không thực sự là MP3.")
        require(int(result.get("bit_rate", 0)) == bitrate, f"Cần bitrate {bitrate}, nhận {result.get('bit_rate')}.")
        require(abs(result["duration"] - measurements["original"]["duration"]) < 0.2, "Thời lượng MP3 khác bản gốc quá 0,2 giây.")
        measurements[f"song{song_id}_{bitrate}"] = result
        return json.dumps(result, ensure_ascii=False)

    print(f"Soul Signal — FLAC -> MP3 128/320 kbps — {args.base_url}; mã test {suffix}")
    try:
        check("FLAC-00", "Ứng dụng sẵn sàng", lambda: status_is(guest.request("/"), 200), critical=True)
        def logins():
            for session, email, area in (
                (listener, "listener@soulsignal.local", "Home"),
                (admin, "admin@soulsignal.local", "Admin"),
                (kite, "distrokite@soulsignal.local", "Distributor"),
                (kore, "tunekore@soulsignal.local", "Distributor"),
            ):
                HTTP.login(session, email, area)
            return "Bốn tài khoản demo đăng nhập đúng khu vực."
        check("FLAC-01", "Đăng nhập các vai trò", logins, critical=True)
        def forms():
            for session, path in ((kite, "/Distributor/Upload"), (admin, "/Admin/Import")):
                response = session.request(path)
                status_is(response, 200)
                require(response.page.script_count == 1 and '/js/upload-progress.js' in response.text,
                        "Form cần đúng script tiến độ tải nhạc nội bộ.")
                require(not re.search(r'<(?:input|textarea)\b[^>]*name="(?:Description|DurationSeconds|Mp3File)"', response.text, re.I),
                        f"{path} còn trường nhập giới thiệu/thời lượng/MP3.")
                require("FlacFile" in response.page.inputs, f"{path} thiếu tệp FLAC.")
            form = admin.request("/Admin/Import")
            state["genre"] = next(value for value in HELPERS.form_options(form, "GenreId") if value > 0)
            state["source"] = next(value for value in HELPERS.form_options(form, "DistributorId") if value > 0)
            return "Cả hai form chỉ nhận FLAC gốc, không yêu cầu giới thiệu/thời lượng/MP3 và không dùng JS."
        check("FLAC-02", "Form tải lên đã bỏ ba trường nhập tay", forms, critical=True)
        def original_probe():
            result = probe_bytes(original, ".flac")
            require(result["codec_name"] == "flac", "Fixture không phải FLAC.")
            require(abs(result["duration"] - 24) < 0.01, "Fixture phải dài 24 giây.")
            measurements["original"] = result
            measurements["originalSha256"] = hashlib.sha256(original).hexdigest()
            return json.dumps(result, ensure_ascii=False)
        check("FLAC-03", "Đọc codec và thời lượng bản FLAC gốc", original_probe, critical=True)
        check("FLAC-04", "MP3 đổi đuôi FLAC bị chặn và không rác tệp", lambda: invalid_upload(("renamed.flac", "audio/flac", mp3_original)))
        check("FLAC-05", "FLAC chỉ có header giả bị chặn và không rác tệp", lambda: invalid_upload(("header-only.flac", "audio/flac", b"fLaC" + b"\x00" * 256)))
        check("FLAC-06", "FLAC bị cắt giữa dữ liệu bị chặn và không rác tệp", lambda: invalid_upload(("truncated.flac", "audio/flac", original[:len(original)//2])))
        def source_upload():
            song_id = created_id(submit(kite, cover=True), "Distributor", "Details")
            state["kite_id"] = song_id
            resources["songIds"].append(song_id)
            resources["titles"].append(titles["kite"])
            response = review(song_id)
            require(HELPERS.pending(response), "Bài mới chưa được admin duyệt mà đã công khai.")
            displayed_duration(response)
            return f"ID={song_id}; upload chỉ có FLAC; Pending và tự đọc 24 giây."
        check("FLAC-07", "Nhà phân phối chỉ tải FLAC và hệ thống tự xử lý", source_upload, critical=True)
        def admin_upload():
            response = submit(admin, mode="admin", overrides={
                "DurationSeconds": "999", "Description": forged_description,
                "Status": "Approved", "AudioFileName": "forged.mp3", "HighQualityFileName": "forged-320.mp3",
            })
            song_id = created_id(response, "Admin", "ReviewSong")
            state["admin_id"] = song_id
            resources["songIds"].append(song_id)
            resources["titles"].append(titles["admin"])
            response = review(song_id)
            require(HELPERS.pending(response), "Admin import đã bỏ qua Pending từ dữ liệu POST.")
            displayed_duration(response)
            return f"ID={song_id}; dữ liệu thời lượng/giới thiệu/tên tệp/trạng thái giả bị bỏ qua."
        check("FLAC-08", "Admin nhập FLAC và bỏ qua thời lượng/giới thiệu giả", admin_upload, critical=True)
        check("FLAC-09", "MP3 thường thực sự mã hóa ở 128 kbps", lambda: rendition(state["kite_id"], f"/Songs/Stream/{state['kite_id']}", 128000), critical=True)
        check("FLAC-10", "MP3 chất lượng cao thực sự mã hóa ở 320 kbps", lambda: rendition(state["kite_id"], f"/Songs/HighQualityPreview/{state['kite_id']}", 320000), critical=True)
        def admin_renditions():
            rendition(state["admin_id"], f"/Songs/Stream/{state['admin_id']}", 128000)
            rendition(state["admin_id"], f"/Songs/HighQualityPreview/{state['admin_id']}", 320000)
            return "Admin import cũng sinh đúng hai MP3 128/320 kbps."
        check("FLAC-11", "Đường nhập admin dùng cùng quy trình chuyển mã", admin_renditions)
        def retained_original():
            for song_id in (state["kite_id"], state["admin_id"]):
                response = admin.request(f"/Songs/FlacPreview/{song_id}")
                status_is(response, 200)
                require(response.headers.get("content-type", "").split(";")[0] == "audio/flac", "Sai MIME FLAC.")
                require(hashlib.sha256(response.body).hexdigest() == measurements["originalSha256"], "FLAC gốc đã bị đổi dữ liệu.")
            return "SHA256 FLAC lưu lại giống hệt bản tải lên ở cả hai đường nhập."
        check("FLAC-12", "Giữ nguyên bản FLAC gốc", retained_original)
        def pending_privacy():
            song_id = state["kite_id"]
            require(song_id not in song_ids(kore.request("/Distributor")), "TuneKore thấy hồ sơ nguồn khác.")
            status_is(kore.request(f"/Distributor/Details/{song_id}"), 404)
            for session in (listener, kore):
                status_is(session.request(f"/Songs/Stream/{song_id}"), 403)
                status_is(session.request(f"/Songs/HighQualityPreview/{song_id}"), 403)
            HTTP.redirects_to_login(guest, f"/Songs/Stream/{song_id}")
            HTTP.catalog(guest, "/?" + urlencode({"q": suffix}), set())
            return HTTP.range_audio(kite, f"/Songs/Stream/{song_id}", "audio/mpeg")
        check("FLAC-13", "Bài xử lý xong vẫn chờ duyệt và cách ly nguồn", pending_privacy)
        def byte_ranges():
            for path, mime in ((f"/Songs/Stream/{state['kite_id']}", "audio/mpeg"),
                               (f"/Songs/HighQualityPreview/{state['kite_id']}", "audio/mpeg"),
                               (f"/Songs/FlacPreview/{state['kite_id']}", "audio/flac")):
                HTTP.range_audio(admin, path, mime)
            return "Cả ba bản hỗ trợ HTTP206/range và đúng MIME."
        check("FLAC-14", "MP3 128/320 và FLAC hỗ trợ nghe/tua bằng range", byte_ranges)
        def approve_and_public():
            song_id = state["kite_id"]
            path = f"/Admin/ReviewSong/{song_id}"
            result = admin.request(path, "POST", {"Id": str(song_id), "Decision": "Approve",
                "ReviewNote": f"Đã đối chiếu chuyển mã FLAC QA {suffix}.",
                "__RequestVerificationToken": admin.token(path)}, follow=False)
            require(result.status in (302, 303), "Quyết định duyệt không redirect.")
            HTTP.catalog(guest, "/?" + urlencode({"q": titles["kite"]}), {song_id})
            response = guest.request(f"/Songs/Stream/{song_id}")
            status_is(response, 200)
            result = probe_bytes(response.body, ".mp3")
            require(int(result["bit_rate"]) == 128000, "Người nghe công khai nhận nhầm bản320.")
            for session in (listener, kite, kore):
                status_is(session.request(f"/Songs/HighQualityPreview/{song_id}"), 403)
                status_is(session.request(f"/Songs/FlacPreview/{song_id}"), 403)
            return "Sau duyệt công khai đúng MP3128; bản320/FLAC vẫn giới hạn admin kiểm tra kỹ thuật."
        check("FLAC-15", "Duyệt công khai bản128 và giữ quyền bản320/FLAC", approve_and_public)
    except Exception as error:
        fatal = f"{type(error).__name__}: {error}"
        print("STOP " + fatal)
    finally:
        resources["filesCreated"] = sorted(media_files() - initial_files)
        passed = sum(item["status"] == "passed" for item in runner.results)
        report = {
            "model": "flac-only-processing", "startedAt": runner.started_at, "completedAt": HTTP.now_iso(),
            "timezone": "Asia/Saigon (UTC+07:00)", "baseUrl": args.base_url,
            "summary": {"total": len(runner.results), "passed": passed, "failed": len(runner.results)-passed},
            "resourcesCreated": resources, "measurements": measurements, "fatalError": fatal,
            "limitations": [
                "Dùng tệp tổng hợp gốc 24 giây; chưa thử toàn bộ biên dung lượng/thời lượng hoặc mất encoder/timeout.",
                "ffprobe kiểm tra codec/bitrate/thời lượng thật trên bytes HTTP; không xác nhận phát âm thanh qua loa.",
                "Tạo và giữ hai bản ghi QA; không sửa/xóa bài hát hoặc tệp có trước lần chạy.",
                "Chưa triển khai VIP/thanh toán/download cho người nghe; endpoint chất lượng cao chỉ là kiểm tra admin Development.",
            ], "tests": runner.results,
        }
        path = args.report if args.report.is_absolute() else ROOT / args.report
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Báo cáo: {path}")
        print(json.dumps(resources, ensure_ascii=False))
        print(f"Kết quả: {passed} PASS, {len(runner.results)-passed} FAIL, tổng {len(runner.results)} ca.")
    return int(bool(fatal) or any(item["status"] != "passed" for item in runner.results))


if __name__ == "__main__":
    raise SystemExit(main())
