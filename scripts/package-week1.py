"""Package the source, SQL, original demo assets and report without build caches."""
from pathlib import Path
import os
import zipfile

root = Path(__file__).resolve().parents[1]
output = root / "artifacts" / "SoulSignal-Week1.zip"
output.parent.mkdir(exist_ok=True)
excluded = {".git", ".vs", ".idea", "bin", "obj", "artifacts", "publish", "logs", "__pycache__"}
demo_audio = {"saigon-dawn.mp3", "saigon-dawn.flac", "afterglow.mp3", "quiet-orbit.mp3"}

with zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for current, directories, files in os.walk(root):
        directories[:] = sorted(name for name in directories if name not in excluded)
        for name in sorted(files):
            path = Path(current) / name
            relative = path.relative_to(root)
            # Runtime uploads stay on the local demo machine; the source bundle uses original seed media.
            if relative.parts[:2] == ("Media", "Audio") and name not in demo_audio:
                continue
            if relative.parts[:2] == ("Media", "Covers"):
                continue
            if name.startswith(".env") or name.endswith((".user", ".suo", ".log", ".bak", ".mdf", ".ldf")):
                continue
            if name == "appsettings.Local.json" or name.endswith(".local.json"):
                continue
            archive.write(path, relative)

with zipfile.ZipFile(output) as archive:
    bad_file = archive.testzip()
    if bad_file:
        raise RuntimeError(f"Archive integrity check failed: {bad_file}")
    print(f"{output}\n{len(archive.namelist())} files; {output.stat().st_size:,} bytes; integrity OK")
