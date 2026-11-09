---
title: Output formats
nav_order: 6
---

# Output formats
{: .no_toc }

1. TOC
{:toc}

Choose the format with **Save as** on the Transcribe tab. The file is saved next to your recording as `<name>_transcript.<extension>`, in UTF-8, so accented and Arabic text stay readable.

## Which one to use

| Format | Extension | Best for |
| --- | --- | --- |
| Plain text | `.txt` | Reading, notes, pasting into documents |
| SubRip | `.srt` | Subtitles in most video players and editors |
| WebVTT | `.vtt` | Subtitles on the web (the HTML video element) |
| JSON | `.json` | Programs and scripts, or when you need exact times |

## Plain text (.txt)

One line per segment, with the start and end time.

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

Numbered subtitles with times in `hours:minutes:seconds,milliseconds`.

```
1
00:00:00,000 --> 00:00:02,100
Speaker 2: Yeah. And you worked on the original computer in this country, right?

2
00:00:02,100 --> 00:00:06,300
Speaker 1: I was very fortunate.
```

## WebVTT (.vtt)

Like SRT, but with a `WEBVTT` header, a dot before the milliseconds, and speakers as standard voice tags.

```
WEBVTT

00:00:00.000 --> 00:00:02.100
<v Speaker 2>Yeah. And you worked on the original computer in this country, right?

00:00:02.100 --> 00:00:06.300
<v Speaker 1>I was very fortunate.
```

## JSON (.json)

A list of segments with times in seconds. `speaker` is the label, or `null` when speakers were not identified.

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

- If a recording has no speaker labels, the format simply has no speaker text, and the JSON `speaker` field is `null`.
- Transcribing the same file again in the same format **overwrites** the earlier file.
- The on-screen list and **Copy all** always show the speaker label first when speakers were identified.
