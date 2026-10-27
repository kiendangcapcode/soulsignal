#!/usr/bin/env python3
"""Verify upload jobs, real processing progress and embedded FLAC artwork.

Creates at most two uniquely named QA songs and retains them for inspection.
No existing songs or Media files are changed or deleted. Requires ffmpeg and
ffprobe on PATH. HTTP checks cannot verify browser upload byte percentages;
the browser UI should be checked separately.
"""
from __future__ import annotations

import argparse
from concurrent.futures import ThreadPoolExecutor
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import time
from urllib.error import HTTPError
from urllib.parse import urlencode, urlparse
from urllib.request import Request
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("soulsignal_progress_helpers", ROOT / "scripts/verify-distributor.py")
assert SPEC and SPEC.loader
HELPERS = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = HELPERS
SPEC.loader.exec_module(HELPERS)
HTTP = HELPERS.HTTP
require, status_is = HTTP.require, HTTP.status_is
PHASES = {"waiting", "probing", "encoding", "cover", "saving", "completed", "failed", "cancelled"}


def media_files() -> set[str]:
    return {str(path.relative_to(ROOT)) for area in ("Audio", "Covers")
            for path in (ROOT / "Media" / area).glob("*") if path.is_file()}


def all_song_ids(response) -> set[int]:
    return {int(value) for value in re.findall(r"/(?:Admin/ReviewSong|Songs/Details)/(\d+)", response.text, re.I)}


def json_body(response):
    require("application/json" in response.headers.get("content-type", ""),
            f"Expected JSON, received {response.headers.get('content-type')}: HTTP {response.status}.")
    return json.loads(response.text)


def multipart(session, path, fields, files):
    boundary = "SoulSignalProgressQA" + uuid4().hex
    body = bytearray()
    for name, value in fields.items():
        body.extend((f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"\r\n\r\n{value}\r\n").encode("utf-8"))
    for name, (filename, mime, value) in files.items():
        body.extend((f"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{filename}\"\r\n"
                     f"Content-Type: {mime}\r\n\r\n").encode("utf-8"))
        body.extend(value)
        body.extend(b"\r\n")
    body.extend(f"--{boundary}--\r\n".encode("ascii"))
    request = Request(session.base_url + path, data=bytes(body), method="POST", headers={
        "Content-Type": f"multipart/form-data; boundary={boundary}",
        "X-Requested-With": "XMLHttpRequest", "Accept": "application/json",
        "User-Agent": "SoulSignal-UploadProgress-Verification/1.0",
    })
    try:
        response = session.raw.open(request, timeout=session.timeout)
    except HTTPError as error:
        response = error
    with response:
        return HTTP.Response(response.code, {k.lower(): v for k, v in response.headers.items()},
                             response.read(), response.geturl())


