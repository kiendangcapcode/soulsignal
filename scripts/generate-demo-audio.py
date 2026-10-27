"""Rebuild Soul Signal's original synthetic demo recordings.

Run from any folder: python scripts/generate-demo-audio.py
Optional authoring requirements: Python 3, numpy, ffmpeg on PATH.
The ASP.NET application does not require Python. Its FLAC upload pipeline
requires ffmpeg and ffprobe to read duration and generate MP3 renditions.
"""

from __future__ import annotations

import shutil
import subprocess
import tempfile
import wave
from pathlib import Path

import numpy as np


ROOT = Path(__file__).resolve().parents[1]
AUDIO_DIR = ROOT / "Media" / "Audio"
RATE = 44_100

TRACKS = [
    {
        "slug": "saigon-dawn",
        "title": "Saigon Dawn",
        "seconds": 24,
        "seed": 104,
        "chords": [(50, 57, 61, 66), (47, 54, 57, 62), (43, 50, 54, 59), (45, 52, 57, 61)],
        "melody": [74, 78, 81, 78, 76, 73, 74, 69, 71, 74, 78, 74, 73, 69, 66, 69],
        "brightness": 0.34,
    },
    {
        "slug": "afterglow",
        "title": "Afterglow",
        "seconds": 28,
        "seed": 208,
        "chords": [(45, 52, 55, 60), (41, 48, 52, 57), (48, 55, 59, 64), (43, 50, 53, 59)],
        "melody": [76, 72, 69, 67, 69, 72, 76, 79, 77, 76, 72, 69, 71, 74, 72, 67],
        "brightness": 0.24,
    },
    {
        "slug": "quiet-orbit",
        "title": "Quiet Orbit",
        "seconds": 32,
        "seed": 312,
        "chords": [(43, 50, 54, 59), (40, 47, 50, 55), (48, 55, 59, 64), (45, 52, 55, 62)],
        "melody": [79, 78, 74, 71, 74, 78, 83, 81, 79, 76, 74, 71, 69, 74, 78, 74],
        "brightness": 0.18,
    },
]


def hz(midi: int) -> float:
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def add_stereo(buffer: np.ndarray, sound: np.ndarray, start: float, pan: float = 0.0) -> None:
    offset = int(start * RATE)
    length = min(len(sound), len(buffer) - offset)
    if length <= 0:
        return
    angle = (pan + 1.0) * np.pi / 4.0
    buffer[offset : offset + length, 0] += sound[:length] * np.cos(angle)
    buffer[offset : offset + length, 1] += sound[:length] * np.sin(angle)


def tone(midi: int, seconds: float, kind: str, brightness: float = 0.25) -> np.ndarray:
    t = np.arange(int(seconds * RATE), dtype=np.float64) / RATE
    frequency = hz(midi)
    if kind == "pad":
        signal = (
            np.sin(2 * np.pi * frequency * t)
            + 0.22 * np.sin(2 * np.pi * frequency * 1.0025 * t)
            + brightness * np.sin(2 * np.pi * 2 * frequency * t)
        )
        envelope = np.minimum(t / 0.45, 1.0) * np.minimum((seconds - t) / 0.85, 1.0)
    elif kind == "bass":
        signal = np.sin(2 * np.pi * frequency * t) + 0.16 * np.sin(2 * np.pi * 2 * frequency * t)
        envelope = (1 - np.exp(-t * 35)) * np.exp(-t * 2.1)
    else:
        signal = (
            np.sin(2 * np.pi * frequency * t)
            + brightness * np.sin(2 * np.pi * 2 * frequency * t)
            + 0.12 * np.sin(2 * np.pi * 3 * frequency * t)
        )
        envelope = (1 - np.exp(-t * 180)) * np.exp(-t * (2.8 if kind == "bell" else 5.0))
        envelope *= np.minimum((seconds - t) / 0.06, 1.0)
    return signal * np.maximum(envelope, 0)


