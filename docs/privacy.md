---
title: Privacy
nav_order: 8
---

# Privacy
{: .no_toc }

1. TOC
{:toc}

Transcriber is built so that your audio never leaves your computer.

## What stays on your computer

| What | Where |
| --- | --- |
| Your recordings and files | Wherever you keep them. The app reads them but does not copy or upload them. |
| Transcripts | Saved next to your recording. |
| Recordings made in the Practice tab | A temporary file on your computer, deleted when you record again or choose another file, and cleaned up after a day if the app was closed unexpectedly. |
| Practice history | A small file, `practice-history.json`, with the measures and title of each run. No audio. |
| Speech, voice and speaker models | Inside the app, or in the models folder after you choose a bigger model. |

## What the app connects to

- **Nothing, for its normal work.** Transcription, speaker detection, speech analysis and recording all run locally.
- **Ollama on your own computer (optional).** If you use written feedback in Practice, the app sends your transcript and measures to Ollama at `localhost`. That is a program on your machine, not the internet.
- **A model download, only if you ask.** Choosing a bigger speech model than the one included downloads it once from its public host. Your audio is not part of that request.

## What the app does not do

- It does not upload audio, transcripts or practice results.
- It does not collect usage data.
- It does not need an account.

## Checking it yourself

You can confirm this the simplest way: turn off your network connection, then transcribe a file and analyse a recording. Both work. The code is open source, so you can read exactly what it does at [github.com/Eomar21/transcriber](https://github.com/Eomar21/transcriber).

## Removing your data

- **Practice history:** use **Delete** or **Clear all** in the Practice tab, or delete `practice-history.json` from the app's local data folder.
- **Transcripts:** they are ordinary files next to your recordings. Delete them like any other file.
- **Downloaded models:** delete them from the `models` folder in the app's local data folder.
