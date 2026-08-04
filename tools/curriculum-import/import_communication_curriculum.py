#!/usr/bin/env python3
"""Convert user-provided communication/work PDFs into project transcripts and SQL seed data."""

from __future__ import annotations

import argparse
import json
import re
import struct
import sys
from dataclasses import dataclass
from pathlib import Path

import pdfplumber


COURSES = {
    "giao-tiep/basic-level": {
        "title": "Real Easy English - Basic Communication",
        "level": "Beginner",
        "learning_mode": "casual",
        "description": "Easy-paced everyday English conversations for beginner listening and shadowing practice.",
    },
    "giao-tiep/intermediate-level": {
        "title": "6 Minute English - Intermediate Communication",
        "level": "Intermediate",
        "learning_mode": "casual",
        "description": "Topic-based discussions for intermediate listening, speaking, and vocabulary practice.",
    },
    "giao-tiep/feelings-and-emotions": {
        "title": "Feelings and Emotions",
        "level": "Intermediate",
        "learning_mode": "casual",
        "description": "Everyday conversations and expressions for talking about feelings and emotions.",
    },
    "giao-tiep/friends-and-family": {
        "title": "Friends and Family",
        "level": "Beginner",
        "learning_mode": "casual",
        "description": "Natural English for conversations about friends, family, and relationships.",
    },
    "giao-tiep/travel-and-transport": {
        "title": "Travel and Transport",
        "level": "Intermediate",
        "learning_mode": "casual",
        "description": "Practical listening and speaking topics for travel, places, and transport.",
    },
    "cong-viec/easy-level": {
        "title": "Workplace English - Easy",
        "level": "Beginner",
        "learning_mode": "professional",
        "description": "Easy English conversations about offices, jobs, and everyday workplace life.",
    },
    "cong-viec/job-applications": {
        "title": "Job Applications",
        "level": "Intermediate",
        "learning_mode": "professional",
        "description": "A practical series covering CVs, job descriptions, interviews, and job offers.",
    },
    "cong-viec/technology": {
        "title": "Technology and Digital Life",
        "level": "Intermediate",
        "learning_mode": "professional",
        "description": "Technology topics and useful digital vocabulary for modern work and life.",
    },
    "cong-viec/upper-intermediate-level": {
        "title": "Office English - Upper Intermediate",
        "level": "Advanced",
        "learning_mode": "professional",
        "description": "Advanced workplace communication for negotiation, deadlines, disagreement, and career growth.",
    },
}

IGNORED_LINE_PATTERNS = (
    re.compile(r"^BBC LEARNING ENGLISH$", re.IGNORECASE),
    re.compile(
        r"^(Real Easy English|6 Minute English|Office English|Job Applications|"
        r"The English We Speak|Beating Speaking Anxiety)$",
        re.IGNORECASE,
    ),
    re.compile(r"^This (?:is|is not).*transcript\.?$", re.IGNORECASE),
    re.compile(r"^bbclearningenglish\.com$", re.IGNORECASE),
    re.compile(r"^Page \d+ of \d+$", re.IGNORECASE),
    re.compile(r"^\d{4}$"),
)

SENTENCE_BOUNDARY = re.compile(r"(?<=[.!?])\s+(?=[A-Z0-9\"'])")
SPEAKER_LINE = re.compile(
    r"^(?:Dr |Professor |Prof |Sir |Dame )?[A-Z][A-Za-zÀ-ÖØ-öø-ÿ'’-]*"
    r"(?:\s+[A-Z][A-Za-zÀ-ÖØ-öø-ÿ'’-]*){0,4}$"
)


@dataclass(frozen=True)
class ImportedLesson:
    course_key: str
    title: str
    slug: str
    order: int
    duration: int
    audio_url: str
    transcript_url: str
    sentences: list[dict[str, object]]


