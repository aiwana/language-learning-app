#!/usr/bin/env python3
"""Run import_video.py for every source in videos.json."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
TOOL_DIR = Path(__file__).resolve().parent
DEFAULT_MANIFEST = TOOL_DIR / "videos.json"


def main() -> int:
    configure_console_encoding()

    parser = argparse.ArgumentParser(description="Import captions for all configured videos.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--lang", default="en,en-US,en-GB")
    parser.add_argument("--continue-on-error", action="store_true")
    parser.add_argument(
        "--allow-auto-captions",
        action="store_true",
        help="Pass through to import_video.py. Imported captions should remain pending review.",
    )
    parser.add_argument(
        "--only-missing",
        action="store_true",
        help="Skip videos that already have a non-empty transcript.json.",
    )
    args = parser.parse_args()

    videos = json.loads(args.manifest.read_text(encoding="utf-8"))
    failures: list[str] = []

    for index, video in enumerate(videos, start=1):
        transcript_path = ROOT / "WebShadowing" / "wwwroot" / "media" / "video-bank" / video["slug"] / "transcript.json"
        if args.only_missing and transcript_has_sentences(transcript_path):
            print(f"[{index}/{len(videos)}] Skipping {video['slug']} (transcript exists).")
            continue

        print(f"[{index}/{len(videos)}] Importing {video['slug']}...")
        command = [
            sys.executable,
            str(TOOL_DIR / "import_video.py"),
            video["url"],
            "--slug",
            video["slug"],
            "--title",
            video.get("title", video["slug"]),
            "--lang",
            args.lang,
        ]
        if args.allow_auto_captions:
            command.append("--allow-auto-captions")

        result = subprocess.run(command, cwd=ROOT, text=True)
        if result.returncode != 0:
            failures.append(video["slug"])
            if not args.continue_on_error:
                break
            continue

    if failures:
        print("Failed imports:", ", ".join(failures), file=sys.stderr)
        return 1

    return 0


def configure_console_encoding() -> None:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8")
            except OSError:
                pass


def transcript_has_sentences(path: Path) -> bool:
    if not path.exists():
        return False

    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return False

    return isinstance(payload, list) and len(payload) > 0


if __name__ == "__main__":
    raise SystemExit(main())
