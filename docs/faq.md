---
title: FAQ
nav_order: 11
---

# Frequently asked questions
{: .no_toc }

1. TOC
{:toc}

## General

### Does the app work offline?

Yes. Transcription, speaker detection, speech analysis and recording all run on your computer. Refer to [Privacy]({{ '/privacy' | relative_url }}). It explains the one optional local connection (Ollama on your own computer).

### Is the app free?

Yes. The app is open source and has the MIT licence.

### Which systems does the app support?

The app supports Windows 10 and 11 (64-bit) and macOS on Apple Silicon (M1 and later). It does not support Intel Macs or Linux.

### Why is the download so large?

The download contains everything that the app needs: the speech model (about 488 MB), the voice model, the speaker models and ffmpeg. For this reason, the app does not download files at first start.

### Do I need an internet connection?

No. You need the internet only to download an extra, larger speech model. You do this one time.

### Do I need a graphics card?

No. A graphics card makes the transcription faster. If no graphics card is available, the app uses the processor.

## Transcribe

### How accurate is the transcript?

The accuracy depends on the recording and the model. A clear recording with the default model usually gives very good results. Noise, accents, special terms and people who speak at the same time reduce the accuracy. These actions help: use a larger model, select the language, and add names to **Names or special terms**.

### How long does the app need?

The time depends on your computer. For example, a recording of 6.5 minutes needed less than two minutes with a graphics card, with speaker labels. The processor alone is slower. A longer recording, a larger model and speaker labels add time.

### Can the app transcribe video?

Yes. The app supports mp4, mov, mkv, webm, avi and other video formats. The app takes the audio from the video. Refer to [Supported formats]({{ '/supported-formats' | relative_url }}).

### Which languages does the app support?

The Small model, which is in the app, supports English, Spanish, Portuguese, Italian, German and Dutch. Dutch shows a hint to use the Medium model for better results. Larger models add French, Polish, Turkish, Indonesian, Swedish and Ukrainian. Refer to [Models and languages]({{ '/models-and-languages' | relative_url }}).

### Can the app make subtitles?

Yes. Under **Save as**, select **SubRip (.srt)** or **WebVTT (.vtt)**.

### Can the app show who speaks?

Yes. It shows Speaker 1, Speaker 2 and so on. The app does not know names. Refer to [Speaker labels]({{ '/speaker-labels' | relative_url }}).

### Where does the transcript go?

The app saves it next to your recording. The name is `<name>_transcript.<extension>`. The app replaces an earlier transcript that has the same name and format.

### Can I transcribe many files at the same time?

Yes. Add the files. Click **Transcribe**. Each file gets its own transcript file.

## Practice

### What does the Practice tab measure?

It measures pace, filler words, pauses, stumbles and length. It gives tips and shows your progress over time. Refer to [Practice]({{ '/practice' | relative_url }}).

### Does the app judge how my voice sounds?

No. The app analyses what you said and when you said it. It does not judge tone, volume or accent. It cannot tell if you sound confident. It measures only the habits in the list above.

### Which languages does the Practice tab support?

English, Spanish and Dutch. The Practice tab uses the Small model. Dutch is less accurate with this model.

### Why did the app not find my "um"?

Speech models normally remove filler words. The Practice tab tells the model to keep them, but this is not perfect. **What was heard** shows the words that the app counted.

### Is my practice history private?

Yes. The history is a small file on your computer. It has numbers and titles. It has no audio. You can delete runs or clear all of them in the tab.

### Do I need Ollama?

No. Ollama is optional. It adds a written review only. All other functions work without it.

## Problems

### Windows or macOS shows a warning about the app

The app does not have a code signature yet. For this reason, both systems show a warning one time. Refer to [Troubleshooting]({{ '/troubleshooting' | relative_url }}) for the two clicks that you must do.

### I found a bug, or I have an idea

Open an issue on [GitHub](https://github.com/Eomar21/transcriber/issues).