def render(track: dict) -> np.ndarray:
    seconds = track["seconds"]
    beat = seconds / 32.0
    bar = beat * 4
    rng = np.random.default_rng(track["seed"])
    dry = np.zeros((seconds * RATE, 2), dtype=np.float64)

    for bar_index in range(8):
        chord = track["chords"][bar_index % 4]
        start = bar_index * bar
        for note_index, note in enumerate(chord):
            pad = tone(note + 12, bar + 0.65, "pad", track["brightness"]) * 0.027
            add_stereo(dry, pad, start, (note_index - 1.5) / 2.2)
        for half in range(2):
            bass = tone(chord[0] - 12, beat * 1.8, "bass") * 0.13
            add_stereo(dry, bass, start + half * beat * 2)
        for step in range(8):
            note = chord[(step + bar_index) % 4] + 24
            pluck = tone(note, beat * 1.45, "pluck", 0.12) * 0.061
            add_stereo(dry, pluck, start + step * beat / 2, -0.4 if step % 2 == 0 else 0.4)

    for index, note in enumerate(track["melody"]):
        start = index * beat * 2 + beat * 0.12
        bell = tone(note, beat * 2.6, "bell", track["brightness"]) * 0.105
        add_stereo(dry, bell, start, np.sin(index * 1.3) * 0.25)

    # Quiet, reproducible pulse under the melody, with no sampled recordings.
    for index in range(32):
        t = np.arange(int(0.20 * RATE)) / RATE
        kick = np.sin(2 * np.pi * (47 * t + 16 * (1 - np.exp(-t * 22)) / 22))
        kick *= np.exp(-t * 23) * np.minimum(t / 0.004, 1.0) * 0.065
        if index % 2 == 0:
            add_stereo(dry, kick, index * beat)
        noise = rng.normal(0, 1, int(RATE * 0.055))
        noise = np.concatenate(([0.0], np.diff(noise)))
        noise *= np.exp(-np.arange(len(noise)) / (RATE * 0.009)) * 0.010
        add_stereo(dry, noise, index * beat + beat * 0.5, 0.55 if index % 2 else -0.55)

    wet = dry.copy()
    for delay, gain in [(beat * 0.75, 0.16), (beat * 1.5, 0.10), (beat * 2.25, 0.055)]:
        offset = int(delay * RATE)
        wet[offset:] += dry[:-offset, ::-1] * gain
    t = np.arange(len(wet)) / RATE
    fade = np.minimum(t / 0.4, 1.0) * np.minimum((seconds - t) / 1.25, 1.0)
    wet *= fade[:, None]
    wet *= 0.83 / max(float(np.max(np.abs(wet))), 0.001)
    return np.round(np.clip(wet, -1, 1) * 32767).astype("<i2")


def main() -> None:
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("Install ffmpeg and place it on PATH before regenerating demo audio.")
    AUDIO_DIR.mkdir(parents=True, exist_ok=True)
    # Only a single, explicitly-created temporary WAV is removed at the end of each track.
    for track in TRACKS:
        audio = render(track)
        temp_handle = tempfile.NamedTemporaryFile(prefix="soulsignal-demo-", suffix=".wav", delete=False)
        wav_path = Path(temp_handle.name)
        temp_handle.close()
        try:
            with wave.open(str(wav_path), "wb") as wav:
                wav.setnchannels(2)
                wav.setsampwidth(2)
                wav.setframerate(RATE)
                wav.writeframes(audio.tobytes())
            base_args = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(wav_path)]
            subprocess.run(
                base_args + ["-codec:a", "libmp3lame", "-b:a", "128k", "-metadata", f"title={track['title']}",
                             "-metadata", "artist=Soul Signal Studio", str(AUDIO_DIR / f"{track['slug']}.mp3")],
                check=True,
            )
            if track["slug"] == "saigon-dawn":
                subprocess.run(
                    base_args + ["-codec:a", "flac", "-compression_level", "8", "-metadata", f"title={track['title']}",
                                 "-metadata", "artist=Soul Signal Studio", str(AUDIO_DIR / f"{track['slug']}.flac")],
                    check=True,
                )
        finally:
            wav_path.unlink(missing_ok=True)
        print(f"Created {track['slug']}: {track['seconds']} seconds, stereo 44.1 kHz.")


if __name__ == "__main__":
    main()
