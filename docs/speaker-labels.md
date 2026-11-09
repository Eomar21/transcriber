---
title: Speaker labels
nav_order: 5
---

# Speaker labels
{: .no_toc }

Mark who said each line: Speaker 1, Speaker 2 and so on.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

## Use cases

- Interviews and podcasts with two or more voices
- Meeting notes where it matters who said what
- Subtitles or transcripts that need a line per speaker

## How to use it

1. On the **Transcribe** tab, turn on **Label who is speaking**.
2. Choose how many people speak, or leave it on **Detect automatically**.
3. Press **Transcribe**.

Each line then starts with its speaker. The labels appear in the list on screen, in **Copy all**, and in every saved format. See [Output formats]({{ '/output-formats' | relative_url }}) for how each format shows them.

{: .note }
Speaker labels add some processing time, because the recording is analysed a second time to tell the voices apart.

## What the labels mean

- Speakers are numbered **in the order they first speak**. The first voice in the transcript is always **Speaker 1**, the next new voice is **Speaker 2**, and so on, with no gaps.
- The numbers only identify voices within one transcript. "Speaker 1" in one file is not the same person as "Speaker 1" in another.
- The app does not know names. If you want names, search and replace the labels in the saved file.

## How it works

1. A model finds where speech changes hands.
2. A second model describes the sound of each voice.
3. Voices that sound alike are grouped together.
4. Each transcript line gets the speaker who talks for most of that line.

All of it runs on your computer.

## Choosing the number of speakers

| Choice | When to use it |
| --- | --- |
| **Detect automatically** | You do not know how many people speak, or the recording is clear. |
| **2 to 6** | You know how many people speak. This is usually more accurate, especially on noisy or difficult recordings. |

With **Detect automatically**, the app also merges very short stretches that the detector wrongly treated as separate people (a breath, a cough or a change of tone). A voice counts as a real speaker when it talks for at least 5% of all the speech, or at least 20 seconds, so a short but genuine second voice is kept.

When you choose a number yourself, that number is used as given.

## Edge cases

| Situation | What happens |
| --- | --- |
| **Two people talk at once** | A line gets the speaker who talks for most of it. Overlapping speech is the hardest case, so expect mistakes. |
| **A very short reply ("yes", "right")** | It may be given to the neighbouring speaker. Short lines carry little voice information. |
| **One person split into two** | Choose the number of speakers yourself. |
| **Two people merged into one** | Choose the number of speakers yourself, or use a recording with less background noise. |
| **Similar voices** | Voices that sound alike are harder to separate. |
| **A line with no clear speaker** | It is left without a label rather than guessed. |
| **More than six people** | The number picker stops at six. Use automatic detection, and expect lower accuracy. |
| **The switch is missing** | The speaker models were not found. See [Troubleshooting]({{ '/troubleshooting' | relative_url }}). |
| **Wrong attribution at a question and answer** | Labels follow the sound of the voices, not the meaning. Where a question and its answer are close together, they can be grouped as one voice. |

## On macOS

Speaker detection runs as a small helper program that is part of the app, because the Mac app cannot load the library the Windows version uses. You do not need to do anything. The result is the same kind of labelling.
