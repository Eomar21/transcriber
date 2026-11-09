---
title: Practice
nav_order: 4
---

# Practice
{: .no_toc }

Record yourself speaking and see how to improve.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

![Practice mode]({{ '/practice.gif' | relative_url }})

## Use cases

- Rehearsing a presentation, talk or pitch
- Preparing interview answers
- Reducing filler words ("um", "uh") and finding a comfortable pace
- Checking progress over weeks of practice
- Practising in English, Dutch or Spanish

Nothing is uploaded. Your recording is analysed on your computer.

## Step by step

1. Open the **Practice** tab.
2. Press **Record**, speak, and press **Stop**. A level bar shows that the microphone is hearing you. Or press **Choose file** to analyse an existing audio or video file.
3. Choose the **language spoken**: English, Dutch or Spanish.
4. Press **Analyse my speaking**.
5. Read the five tiles and the tips under **What to work on**.
6. Check **What was heard** to compare the numbers with your actual words.
7. Past runs appear under **Your progress**.

## What is measured

| Measure | How it is worked out | A good range |
| --- | --- | --- |
| **Pace** | Words per minute of the time you were actually speaking, so long silences do not slow it down. | 120 to 160 words per minute is easy to follow. Under 110 is flagged as slow and over 170 as fast. |
| **Filler words** | Fillers per 100 words, from lists for English, Dutch and Spanish, such as "um", "uh", "euh", "eh" and "este". The word "like" is only counted when it is set apart by commas, so "I like cats" is never flagged. | Up to 2 per 100 words is clean. Over 5 is worth working on. |
| **Pauses** | Gaps of 0.8 seconds or more between spoken lines. Shown as a count, with the longest one. | A few pauses help listeners take things in. More than about 6 a minute reads as hesitation. |
| **Stumbles** | Repeated words such as "the the", and stuttered starts such as "b-b-because". Emphasis ("very, very") and single letters are not counted. | None is ideal. |
| **Length** | Duration and word count. | |

## Tips and symbols

Each measure comes with a short tip. A symbol shows its kind, so it does not depend on colour alone:

| Symbol | Meaning |
| --- | --- |
| ✓ | Good |
| ! | Something to work on |
| i | Information |

## Progress over time

Each analysis of **at least 25 words** is saved. The next run in the same language is compared with the previous one, with tips such as "Fewer fillers than last time" or "Pace moved toward the sweet spot".

Under **Your progress** you can **Delete** one run or **Clear all**. Shorter recordings are still analysed but are not saved, because the numbers would be too noisy to compare.

## Recording

- Recordings are saved as 16 kHz mono audio and used directly.
- A recording under one second is treated as an accidental click and discarded.
- A recording stops by itself after **30 minutes**.
- If no sound arrives for about three seconds, a warning appears: check that the microphone is connected, not muted and allowed in your system's privacy settings.
- Your recording is kept while you work with it, so you can analyse it again, for example in another language. It is deleted when you record again or choose another file. Files you choose yourself are never deleted.
- Leftover recordings older than a day are cleaned up when the app starts.

### Microphone permission

- **Windows:** if recording fails or stays silent, open **Settings, Privacy and security, Microphone** and allow desktop apps to use the microphone.
- **macOS:** the first time you record, macOS asks for permission. If you said no, change it in **System Settings, Privacy and Security, Microphone**.

## How filler words are counted

Speech recognition normally cleans up "um" and "uh", which would hide exactly what this tab measures. Practice transcribes with settings that encourage the model to keep fillers and stutters. The **What was heard** box shows the result, so you can see which words were kept.

Filler counts are only as good as that transcript. A clear recording close to the microphone gives the most reliable numbers.

## Written feedback with Ollama (optional)

If you use [Ollama](https://ollama.com) on your computer, Practice can ask one of your models for a short written review of your transcript and measures.

1. Install Ollama and download any model.
2. Make sure Ollama is running.
3. Open the Practice tab. If it finds models, a picker and **Get written feedback** appear after you analyse a recording.

The request goes to Ollama on your own computer (`localhost`), not to the internet. If Ollama is not running, the tab says how to set it up, and everything else keeps working.

## Where your history is stored

Practice history is a small file named `practice-history.json` in the app's local data folder. On Windows that is `%LOCALAPPDATA%\Transcriber`. It holds the measures and the title of each run. It does not contain your audio.

## Edge cases

| Situation | What happens |
| --- | --- |
| **Under 25 words** | The recording is analysed but not saved to your history. |
| **Very short recording** | Under one second is discarded with a hint to try again. |
| **No microphone** | A message explains that none was found. Use **Choose file** with a recording made elsewhere. |
| **Microphone blocked or muted** | The recording is silent. After a few seconds a warning appears. |
| **A pause that is not counted** | Pauses are measured from gaps between spoken lines, so very short hesitations inside a line may not appear. |
| **A filler the app missed** | The model may have left it out. Check **What was heard**. |
| **Recording in another language than selected** | Results will be poor. Choose the language you actually spoke. |
| **Language not in the list** | Practice currently supports English, Dutch and Spanish. For other languages, use the Transcribe tab. |
| **Background noise or several voices** | The analysis assumes one speaker. Other voices count as yours. |
| **A very fast speaker** | A high pace is reported honestly, for example 220 words per minute for fast conversation, and the tip suggests slowing down. |