def main() -> int:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", default="http://localhost:5023")
    parser.add_argument("--timeout", type=float, default=240)
    parser.add_argument("--ffmpeg", default="ffmpeg")
    parser.add_argument("--ffprobe", default="ffprobe")
    parser.add_argument("--report", type=Path, default=Path("docs/upload-progress-verification.json"))
    args = parser.parse_args()
    require(urlparse(args.base_url).scheme in ("http", "https") and bool(urlparse(args.base_url).netloc), "Invalid base URL.")
    ffmpeg, ffprobe = shutil.which(args.ffmpeg), shutil.which(args.ffprobe)
    require(bool(ffmpeg and ffprobe), "ffmpeg and ffprobe are required for real media fixtures.")
    guest, listener, admin, kite, kore, poller = (HTTP.Session(args.base_url, args.timeout) for _ in range(6))
    runner = HTTP.Runner()
    suffix = uuid4().hex[:12]
    state = {"genre": None, "auto_id": None, "explicit_id": None, "waiting_id": None,
             "auto_job": None, "explicit_job": None, "embedded": b"", "embedded_jpeg": b""}
    resources = {"songIds": [], "titles": [], "uploadIds": [], "filesCreated": [],
                 "existingSongsModified": [], "accountsCreated": []}
    measurements = {"observations": []}
    initial_files = media_files()
    fatal = None

    def check(test_id, name, action, critical=False):
        ok = runner.check(test_id, name, action)
        if critical and not ok:
            raise RuntimeError(f"Dependent checks stopped because {test_id} failed.")

    def run_tool(arguments):
        result = subprocess.run(arguments, capture_output=True, timeout=90)
        require(result.returncode == 0, "Fixture/media tool failed: " + result.stderr.decode("utf-8", errors="replace")[:400])
        return result.stdout

    def probe_image(payload, extension):
        with tempfile.TemporaryDirectory(prefix="SoulSignalPictureQA-") as temporary:
            path = Path(temporary) / ("cover" + extension)
            path.write_bytes(payload)
            result = json.loads(run_tool([ffprobe, "-v", "error", "-select_streams", "v:0",
                "-show_entries", "stream=codec_name,width,height", "-of", "json", str(path)]))
            require(bool(result.get("streams")), "Returned cover has no decodable image stream.")
            return result["streams"][0]

    def dashboard():
        response = admin.request("/Admin")
        status_is(response, 200)
        return response

    def start(session):
        path = "/Admin/Import" if session is admin else "/Distributor/Upload"
        response = session.request("/UploadProgress/Start", "POST", {
            "__RequestVerificationToken": session.token(path),
        }, headers={"X-Requested-With": "XMLHttpRequest", "Accept": "application/json"}, follow=False)
        status_is(response, 200)
        upload_id = json_body(response).get("uploadId", "")
        require(bool(re.fullmatch(r"[0-9a-f]{32}", upload_id, re.I)), f"Invalid upload ID: {upload_id!r}.")
        resources["uploadIds"].append(upload_id)
        return upload_id

    def status(session, upload_id):
        response = session.request(f"/UploadProgress/Status/{upload_id}", follow=False)
        status_is(response, 200)
        value = json_body(response)
        require(value.get("phase") in PHASES, f"Unknown phase: {value!r}.")
        percent = value.get("percent")
        require(percent is None or (isinstance(percent, int) and not isinstance(percent, bool) and 0 <= percent <= 100),
                f"Percent out of bounds: {value!r}.")
        require(isinstance(value.get("message"), str) and bool(value["message"].strip()), "Missing progress message.")
        if value["phase"] == "encoding":
            require(percent is not None and percent <= 99, "Encoding must report a real percentage below completion.")
        return value

    def fields(session, upload_id, mode, overrides=None):
        value = {
            "Title": f"Progress Cover QA {mode} {suffix}", "Artist": "Soul Signal Studio", "GenreId": str(state["genre"]),
            "RightsHolder": "Soul Signal Studio — QA", "RightsEvidence": "Original synthetic music and generated image for local upload progress QA.",
            "AcceptRights": "true", "ExternalRecordingCode": f"PROGRESS-QA-{mode}-{suffix}",
            "UploadId": upload_id, "__RequestVerificationToken": session.token("/Distributor/Upload"),
        }
        value.update(overrides or {})
        return value

    def success(response, mode):
        status_is(response, 200)
        result = json_body(response)
        match = re.fullmatch(r"/Distributor/Details/(\d+)", urlparse(result.get("redirectUrl", "")).path, re.I)
        require(match is not None, f"Missing local success redirect: {result!r}.")
        song_id = int(match.group(1))
        resources["songIds"].append(song_id)
        resources["titles"].append(f"Progress Cover QA {mode} {suffix}")
        return song_id

    def cover_response(song_id):
        response = kite.request(f"/Distributor/Details/{song_id}")
        status_is(response, 200)
        urls = re.findall(r'<img\b[^>]*\bsrc="([^"]+)"', response.text, re.I)
        path = next((url for url in urls if urlparse(url).path.lower().startswith("/songs/cover/")), None)
        require(bool(path), "Submission did not receive a saved automatic/explicit cover.")
        image = kite.request(path)
        status_is(image, 200)
        require(image.headers.get("content-type", "").split(";")[0] in ("image/jpeg", "image/png"), "Cover MIME is not JPEG/PNG.")
        require(len(image.body) <= 2 * 1024 * 1024, "Extracted cover exceeds the allowed limit.")
        return path, image

    def rejected_upload(session, upload_id, mode, payload, overrides=None):
        before_ids, before_files = all_song_ids(dashboard()), media_files()
        response = multipart(session, "/Distributor/Upload", fields(session, upload_id, mode, overrides), {
            "FlacFile": ("original.flac", "audio/flac", payload),
        })
        status_is(response, 400)
        result = json_body(response)
        require(isinstance(result.get("errors"), list) and bool(result["errors"]), "Validation response has no error list.")
        require(all_song_ids(dashboard()) == before_ids, "Rejected request created a song.")
        require(media_files() == before_files, "Rejected request left new Media files.")
        return result

    print(f"Soul Signal — progress and embedded cover — {args.base_url}; QA {suffix}")
    try:
        check("PROGRESS-00", "Application responds", lambda: status_is(guest.request("/"), 200), critical=True)
        def logins():
            for session, email, area in (
                (listener, "listener@soulsignal.local", "Home"),
                (admin, "admin@soulsignal.local", "Admin"),
                (kite, "distrokite@soulsignal.local", "Distributor"),
                (kore, "tunekore@soulsignal.local", "Distributor"),
                (poller, "distrokite@soulsignal.local", "Distributor"),
            ):
                HTTP.login(session, email, area)
            state["genre"] = next(value for value in HELPERS.form_options(kite.request("/Distributor/Upload"), "GenreId") if value > 0)
            return "Existing demo accounts signed in; polling uses an independent session of the same owner."
        check("PROGRESS-01", "Authenticate owners and other roles", logins, critical=True)
        def auth_gate():
            response = guest.request("/UploadProgress/Start", "POST", {}, follow=False)
            require(response.status in (401, 302, 303), f"Guest start unexpectedly allowed: {response.status}.")
            response = listener.request("/UploadProgress/Start", "POST", {
                "__RequestVerificationToken": listener.token("/"),
            }, follow=False)
            require(response.status in (403, 302, 303), f"Listener start unexpectedly allowed: {response.status}.")
            return "Guest and listener cannot create source/admin processing jobs."
        check("PROGRESS-02", "Progress creation requires an authorized role", auth_gate)
        check("PROGRESS-03", "Progress creation requires CSRF", lambda: status_is(kite.request(
            "/UploadProgress/Start", "POST", {}, headers={"X-Requested-With": "XMLHttpRequest"}, follow=False), 400))
        def owner_job():
            state["waiting_id"] = start(kite)
            value = status(kite, state["waiting_id"])
            require(value["phase"] == "waiting", f"New job is not waiting: {value!r}.")
            status(poller, state["waiting_id"])
            return json.dumps(value, ensure_ascii=False)
        check("PROGRESS-04", "Owner starts and reads a waiting job", owner_job, critical=True)
        def owner_isolation():
            for session in (kore, admin):
                status_is(session.request(f"/UploadProgress/Status/{state['waiting_id']}", follow=False), 404)
            status_is(kite.request("/UploadProgress/Status/" + uuid4().hex, follow=False), 404)
            return "Other source/admin sessions cannot read another uploader's job; unknown ID returns404."
        check("PROGRESS-05", "Job status is private to its uploader", owner_isolation)
        def fixtures():
            with tempfile.TemporaryDirectory(prefix="SoulSignalEmbeddedQA-") as temporary:
                directory = Path(temporary)
                jpeg, flac = directory / "embedded.jpg", directory / "with-cover.flac"
                run_tool([ffmpeg, "-v", "error", "-nostdin", "-f", "lavfi", "-i", "color=c=0xc84c31:s=320x240:d=0.1",
                          "-frames:v", "1", "-q:v", "2", "-update", "1", str(jpeg)])
                run_tool([ffmpeg, "-v", "error", "-nostdin", "-stream_loop", "-1", "-i", str(ROOT / "Media/Audio/saigon-dawn.flac"),
                          "-i", str(jpeg), "-map", "0:a:0", "-map", "1:v:0", "-t", "180", "-c:a", "flac", "-compression_level", "5",
                          "-c:v", "copy", "-disposition:v:0", "attached_pic", "-metadata:s:v:0", "title=Front cover",
                          "-metadata:s:v:0", "comment=Cover (front)", str(flac)])
                state["embedded"], state["embedded_jpeg"] = flac.read_bytes(), jpeg.read_bytes()
                require(len(state["embedded"]) < 50 * 1024 * 1024, "Fixture exceeds upload limit.")
            measurements["fixture"] = {"durationSeconds": 180, "bytes": len(state["embedded"]),
                "embeddedArtwork": probe_image(state["embedded_jpeg"], ".jpg")}
            return json.dumps(measurements["fixture"])
        check("PROGRESS-06", "Generate a real FLAC with attached front artwork", fixtures, critical=True)
        def invalid_model():
            upload_id = start(kite)
            result = rejected_upload(kite, upload_id, "invalid-model", state["embedded"], {"Title": ""})
            value = status(kite, upload_id)
            require(value["phase"] == "failed", f"Validation failure did not terminate progress: {value!r}.")
            return json.dumps(result, ensure_ascii=False)
        check("PROGRESS-07", "AJAX validation errors terminate their job without files", invalid_model)
        def malformed():
            upload_id = start(kite)
            rejected_upload(kite, upload_id, "bad-flac", b"fLaC" + b"\x00" * 256)
            value = status(kite, upload_id)
            require(value["phase"] == "failed", f"Malformed FLAC did not mark failed: {value!r}.")
            return json.dumps(value, ensure_ascii=False)
        check("PROGRESS-08", "Bad media fails visibly and leaves no orphan files", malformed)
        def wrong_owner():
            rejected_upload(kore, state["waiting_id"], "wrong-owner", state["embedded"])
            require(status(kite, state["waiting_id"])["phase"] == "waiting", "Wrong owner modified the original owner's progress.")
            return "Foreign job ID cannot authorize a submission or mutate its owner's waiting status."
        check("PROGRESS-09", "A source cannot submit with another source's upload ID", wrong_owner)
        def live_upload():
            upload_id = state["auto_job"] = start(kite)
            form = fields(kite, upload_id, "embedded")
            observations = []
            with ThreadPoolExecutor(max_workers=1) as executor:
                future = executor.submit(multipart, kite, "/Distributor/Upload", form, {
                    "FlacFile": ("with-cover.flac", "audio/flac", state["embedded"]),
                })
                deadline = time.monotonic() + args.timeout + 5
                while not future.done() and time.monotonic() < deadline:
                    observations.append(status(poller, upload_id))
                    time.sleep(0.08)
                response = future.result(timeout=2)
            state["auto_id"] = success(response, "embedded")
            observations.append(status(poller, upload_id))
            encoding = [value["percent"] for value in observations if value["phase"] == "encoding"]
            require(encoding == sorted(encoding), f"Encoding progress moved backwards: {encoding!r}.")
            require(observations[-1]["phase"] == "completed", f"Successful upload not completed: {observations[-1]!r}.")
            measurements["observations"] = observations
            measurements["observedEncodingPercents"] = encoding
            return f"CreatedID={state['auto_id']}; observed phases={sorted({item['phase'] for item in observations})}; encode%={encoding}."
        check("PROGRESS-10", "Poll actual conversion while a real FLAC is submitted", live_upload, critical=True)
        def automatic_cover():
            path, image = cover_response(state["auto_id"])
            extension = ".png" if image.headers.get("content-type", "").startswith("image/png") else ".jpg"
            dimensions = probe_image(image.body, extension)
            require(dimensions["width"] == 320 and dimensions["height"] == 240, f"Embedded artwork was not used: {dimensions!r}.")
            measurements["automaticCover"] = {"path": path, "bytes": len(image.body), **dimensions}
            return json.dumps(measurements["automaticCover"])
        check("PROGRESS-11", "Embedded FLAC picture becomes the song cover", automatic_cover)
        def duplicate_job():
            before_ids, before_files = all_song_ids(dashboard()), media_files()
            result = multipart(kite, "/Distributor/Upload", fields(kite, state["auto_job"], "reused-job"), {
                "FlacFile": ("with-cover.flac", "audio/flac", state["embedded"]),
            })
            status_is(result, 400)
            require(all_song_ids(dashboard()) == before_ids and media_files() == before_files, "Reused job created new data.")
            require(status(kite, state["auto_job"])["phase"] == "completed", "Replayed request changed a completed job.")
            return "A used upload ID cannot create another song or overwrite completed state."
        check("PROGRESS-12", "Processing IDs cannot be reused", duplicate_job)
        def explicit_cover():
            upload_id = state["explicit_job"] = start(kite)
            pixel = HELPERS.png_pixel()
            response = multipart(kite, "/Distributor/Upload", fields(kite, upload_id, "explicit"), {
                "FlacFile": ("with-cover.flac", "audio/flac", state["embedded"]),
                "CoverFile": ("explicit.png", "image/png", pixel),
            })
            state["explicit_id"] = success(response, "explicit")
            path, image = cover_response(state["explicit_id"])
            dimensions = probe_image(image.body, ".png")
            require(dimensions["width"] == 1 and dimensions["height"] == 1, "Explicit image did not override embedded image.")
            require(hashlib.sha256(image.body).digest() == hashlib.sha256(pixel).digest(), "Explicit PNG content unexpectedly changed.")
            require(status(kite, upload_id)["phase"] == "completed", "Explicit upload job is not completed.")
            measurements["explicitCover"] = {"path": path, "bytes": len(image.body), **dimensions}
            return json.dumps(measurements["explicitCover"])
        check("PROGRESS-13", "A separately chosen image takes priority over embedded art", explicit_cover, critical=True)
        def privacy():
            for song_id in (state["auto_id"], state["explicit_id"]):
                path = f"/Admin/ReviewSong/{song_id}"
                response = admin.request(path)
                status_is(response, 200)
                require(HELPERS.pending(response), "Successfully processed song bypassed Pending.")
                status_is(kore.request(f"/Distributor/Details/{song_id}"), 404)
                status_is(listener.request(f"/Songs/Stream/{song_id}"), 403)
                cover, _ = cover_response(song_id)
                status_is(kore.request(cover), 403)
                status_is(listener.request(cover), 403)
                HTTP.range_audio(admin, f"/Songs/Stream/{song_id}", "audio/mpeg")
            HTTP.catalog(guest, "/?" + urlencode({"q": suffix}), set())
            return "Both new submissions remain Pending; admin can preview; other source/listener cannot read private media."
        check("PROGRESS-14", "Progress and cover extraction preserve moderation/privacy", privacy)
        def fallback_cover():
            # Existing seed uses the default artwork; no extra record is created for this read-only check.
            response = kite.request("/Distributor/Details/1")
            status_is(response, 200)
            require("/images/covers/saigon-dawn.svg" in response.text, "Existing no-art default cover disappeared.")
            return "Existing no-art submission keeps its default cover; no third QA song is created."
        check("PROGRESS-15", "Existing submissions without embedded art keep their cover", fallback_cover)
        def unused_cover_cleanup():
            created_covers = {Path(value).name for value in media_files() - initial_files
                              if Path(value).parent.name == "Covers"}
            used_covers = {urlparse(measurements[key]["path"]).path.rsplit("/", 1)[-1]
                          for key in ("automaticCover", "explicitCover")}
            require(created_covers == used_covers,
                    f"Unused automatic cover was left behind: created={created_covers}, used={used_covers}.")
            return "Only the two chosen covers remain; embedded artwork superseded by an explicit image is cleaned up."
        check("PROGRESS-16", "Unused extracted artwork is cleaned up after an override", unused_cover_cleanup)
    except Exception as error:
        fatal = f"{type(error).__name__}: {error}"
        print("STOP " + fatal)
    finally:
        resources["filesCreated"] = sorted(media_files() - initial_files)
        passed = sum(item["status"] == "passed" for item in runner.results)
        report = {
            "model": "upload-progress-and-embedded-cover", "startedAt": runner.started_at, "completedAt": HTTP.now_iso(),
            "timezone": "Asia/Saigon (UTC+07:00)", "baseUrl": args.base_url,
            "summary": {"total": len(runner.results), "passed": passed, "failed": len(runner.results)-passed},
            "resourcesCreated": resources, "measurements": measurements, "fatalError": fatal,
            "limitations": [
                "Browser upload byte percentages and cancel/retry UI are verified separately; this script polls server processing state.",
                "Real encoding phases may be too short to observe every update; only observed encode percentages are checked for bounds/ordering.",
                "No missing-encoder, forced-timeout, client-disconnect or maximum-size tests are performed here.",
                "Exactly two QA song rows may be created and retained; existing rows/files are never modified or deleted.",
                "The no-art case is a read-only existing-record check, not another fresh upload.",
            ], "tests": runner.results,
        }
        path = args.report if args.report.is_absolute() else ROOT / args.report
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        print(f"Report: {path}")
        print(json.dumps(resources, ensure_ascii=False))
        print(f"Result: {passed} PASS, {len(runner.results)-passed} FAIL, total {len(runner.results)} checks.")
    return int(bool(fatal) or any(item["status"] != "passed" for item in runner.results))


if __name__ == "__main__":
    raise SystemExit(main())