def normalize_text(value: str) -> str:
    value = value.replace("\u00ad", "")
    value = value.replace("\u2018", "'").replace("\u2019", "'")
    value = value.replace("\u201c", '"').replace("\u201d", '"')
    value = value.replace("\u2013", "-").replace("\u2014", "-")
    value = re.sub(r"(?<=\w)\ufffd(?=\w)", "'", value)
    value = re.sub(r"\ufffd(?=\d)", "£", value)
    value = value.replace(" \ufffd ", " - ")
    value = re.sub(
        r"\s*(?:Real Easy English|6 Minute English|Office English|Job Applications|"
        r"The English We Speak|Beating Speaking Anxiety)?\s*"
        r"[©\ufffd]?British Broadcasting Corporation\s*\d{4}\s*",
        " ",
        value,
        flags=re.IGNORECASE,
    )
    value = re.sub(
        r"\s*(?:Real Easy English|6 Minute English|Office English|Job Applications|"
        r"The English We Speak|Beating Speaking Anxiety)\s*©\s*",
        " ",
        value,
        flags=re.IGNORECASE,
    )
    value = value.replace("\ufffd", "")
    value = re.sub(
        r"\s*bbclearningenglish\.com\s+Page\s+\d+\s+of\s+\d+\s*",
        " ",
        value,
        flags=re.IGNORECASE,
    )
    return re.sub(r"\s+", " ", value).strip()


def is_ignored_line(line: str, title: str) -> bool:
    if not line:
        return True
    if line.casefold() == title.casefold():
        return True
    if "British Broadcasting Corporation" in line:
        return True
    return any(pattern.match(line) for pattern in IGNORED_LINE_PATTERNS)


def looks_like_speaker(line: str) -> bool:
    if line.casefold() in {"both", "voicenote clips", "voice note clips"}:
        return True
    if len(line) > 42 or not SPEAKER_LINE.fullmatch(line):
        return False
    return line not in {
        "Vocabulary",
        "Worksheet",
        "Answers",
        "Question",
        "Summary",
    }


def extract_title(pdf_path: Path) -> str:
    with pdfplumber.open(pdf_path) as document:
        first_page_text = document.pages[0].extract_text() or ""
    lines = [normalize_text(line) for line in first_page_text.splitlines()]
    disclaimer_index = next(
        (
            index
            for index, line in enumerate(lines)
            if line.lower().startswith("this is") and "transcript" in line.lower()
        ),
        -1,
    )
    if disclaimer_index >= 0:
        title_lines = lines[:disclaimer_index]
        cleaned: list[str] = []
        series_names = {
            "bbc learning english",
            "real easy english",
            "6 minute english",
            "office english",
            "job applications",
            "the english we speak",
            "beating speaking anxiety",
        }
        for line in title_lines:
            candidate = re.sub(r"^BBC LEARNING ENGLISH\s*", "", line, flags=re.IGNORECASE).strip()
            for series_name in sorted(series_names, key=len, reverse=True):
                if candidate.casefold().startswith(series_name + " "):
                    candidate = candidate[len(series_name) :].strip()
                    break
            if candidate and candidate.casefold() not in series_names:
                cleaned.append(candidate)
        if cleaned:
            return normalize_text(" ".join(cleaned))
    return pdf_path.parent.name.replace("-", " ")


def extract_sentences(pdf_path: Path, title: str) -> list[dict[str, object]]:
    lines: list[str] = []
    with pdfplumber.open(pdf_path) as document:
        for page_number, page in enumerate(document.pages):
            page_lines = [
                normalize_text(line)
                for line in (page.extract_text(x_tolerance=2, y_tolerance=3) or "").splitlines()
            ]
            if page_number == 0:
                disclaimer_index = next(
                    (
                        index
                        for index, line in enumerate(page_lines)
                        if line.lower().startswith("this is") and "transcript" in line.lower()
                    ),
                    -1,
                )
                if disclaimer_index >= 0:
                    page_lines = page_lines[disclaimer_index + 1 :]
            filtered_lines = [line for line in page_lines if not is_ignored_line(line, title)]
            if page_number > 0 and page_number == len(document.pages) - 1:
                if any(line.casefold() == "vocabulary" for line in page_lines):
                    continue
                if not any(looks_like_speaker(line) for line in filtered_lines):
                    continue
            lines.extend(filtered_lines)

    turns: list[str] = []
    current_parts: list[str] = []
    for line in lines:
        if looks_like_speaker(line):
            if current_parts:
                turns.append(normalize_text(" ".join(current_parts)))
                current_parts = []
            continue
        current_parts.append(line)
    if current_parts:
        turns.append(normalize_text(" ".join(current_parts)))

    sentence_texts: list[str] = []
    for turn in turns:
        for sentence in SENTENCE_BOUNDARY.split(turn):
            sentence = normalize_text(sentence)
            if sentence and len(sentence) > 1:
                sentence_texts.append(sentence)

    return [
        {
            "sentence_order": index,
            "text": text,
            "translation": None,
            "ipa": None,
        }
        for index, text in enumerate(sentence_texts, start=1)
    ]


