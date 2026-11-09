---
title: Privacy
nav_order: 8
---

# Privacy
{: .no_toc }

1. TOC
{:toc}

The app keeps your audio on your computer. It does not send your audio to the internet.

## What stays on your computer

| Item | Location |
| --- | --- |
| Your recordings and files | In the folder where you keep them. The app reads them. The app does not copy them and does not upload them. |
| Transcripts | In the folder of the recording. |
| Recordings that you make in the Practice tab | A temporary file on your computer. The app deletes the file when you record again or select another file. If the app closes by accident, the app deletes the file after one day. |
| Practice history | A small file with the name `practice-history.json`. It has the measures and the title of each run. It has no audio. |
| Speech, voice and speaker models | In the app. If you select a larger model, the models folder. |

## What the app connects to

- **Normal work: no connection.** Transcription, speaker detection, speech analysis and recording all run on your computer.
- **Ollama on your own computer (optional).** If you use written feedback in the Practice tab, the app sends your transcript and measures to Ollama at `localhost`. Ollama is a program on your computer. It is not on the internet.
- **A model download, only when you ask for it.** If you select a larger speech model than the included model, the app downloads it one time from its public host. The request does not contain your audio.

## What the app does not do

- It does not upload audio, transcripts or practice results.
- It does not collect usage data.
- It does not need an account.

## Check it yourself

1. Disconnect your computer from the network.
2. Transcribe a file.
3. Analyse a recording in the Practice tab.

Both functions work without a network. The source code is open. You can read it at [github.com/Eomar21/transcriber](https://github.com/Eomar21/transcriber).

## Remove your data

- **Practice history:** In the Practice tab, click **Delete** or **Clear all**. Or delete `practice-history.json` from the local data folder of the app.
- **Transcripts:** A transcript is a normal file next to your recording. Delete it as you delete other files.
- **Downloaded models:** Delete them from the `models` folder in the local data folder of the app.
