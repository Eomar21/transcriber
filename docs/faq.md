---
title: FAQ
nav_order: 11
---

# Frequently asked questions
{: .no_toc }

1. TOC
{:toc}

## General

### Is it really offline?

Yes. Transcription, speaker detection, speech analysis and recording all run on your computer. See [Privacy]({{ '/privacy' | relative_url }}) for the details, including the one optional local connection (Ollama on your own machine).

### Is it free?

Yes. It is open source under the MIT licence.

### Which systems does it run on?

Windows 10 and 11 (64-bit), and macOS on Apple Silicon (M1 and later). Intel Macs and Linux are not supported.

### Why is the download so big?

It includes everything: the speech model (about 488 MB), the voice and speaker models, and ffmpeg. That is why nothing is downloaded when you first start it.

### Do I need an internet connection?

Not to use it. Only downloading an extra, bigger speech model needs the internet, once.

### Do I need a graphics card?

No. A graphics card makes transcription faster when available, and the app falls back to the processor by itself.

## Transcribe

### How accurate is it?

It depends on the recording and the model. Clear speech with the default model is usually very good. Noise, accents, jargon and several people talking at once reduce accuracy. A bigger model, choosing the language yourself and adding names under **Names or special terms** all help.

### How long does it take?

It depends on your computer. For example, a 6.5 minute recording took under two minutes with a graphics card, including speaker labels. The processor alone is slower. Longer recordings, bigger models and speaker labels all add time.

### Can I transcribe video?

Yes. mp4, mov, avi, mkv and webm are supported, and the audio is extracted for you.

### Which languages are supported?

Automatic detection or your choice. It is tested with English, Dutch, Arabic and Spanish, and the model understands many more. See [Models and languages]({{ '/models-and-languages' | relative_url }}).

### Can it make subtitles?

Yes. Choose **SubRip (.srt)** or **WebVTT (.vtt)** under **Save as**.

### Can it tell me who is speaking?

Yes, as Speaker 1, Speaker 2 and so on. It does not know names. See [Speaker labels]({{ '/speaker-labels' | relative_url }}).

### Where does the transcript go?

Next to your recording, as `<name>_transcript.<extension>`. It overwrites an earlier transcript with the same name and format.

### Can I transcribe many files at once?

Yes. Add several files and press **Transcribe**. Each gets its own transcript file.

## Practice

### What does Practice measure?

Pace, filler words, pauses, stumbles and length, with tips and your progress over time. See [Practice]({{ '/practice' | relative_url }}).

### Does it hear what my voice sounds like?

It analyses what you said and when, not tone, volume or accent. It cannot judge how confident you sound, only the measurable habits listed above.

### Which languages does Practice support?

English, Dutch and Spanish.

### Why did it miss my "um"?

Speech models tend to leave fillers out. Practice asks the model to keep them, but it is not perfect. **What was heard** shows exactly what was counted.

### Is my practice history private?

Yes. It is a small file on your computer with numbers and titles, not audio. You can delete runs or clear everything in the tab.

### Do I need Ollama?

No. It is optional and only adds a written review. Everything else works without it.

## Problems

### Windows or macOS warns me about the app

The app is not code signed yet, so both systems show a one-time warning. See [Troubleshooting]({{ '/troubleshooting' | relative_url }}) for the two clicks needed.

### I found a bug or have an idea

Please open an issue on [GitHub](https://github.com/Eomar21/transcriber/issues).