def synchsafe_to_int(raw: bytes) -> int:
    return (raw[0] << 21) | (raw[1] << 14) | (raw[2] << 7) | raw[3]


def mp3_duration_seconds(path: Path) -> int:
    data = path.read_bytes()
    offset = 10 + synchsafe_to_int(data[6:10]) if data[:3] == b"ID3" and len(data) >= 10 else 0
    bitrate_table = {
        1: [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0],
        2: [0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0],
    }
    sample_rates = {
        3: [44100, 48000, 32000],
        2: [22050, 24000, 16000],
        0: [11025, 12000, 8000],
    }

    while offset + 4 < len(data):
        header = struct.unpack(">I", data[offset : offset + 4])[0]
        if header & 0xFFE00000 != 0xFFE00000:
            offset += 1
            continue
        version_bits = (header >> 19) & 0b11
        layer_bits = (header >> 17) & 0b11
        bitrate_index = (header >> 12) & 0b1111
        sample_index = (header >> 10) & 0b11
        if version_bits == 1 or layer_bits != 1 or sample_index == 3 or bitrate_index in (0, 15):
            offset += 1
            continue

        is_mpeg1 = version_bits == 3
        sample_rate = sample_rates[version_bits][sample_index]
        bitrate = bitrate_table[1 if is_mpeg1 else 2][bitrate_index] * 1000
        channel_mode = (header >> 6) & 0b11
        has_crc = ((header >> 16) & 1) == 0
        side_info = 17 if is_mpeg1 and channel_mode == 3 else 32 if is_mpeg1 else 9 if channel_mode == 3 else 17
        xing_offset = offset + 4 + (2 if has_crc else 0) + side_info
        if data[xing_offset : xing_offset + 4] in (b"Xing", b"Info"):
            flags = struct.unpack(">I", data[xing_offset + 4 : xing_offset + 8])[0]
            if flags & 1:
                frames = struct.unpack(">I", data[xing_offset + 8 : xing_offset + 12])[0]
                samples_per_frame = 1152 if is_mpeg1 else 576
                return max(1, round(frames * samples_per_frame / sample_rate))
        return max(1, round((len(data) - offset) * 8 / bitrate))
    raise ValueError(f"No MPEG audio frame found in {path}")


def sql_string(value: str | None) -> str:
    if value is None:
        return "NULL"
    return "N'" + value.replace("'", "''") + "'"


