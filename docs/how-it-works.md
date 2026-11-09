---
title: How it works
nav_order: 9
---

# How it works
{: .no_toc }

1. TOC
{:toc}

## From recording to transcript

1. **Read.** The app decodes MP3 and WAV files itself. For other formats, including video, the app uses ffmpeg. ffmpeg is in the app. It takes a 16 kHz mono audio track from the file.
2. **Find the speech.** A voice activity model ([Silero VAD](https://github.com/snakers4/silero-vad)) finds the parts where people speak. The app does not send silence or background noise to the speech model. This prevents invented text and keeps the time stamps correct.
3. **Transcribe.** [Whisper](https://github.com/openai/whisper) runs on your computer, through [Whisper.net](https://github.com/sandrohanea/whisper.net). It makes lines of text with a start time and an end time. The app removes clear Whisper errors, for example a phrase that repeats.
4. **Label the speakers (optional).** A [pyannote](https://github.com/pyannote/pyannote-audio) segmentation model finds the points where the speaker changes. A 3D-Speaker embedding model describes each voice. Both models run through [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx). The app puts the voices in groups. Each line gets the speaker who talks the longest in that line. Then the app numbers the speakers in the order of their first speech. It joins short fragments to the nearest real speaker.
5. **Save.** The app writes the transcript next to the source file.

## How the Practice tab analyses speech

The Practice tab uses the same transcript. Then it measures the text:

1. The app transcribes the recording with the **Small** model. A prompt tells the model to keep "um", "uh" and stutters.
2. **Pace** is the number of words for each minute of speaking time.
3. **Fillers** are the words that match the word list for the language that you select.
4. **Pauses** are gaps of 0.8 seconds or more between lines.
5. **Stumbles** are repeated words and stuttered starts.
6. Rules change the numbers into tips. The app compares the result with your last run in the same language.

## The parts

| Part | Function |
| --- | --- |
| Whisper (ggml) | Changes speech to text |
| Silero VAD | Finds the parts that have speech |
| pyannote segmentation and 3D-Speaker CAM++ | Finds the speakers |
| ffmpeg | Reads video and other audio formats |
| .NET MAUI | The Windows and macOS app |
| Ollama (optional, you install it) | Written feedback in the Practice tab |

All these parts are in the app, with one exception. You install Ollama yourself if you want it.

## Fixed and checked versions

The models and tools in the app have fixed versions. When the app is built, a SHA-256 checksum confirms each file. For this reason, the files in a release are the same files that the developer reviewed.

## Source code

The source code is open: [github.com/Eomar21/transcriber](https://github.com/Eomar21/transcriber). The README explains how to build the app and how it is tested.
