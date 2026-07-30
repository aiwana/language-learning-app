# Video Import

Use this tool to add real video-bank lessons without mock data. It calls `yt-dlp`
for metadata and manual captions, then writes files in the format already
consumed by `LessonContentService`.

## Why No Admin UI Yet?

Start with this internal importer instead of an admin screen. The project does
not yet have admin roles, content moderation workflow, or storage quotas. A UI
would still need a trusted background worker behind it, so this script is the
smallest honest step.

Add an admin UI later when the app has:

- an `Admin` role or trusted staff account model;
- a review queue for `source_review_status`;
- clear license/source approval rules;
- background jobs instead of running yt-dlp inside a web request.

## Install

```powershell
python -m pip install -U yt-dlp
yt-dlp --version
```

Keep yt-dlp updated because video sites change often.

## Import A Video

```powershell
python tools/video-import/import_video.py "https://www.youtube.com/watch?v=VIDEO_ID" `
  --slug "my-real-lesson" `
  --lesson-id 123 `
  --license-note "Review source ownership before public use."
```

By default, the importer only accepts manual subtitles. If no manual captions
exist, it writes an empty transcript and reports that the video needs manual
work.

Use automatic captions only for drafts:

```powershell
python tools/video-import/import_video.py "https://www.youtube.com/watch?v=VIDEO_ID" `
  --slug "my-draft-lesson" `
  --allow-auto-captions
```

Drafts imported from automatic captions should stay `pending` or `rejected`
until a person fixes and reviews the transcript.

## Import The 30 Seeded Videos

```powershell
python tools/video-import/import_batch.py --continue-on-error
```

This reads `tools/video-import/videos.json` and creates transcript folders under
`WebShadowing/wwwroot/media/video-bank`.

The project seed `Designs/Database/Seed_video_bank_sources.sql` is the canonical
database script for the current curated list. Update that seed when new imported
transcripts should become part of the default database setup.

The script creates:

- `WebShadowing/wwwroot/media/video-bank/<slug>/transcript.json`
- `WebShadowing/wwwroot/media/video-bank/<slug>/source-metadata.json`
- `WebShadowing/wwwroot/media/video-bank/<slug>/import-report.json`

The video is not downloaded or rehosted. The database keeps the original URL for
playback through the existing YouTube/IFrame flow.

## Review Checklist

Before running the generated SQL in a shared database:

- confirm the video is allowed for classroom/app use;
- check that captions are accurate enough for learners;
- reject or manually fix videos with no manual captions;
- split or merge overly short caption cues if needed;
- add Vietnamese `translation` and IPA later if the lesson needs them;
- only change `source_review_status` to `approved` after review.

## Transcript Contract

Transcript files are UTF-8 JSON arrays:

```json
[
  {
    "sentence_order": 1,
    "start_time": 0.0,
    "end_time": 3.2,
    "text": "Example sentence.",
    "translation": null,
    "ipa": null
  }
]
```

This matches the loader in `WebShadowing/Services/LessonContentService.cs`.