def build_seed_sql(lessons: list[ImportedLesson]) -> str:
    lines = [
        "USE EnglishShadowingDB;",
        "GO",
        "",
        "SET XACT_ABORT ON;",
        "BEGIN TRANSACTION;",
        "",
        "-- Generated from user-provided BBC Learning English audio and transcript PDFs.",
        "-- Safe to run repeatedly: courses/lessons are updated and sentence rows are replaced.",
        "",
        "DECLARE @LessonId BIGINT;",
        "",
        "-- Remove duplicate rows produced by the legacy VARCHAR schema replacing Vietnamese letters with '?'.",
        "DELETE FROM Courses",
        "WHERE learning_mode = 'casual' AND course_type = 'curriculum'",
        "  AND title IN ('Real Easy English - Giao ti?p co b?n', '6 Minute English - Giao ti?p trung c?p')",
        "  AND EXISTS (",
        "      SELECT 1 FROM Lessons AS legacy_lesson",
        "      INNER JOIN Lesson_Material AS legacy_material",
        "          ON legacy_material.lesson_id = legacy_lesson.lesson_id",
        "      WHERE legacy_lesson.course_id = Courses.course_id",
        "        AND legacy_material.source_provider = 'BBC Learning English'",
        "  );",
        "",
    ]
    for course_index, (course_key, course) in enumerate(COURSES.items(), start=1):
        variable = f"@CourseId{course_index}"
        course_lessons = [lesson for lesson in lessons if lesson.course_key == course_key]
        if not course_lessons:
            continue
        lines.extend(
            [
                f"DECLARE {variable} BIGINT;",
                "SELECT " + variable + " = course_id FROM Courses",
                f"WHERE title = {sql_string(course['title'])}",
                f"  AND learning_mode = '{course['learning_mode']}' AND course_type = 'curriculum';",
                f"IF {variable} IS NULL",
                "BEGIN",
                "    INSERT INTO Courses (title, [description], level, learning_mode, course_type, created_at, updated_at)",
                f"    VALUES ({sql_string(course['title'])}, {sql_string(course['description'])}, "
                f"'{course['level']}', '{course['learning_mode']}', 'curriculum', "
                "SYSUTCDATETIME(), SYSUTCDATETIME());",
                f"    SET {variable} = SCOPE_IDENTITY();",
                "END",
                "ELSE",
                "BEGIN",
                "    UPDATE Courses",
                f"    SET [description] = {sql_string(course['description'])}, level = '{course['level']}',",
                "        updated_at = SYSUTCDATETIME()",
                f"    WHERE course_id = {variable};",
                "END;",
                "",
                "-- Free the managed lesson-order range before applying the current source ordering.",
                "UPDATE Lessons",
                "SET lesson_order = lesson_order + 10000",
                f"WHERE course_id = {variable}",
                "  AND lesson_order < 10000",
                "  AND EXISTS (",
                "      SELECT 1 FROM Lesson_Material AS managed_material",
                "      WHERE managed_material.lesson_id = Lessons.lesson_id",
                "        AND managed_material.source_provider = 'BBC Learning English'",
                "  );",
                "",
            ]
        )
        for lesson in course_lessons:
            lesson_var = "@LessonId"
            description = (
                f"BBC Learning English - {course['title'].split(' - ')[0]}. "
                "Listening and shadowing practice from the original conversation."
            )
            lines.extend(
                [
                    f"SET {lesson_var} = NULL;",
                    f"SELECT {lesson_var} = lesson_id FROM Lessons",
                    f"WHERE course_id = {variable} AND title = {sql_string(lesson.title)};",
                    f"IF {lesson_var} IS NULL",
                    "BEGIN",
                    "    INSERT INTO Lessons (course_id, title, [description], lesson_order, duration)",
                    f"    VALUES ({variable}, {sql_string(lesson.title)}, {sql_string(description)}, "
                    f"{lesson.order}, {lesson.duration});",
                    f"    SET {lesson_var} = SCOPE_IDENTITY();",
                    "END",
                    "ELSE",
                    "BEGIN",
                    "    UPDATE Lessons",
                    f"    SET [description] = {sql_string(description)}, lesson_order = {lesson.order}, "
                    f"duration = {lesson.duration}",
                    f"    WHERE lesson_id = {lesson_var};",
                    "END;",
                    "",
                    "DELETE FROM Lesson_Material",
                    f"WHERE lesson_id = {lesson_var} AND material_type IN ('audio', 'transcript');",
                    "INSERT INTO Lesson_Material",
                    "    (lesson_id, material_type, content_url, source_provider, source_id, license_note,",
                    "     source_review_status, source_reviewed_at)",
                    "VALUES",
                    f"    ({lesson_var}, 'audio', {sql_string(lesson.audio_url)}, 'BBC Learning English', "
                    f"{sql_string(lesson.slug)},",
                    "     N'User-provided educational media; verify distribution rights before publishing.',",
                    "     'pending', NULL),",
                    f"    ({lesson_var}, 'transcript', {sql_string(lesson.transcript_url)}, "
                    f"'BBC Learning English', {sql_string(lesson.slug)},",
                    "     N'Converted from the user-provided official transcript PDF.', 'pending', NULL);",
                    "",
                    f"DELETE FROM Lesson_Sentences WHERE lesson_id = {lesson_var};",
                    "INSERT INTO Lesson_Sentences",
                    "    (lesson_id, sentence_order, [text], translation, ipa, start_ms, end_ms)",
                    "VALUES",
                ]
            )
            value_rows = [
                f"    ({lesson_var}, {sentence['sentence_order']}, {sql_string(str(sentence['text']))}, "
                "NULL, NULL, NULL, NULL)"
                for sentence in lesson.sentences
            ]
            lines.append(",\n".join(value_rows) + ";")
            lines.append("")
    lines.extend(
        [
            "COMMIT TRANSACTION;",
            "GO",
            "",
            "SELECT c.learning_mode, c.title AS course_title, l.lesson_order, l.title AS lesson_title,",
            "       l.duration, COUNT(s.sentence_id) AS sentence_count",
            "FROM Courses AS c",
            "INNER JOIN Lessons AS l ON l.course_id = c.course_id",
            "LEFT JOIN Lesson_Sentences AS s ON s.lesson_id = l.lesson_id",
            "WHERE EXISTS (",
            "    SELECT 1 FROM Lesson_Material AS m",
            "    WHERE m.lesson_id = l.lesson_id AND m.source_provider = 'BBC Learning English'",
            ")",
            "  AND c.course_type = 'curriculum'",
            "GROUP BY c.course_id, c.learning_mode, c.title, l.lesson_order, l.title, l.duration",
            "ORDER BY c.learning_mode, c.title, l.lesson_order;",
            "GO",
            "",
        ]
    )
    return "\n".join(lines)


