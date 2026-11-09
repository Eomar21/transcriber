---
title: Supported formats
nav_order: 3.5
---

# Supported formats
{: .no_toc }

This page lists the audio files and video files that the app can read. It also lists the files that the app cannot read.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

## How the app reads a file

The app changes each file to a 16 kHz mono audio file before it transcribes the file. The app uses two readers:

- **Built-in reader.** It reads `.mp3` and `.wav` files. It is fast.
- **ffmpeg.** It reads all other formats. It also reads each `.mp3` or `.wav` file that the built-in reader cannot read.

ffmpeg is a part of the app. You do not install it. The app does not change your original file. The app deletes the converted copy after the work.

## Audio formats

| Format | Extensions |
| --- | --- |
| MP3 | `.mp3` |
| WAV | `.wav` |
| MPEG-4 audio | `.m4a`, `.m4b` |
| AAC | `.aac` |
| FLAC | `.flac` |
| Ogg Vorbis and Opus | `.ogg`, `.oga`, `.opus` |
| Windows Media Audio | `.wma` |
| Apple formats | `.aiff`, `.aif`, `.caf` |
| Mobile phone voice | `.amr`, `.3gp` |
| Dolby Digital | `.ac3` |
| Matroska audio | `.mka` |
| Other | `.mp2`, `.wv`, `.au` |

A `.wav` file can use many codecs. Examples are ADPCM, A-law, mu-law and surround sound. The app reads these files through ffmpeg.

## Video formats

The app takes the audio track from the video. The app ignores the picture.

| Format | Extensions |
| --- | --- |
| MPEG-4 video | `.mp4`, `.m4v` |
| QuickTime | `.mov` |
| Matroska | `.mkv` |
| WebM | `.webm` |
| AVI | `.avi` |
| Windows Media Video | `.wmv` |
| MPEG | `.mpg`, `.mpeg` |
| Flash video | `.flv` |

## Windows and macOS

The Windows app and the macOS app support the same formats. Each app has its own copy of ffmpeg.

On macOS, the file picker can hide a format that the system does not know, for example `.opus` or `.mka`. If this happens, drop the file on the window.

## Files that the app cannot read

| File | Reason |
| --- | --- |
| A file with copy protection (DRM), for example a purchased song or film | The app cannot decode a protected file. |
| A link to a web page or a streaming service | The app reads files on your computer only. |
| A file with a password or encryption | The app cannot open the file. |
| A video with no sound | The file has no audio track. |
| A damaged file | The file data is not complete. |
| A file that is not an audio file or a video file | There is no sound to read. |
| A `.ts` file | The app does not read this format, because the extension is also used for code files. |

If the app cannot read a file, the app shows a message for that file. Refer to [Troubleshooting]({{ '/troubleshooting' | relative_url }}#the-app-cannot-read-a-file).

## Output formats

The transcript formats are `.txt`, `.srt`, `.vtt` and `.json`. Refer to [Output formats]({{ '/output-formats' | relative_url }}).
