---
title: Output formats
nav_order: 6
---

# Output formats
{: .no_toc }

1. TOC
{:toc}

Select the format with **Save as** on the Transcribe tab. The app saves the file in the folder of your recording. The name is `<name>_transcript.<extension>`. The file uses UTF-8 encoding, so accented and Arabic text stays readable.

## Choose a format

| Format | Extension | Use it for |
| --- | --- | --- |
| Plain text | `.txt` | Reading, notes and text in other documents |
| SubRip | `.srt` | Subtitles in most video players and editors |
| WebVTT | `.vtt` | Subtitles on web pages (the HTML video element) |
| JSON | `.json` | Programs and scripts, or exact times |

## Plain text (.txt)

The file has one line for each segment. Each line has a start time and an end time.

```
[00:00:00 -> 00:00:02] Yeah. And you worked on the original computer in this country, right?
[00:00:02 -> 00:00:06] I was very fortunate.
```

With speaker labels:

```
[00:00:00 -> 00:00:02] Speaker 2: Yeah. And you worked on the original computer in this country, right?
[00:00:02 -> 00:00:06] Speaker 1: I was very fortunate.
```

## SubRip (.srt)

The file has numbered subtitles. The times use the format `hours:minutes:seconds,milliseconds`.

```
1
00:00:00,000 --> 00:00:02,100
Speaker 2: Yeah. And you worked on the original computer in this country, right?

2
00:00:02,100 --> 00:00:06,300
Speaker 1: I was very fortunate.
```

## WebVTT (.vtt)

This format is like SRT, but it has these differences:

- The file starts with the header `WEBVTT`.
- The times use a dot before the milliseconds.
- Speakers use the standard voice tag.

```
WEBVTT

00:00:00.000 --> 00:00:02.100
<v Speaker 2>Yeah. And you worked on the original computer in this country, right?

00:00:02.100 --> 00:00:06.300
<v Speaker 1>I was very fortunate.
```

## JSON (.json)

The file has a list of segments. The times are in seconds. The `speaker` field has the label. If the app did not identify the speakers, the field is `null`.

```json
[
  {
    "start": 0,
    "end": 2.1,
    "speaker": "Speaker 2",
    "text": "Yeah. And you worked on the original computer in this country, right?"
  },
  {
    "start": 2.1,
    "end": 6.3,
    "speaker": "Speaker 1",
    "text": "I was very fortunate."
  }
]
```

## Notes

- If a recording has no speaker labels, the file has no speaker text. In JSON, the `speaker` field is `null`.
- If you transcribe the same file again in the same format, the app replaces the earlier file.
- The list on the screen and **Copy all** show the speaker label first when the app identified the speakers.