def import_curriculum(root: Path, seed_path: Path) -> list[ImportedLesson]:
    imported: list[ImportedLesson] = []
    for directory_key, course in COURSES.items():
        course_root = root
        for part in directory_key.split("/"):
            course_root = next(
                (item for item in course_root.iterdir() if item.is_dir() and item.name.casefold() == part),
                None,
            )
            if course_root is None:
                break
        if course_root is None:
            print(f"SKIP missing course directory: {root / directory_key}", file=sys.stderr)
            continue

        lesson_directories = sorted(
            (item for item in course_root.iterdir() if item.is_dir()),
            key=lambda item: (
                next(iter(sorted(item.glob("*.pdf"))), item).name[:6],
                item.name.casefold(),
            ),
        )
        course_order = 0
        for lesson_directory in lesson_directories:
            pdf_files = sorted(lesson_directory.glob("*.pdf"))
            audio_files = sorted(
                path
                for path in lesson_directory.iterdir()
                if path.suffix.casefold() in {".mp3", ".wav", ".m4a", ".ogg", ".webm"}
            )
            if len(pdf_files) != 1 or len(audio_files) != 1:
                print(
                    "SKIP "
                    f"{lesson_directory}: expected one PDF and one audio file, "
                    f"found {len(pdf_files)} PDF(s) and {len(audio_files)} audio file(s)",
                    file=sys.stderr,
                )
                continue
            if "worksheet" in pdf_files[0].name.casefold():
                print(f"SKIP {pdf_files[0]}: worksheet is not a transcript", file=sys.stderr)
                continue

            title = extract_title(pdf_files[0])
            sentences = extract_sentences(pdf_files[0], title)
            if not sentences:
                raise ValueError(f"No transcript sentences extracted from {pdf_files[0]}")

            transcript_path = lesson_directory / "transcript.json"
            transcript_path.write_text(
                json.dumps(sentences, ensure_ascii=False, indent=2) + "\n",
                encoding="utf-8",
            )

            course_order += 1
            imported.append(
                ImportedLesson(
                    course_key=directory_key,
                    title=title,
                    slug=lesson_directory.name,
                    order=course_order,
                    duration=mp3_duration_seconds(audio_files[0])
                    if audio_files[0].suffix.casefold() == ".mp3"
                    else 0,
                    audio_url="/" + (Path("media") / "curriculum" / directory_key
                                     / lesson_directory.name / audio_files[0].name).as_posix(),
                    transcript_url="/" + (Path("media") / "curriculum" / directory_key
                                          / lesson_directory.name / "transcript.json").as_posix(),
                    sentences=sentences,
                )
            )

    seed_path.parent.mkdir(parents=True, exist_ok=True)
    seed_path.write_text(build_seed_sql(imported), encoding="utf-8-sig")
    return imported


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--root",
        type=Path,
        default=Path("WebShadowing/wwwroot/media/curriculum"),
    )
    parser.add_argument(
        "--seed",
        type=Path,
        default=Path("Designs/Database/Seed_communication_work_curriculum.sql"),
    )
    args = parser.parse_args()
    lessons = import_curriculum(args.root.resolve(), args.seed.resolve())
    for lesson in lessons:
        print(
            f"{COURSES[lesson.course_key]['title']} | {lesson.order:02d} | {lesson.title} | "
            f"{lesson.duration}s | {len(lesson.sentences)} sentences"
        )
    print(f"Generated SQL seed: {args.seed}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
