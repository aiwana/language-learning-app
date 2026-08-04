#!/usr/bin/env python3
"""Import a real video source into the WebShadowing lesson-material format.

The script does not download/rehost video. It uses yt-dlp to fetch metadata and
captions, then writes a transcript JSON file that the app can load from
wwwroot/media/video-bank/<slug>/transcript.json.
"""

from __future__ import annotations

import argparse
import html
import json
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from urllib.parse import urlparse, parse_qs


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_MEDIA_ROOT = ROOT / "WebShadowing" / "wwwroot" / "media" / "video-bank"


def main() -> int:
    configure_console_encoding()

    parser = argparse.ArgumentParser(
        description="Create WebShadowing video-bank material from a real video URL."
    )
    parser.add_argument("url", help="Source video URL, for example a YouTube URL.")
    parser.add_argument("--slug", required=True, help="Folder slug under media/video-bank.")
    parser.add_argument(
        "--lesson-id",
        type=int,
        help="Accepted for older commands. Database seeding now lives in Seed_video_bank_sources.sql.",
    )
    parser.add_argument("--title", help="Lesson title for the generated SQL comment.")
    parser.add_argument(
        "--lang",
        default="en,en-US,en-GB",
        help="Comma-separated caption language codes, default: en,en-US,en-GB.",
    )
    parser.add_argument(
        "--allow-auto-captions",
        action="store_true",
        help="Allow YouTube automatic captions when manual subtitles are not available.",
    )
    parser.add_argument(
        "--media-root",
        type=Path,
        default=DEFAULT_MEDIA_ROOT,
        help="Output media root. Default: WebShadowing/wwwroot/media/video-bank.",
    )
    parser.add_argument(
        "--review-status",
        default="pending",
        choices=("pending", "approved", "rejected"),
        help="Accepted for older commands. Database seeding now lives in Seed_video_bank_sources.sql.",
    )
    parser.add_argument(
        "--license-note",
        default="Needs source/license review before public use.",
        help="Accepted for older commands. Database seeding now lives in Seed_video_bank_sources.sql.",
    )
    args = parser.parse_args()

    ytdlp = resolve_ytdlp_command()
    if not ytdlp:
        print(
            "yt-dlp was not found. Install it first, then rerun this script:\n"
            "  python -m pip install -U yt-dlp",
            file=sys.stderr,
        )
        return 2

    out_dir = args.media_root / args.slug
    out_dir.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="webshadowing-ytdlp-") as temp_name:
        temp_dir = Path(temp_name)
        try:
            metadata = read_metadata(ytdlp, args.url)
        except subprocess.CalledProcessError as exc:
            print(exc.stderr.strip() or str(exc), file=sys.stderr)
            return 1

        caption_file = download_caption(
            ytdlp,
            args.url,
            args.lang,
            temp_dir,
            allow_auto_captions=args.allow_auto_captions,
        )

        transcript = parse_vtt(caption_file) if caption_file else []
        quality_report = build_quality_report(transcript, caption_file, args.allow_auto_captions)
        transcript_path = out_dir / "transcript.json"
        transcript_path.write_text(
            json.dumps(transcript, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

        metadata_path = out_dir / "source-metadata.json"
        metadata_path.write_text(
            json.dumps(normalize_metadata(metadata, args.url), ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

        report_path = out_dir / "import-report.json"
        report_path.write_text(
            json.dumps(quality_report, ensure_ascii=False, indent=2) + "\n",
            encoding="utf-8",
        )

    print(f"Wrote transcript: {transcript_path}")
    print(f"Wrote metadata:   {metadata_path}")
    print(f"Wrote report:     {report_path}")
    if not transcript:
        print(
            "No manual captions were found. Use --allow-auto-captions only for draft imports, "
            "or fill transcript.json manually before approving this material.",
            file=sys.stderr,
        )
    elif quality_report["status"] != "ok":
        print(
            f"Caption quality needs review: {', '.join(quality_report['warnings'])}",
            file=sys.stderr,
        )
    return 0


def configure_console_encoding() -> None:
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            try:
                stream.reconfigure(encoding="utf-8")
            except OSError:
                pass


def resolve_ytdlp_command() -> list[str] | None:
    executable = shutil.which("yt-dlp")
    if executable:
        return [executable]

    for candidate in (
        Path.home() / "AppData" / "Roaming" / "Python" / f"Python{sys.version_info.major}{sys.version_info.minor}" / "Scripts" / "yt-dlp.exe",
        Path.home() / ".local" / "bin" / "yt-dlp",
    ):
        try:
            if candidate.exists():
                return [str(candidate)]
        except OSError:
            pass

    module_check = subprocess.run(
        [sys.executable, "-m", "yt_dlp", "--version"],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if module_check.returncode == 0:
        return [sys.executable, "-m", "yt_dlp"]

    return None


def run_ytdlp(ytdlp: list[str], command: list[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        [*ytdlp, *command],
        check=True,
        text=True,
        encoding="utf-8",
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )


def read_metadata(ytdlp: list[str], url: str) -> dict:
    result = run_ytdlp(ytdlp, ["-J", "--skip-download", url])
    return json.loads(result.stdout)


def download_caption(
    ytdlp: list[str],
    url: str,
    lang: str,
    temp_dir: Path,
    *,
    allow_auto_captions: bool,
) -> Path | None:
    command = [
        "--skip-download",
        "--write-subs",
        "--sub-langs",
        lang,
        "--sub-format",
        "vtt",
        "--paths",
        str(temp_dir),
        "-o",
        "%(id)s.%(ext)s",
        url,
    ]
    if allow_auto_captions:
        command.insert(2, "--write-auto-subs")

    try:
        run_ytdlp(ytdlp, command)
    except subprocess.CalledProcessError as exc:
        print(exc.stderr.strip(), file=sys.stderr)

    caption_files = sorted(temp_dir.glob("*.vtt"))
    return caption_files[0] if caption_files else None


def parse_vtt(path: Path) -> list[dict]:
    content = path.read_text(encoding="utf-8-sig")
    cues: list[dict] = []
    pending_times: tuple[float, float] | None = None
    pending_lines: list[str] = []

    for raw_line in [*content.splitlines(), ""]:
        line = raw_line.strip()
        if not line or line.startswith(("WEBVTT", "Kind:", "Language:", "NOTE")):
            flush_cue(cues, pending_times, pending_lines)
            pending_times = None
            pending_lines = []
            continue

        if "-->" in line:
            flush_cue(cues, pending_times, pending_lines)
            pending_times = parse_times(line)
            pending_lines = []
            continue

        if pending_times is not None and not line.isdigit():
            pending_lines.append(line)

    return merge_duplicate_cues(cues)


def flush_cue(
    cues: list[dict],
    pending_times: tuple[float, float] | None,
    pending_lines: list[str],
) -> None:
    if pending_times is None or not pending_lines:
        return

    text = clean_caption_text(" ".join(pending_lines))
    if not text:
        return

    cues.append(
        {
            "sentence_order": len(cues) + 1,
            "start_time": pending_times[0],
            "end_time": pending_times[1],
            "text": text,
            "translation": None,
            "ipa": None,
        }
    )


def parse_times(line: str) -> tuple[float, float]:
    left, right = line.split("-->", 1)
    return parse_timestamp(left.strip()), parse_timestamp(right.split()[0].strip())


def parse_timestamp(value: str) -> float:
    parts = value.replace(",", ".").split(":")
    seconds = float(parts[-1])
    minutes = int(parts[-2]) if len(parts) >= 2 else 0
    hours = int(parts[-3]) if len(parts) >= 3 else 0
    return round(hours * 3600 + minutes * 60 + seconds, 3)


def clean_caption_text(value: str) -> str:
    value = re.sub(r"<[^>]+>", "", value)
    value = re.sub(r"\{\\.*?\}", "", value)
    value = html.unescape(value)
    value = re.sub(r"\s+", " ", value).strip()
    return value


def merge_duplicate_cues(cues: list[dict]) -> list[dict]:
    merged: list[dict] = []
    for cue in cues:
        if merged and merged[-1]["text"] == cue["text"]:
            merged[-1]["end_time"] = cue["end_time"]
            continue
        cue["sentence_order"] = len(merged) + 1
        merged.append(cue)
    return merged


def build_quality_report(
    cues: list[dict],
    caption_file: Path | None,
    allow_auto_captions: bool,
) -> dict:
    durations = [
        cue["end_time"] - cue["start_time"]
        for cue in cues
        if cue.get("end_time") is not None and cue.get("start_time") is not None
    ]
    tiny_cues = sum(1 for duration in durations if duration < 0.35)
    duplicate_prefixes = sum(
        1
        for previous, current in zip(cues, cues[1:])
        if current["text"].startswith(previous["text"])
        or previous["text"].startswith(current["text"])
    )

    warnings: list[str] = []
    if caption_file is None:
        warnings.append("no_manual_caption_file")
    if allow_auto_captions:
        warnings.append("auto_captions_allowed")
    if tiny_cues:
        warnings.append(f"tiny_cues={tiny_cues}")
    if duplicate_prefixes:
        warnings.append(f"rolling_or_duplicate_cues={duplicate_prefixes}")
    if len(cues) > 600:
        warnings.append(f"high_cue_count={len(cues)}")

    return {
        "status": "needs_review" if warnings else "ok",
        "caption_file": caption_file.name if caption_file else None,
        "auto_captions_allowed": allow_auto_captions,
        "cue_count": len(cues),
        "tiny_cue_count": tiny_cues,
        "duplicate_prefix_count": duplicate_prefixes,
        "warnings": warnings,
    }


def normalize_metadata(metadata: dict, url: str) -> dict:
    return {
        "source_url": url,
        "source_provider": source_provider(url),
        "source_id": metadata.get("id") or source_id_from_url(url),
        "title": metadata.get("title"),
        "duration": metadata.get("duration"),
        "uploader": metadata.get("uploader"),
        "channel": metadata.get("channel"),
        "webpage_url": metadata.get("webpage_url") or url,
        "license": metadata.get("license"),
    }


def source_provider(url: str) -> str:
    host = urlparse(url).hostname or ""
    if "youtube" in host or "youtu.be" in host:
        return "youtube"
    return host.lower()[:100] or "unknown"


def source_id_from_url(url: str) -> str:
    parsed = urlparse(url)
    if "youtu.be" in (parsed.hostname or ""):
        return parsed.path.strip("/")
    query_id = parse_qs(parsed.query).get("v")
    if query_id:
        return query_id[0]
    return parsed.path.rstrip("/").split("/")[-1]


if __name__ == "__main__":
    raise SystemExit(main())
