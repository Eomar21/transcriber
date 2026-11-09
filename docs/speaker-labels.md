---
title: Speaker labels
nav_order: 5
---

# Speaker labels
{: .no_toc }

Use this function to show which person speaks each line: Speaker 1, Speaker 2 and so on.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

## Use cases

- Interviews and podcasts with two or more voices.
- Meeting notes that show who said what.
- Subtitles or transcripts with one line for each speaker.

## Procedure

1. Open the **Transcribe** tab.
2. Turn on **Label who is speaking**.
3. Select the number of people who speak. Or keep **Detect automatically**.
4. Click **Transcribe**.

Each line then starts with its speaker. The labels show in the list, in **Copy all** and in each saved format. Refer to [Output formats]({{ '/output-formats' | relative_url }}) to see how each format shows the labels.

{: .note }
Speaker labels add processing time. The app analyses the recording a second time to find the different voices.

## What the labels mean

- The app numbers the speakers in the order of their first speech. The first voice in the transcript is always **Speaker 1**. The next new voice is **Speaker 2**. The numbers have no gaps.
- A number identifies a voice in one transcript only. Speaker 1 in one file is not necessarily the same person as Speaker 1 in another file.
- The app does not know names. To use names, replace the labels in the saved file.

## How it works

1. A model finds the points where the speaker changes.
2. A second model describes the sound of each voice.
3. The app puts voices that sound the same in one group.
4. The app gives each line to the speaker who talks for the longest time in that line.

The app does all of this on your computer.

## Select the number of speakers

| Choice | Use it when |
| --- | --- |
| **Detect automatically** | You do not know the number of speakers, or the recording is clear. |
| **2 to 6** | You know the number of speakers. This is usually more accurate, especially for noisy recordings or recordings that are difficult. |

When you select **Detect automatically**, the app can find too many speakers. For example, a breath, a cough or a change of tone can look like a new person. The app joins these short parts to the nearest real speaker. A voice is a real speaker if it has at least 5% of all the speech, or at least 20 seconds of speech. This keeps a short second voice that is real.

When you select a number, the app uses that number as given.

## Special cases

| Situation | Result |
| --- | --- |
| **Two people speak at the same time** | A line gets the speaker who talks the longest in that line. Overlapping speech is the most difficult case. Expect mistakes. |
| **A very short reply ("yes", "right")** | The app can give it to the speaker next to it. A short line has little voice information. |
| **One person has two labels** | Select the number of speakers yourself. |
| **Two people have one label** | Select the number of speakers yourself. Or use a recording with less background noise. |
| **Voices that sound the same** | These voices are more difficult to separate. |
| **No clear speaker for a line** | The app does not give the line a label. It does not guess. |
| **More than six people** | The number list stops at six. Use **Detect automatically**. Expect lower accuracy. |
| **The switch does not show** | The app did not find the speaker models. Refer to [Troubleshooting]({{ '/troubleshooting' | relative_url }}). |
| **A question and its answer have the same speaker** | The labels follow the sound of the voices. They do not follow the meaning. If a question and an answer are close together, the app can put them in one group. |

## On macOS

On macOS, the speaker detection runs as a small helper program in the app. The Mac app cannot load the library that the Windows version uses. You do not have to do anything. The labels are the same type of labels.
