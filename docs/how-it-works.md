---
title: How it works
nav_order: 9
---

# How it works
{: .no_toc }

1. TOC
{:toc}

## From recording to transcript

1. **Read.** MP3 and WAV files are decoded inside the app. Other formats, including video, go through ffmpeg (bundled in the app), which extracts a 16 kHz mono audio track.
2. **Find the speech.** A voice activity model ([Silero VAD](https://github.com/snakers4/silero-vad)) locates where people are talking. Silence and background noise are never sent to the speech model, which prevents invented text and keeps timestamps correct.
3. **Transcribe.** [Whisper](https://github.com/openai/whisper) runs on your computer on those stretches, through [Whisper.net](https://github.com/sandrohanea/whisper.net), producing lines with start and end times. Obvious Whisper hallucinations, such as the same phrase repeated, are filtered out.
4. **Label speakers (optional).** A [pyannote](https://github.com/pyannote/pyannote-audio) segmentation model finds speaker changes and a 3D-Speaker embedding model describes each voice, through [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx). Voices are clustered, and each line gets the speaker who talks for most of it. Speakers are then numbered by first appearance, and tiny fragments are folded into real speakers.
5. **Save.** The transcript is written next to the source file.

## How Practice analyses speech

The Practice tab uses the same transcript, then measures it:

1. The recording is transcribed with the **Small** model and a prompt that encourages the model to keep "um", "uh" and stutters.
2. **Pace** is words per minute over the time spent speaking.
3. **Fillers** are matched against lists for English, Dutch and Spanish.
4. **Pauses** are gaps of 0.8 seconds or more between lines.
5. **Stumbles** are repeated words and stuttered starts.
6. Rules turn the numbers into tips, and the result is compared with your previous run in the same language.

## The parts

| Part | Role |
| --- | --- |
| Whisper (ggml) | Speech to text |
| Silero VAD | Finds where speech is |
| pyannote segmentation and 3D-Speaker CAM++ | Speaker detection |
| ffmpeg | Reads video and other audio formats |
| .NET MAUI | The Windows and macOS app |
| Ollama (optional, yours) | Written feedback in Practice |

All of these are inside the app, except Ollama, which you install yourself if you want it.

## Pinned and verified

The models and tools that are bundled are pinned to exact versions and checked against a SHA-256 checksum when the app is built, so the files in a release are exactly the ones that were reviewed.

## Source code

Everything is open source: [github.com/Eomar21/transcriber](https://github.com/Eomar21/transcriber). The README describes how to build it and how it is tested.
