---
title: Practice
nav_order: 4
---

# Practice
{: .no_toc }

Use this mode to record your voice and to see how you can improve your speech.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

![Practice mode]({{ '/practice.gif' | relative_url }})

## Use cases

- Practise a presentation, a talk or a pitch.
- Prepare answers for an interview.
- Use fewer filler words (for example "um" and "uh") and find a good pace.
- Check your progress over many weeks.
- Practise in English, Dutch or Spanish.

The app does not send anything to the internet. It analyses your recording on your computer.

## Procedure

1. Open the **Practice** tab.
2. Click **Record**. Speak. Click **Stop**. A level bar shows that the microphone hears you. Or click **Choose file** to analyse a recording that you already have.
3. Select the language that you speak: English, Dutch or Spanish.
4. Click **Analyse my speaking**.
5. Read the five tiles and the tips under **What to work on**.
6. Read **What was heard**. Compare the numbers with your words.
7. Find your earlier runs under **Your progress**.

## What the app measures

| Measure | How the app calculates it | Good range |
| --- | --- | --- |
| **Pace** | The number of words for each minute of speaking time. Long silences do not count. | 120 to 160 words each minute is easy to follow. The app marks a pace below 110 as slow and a pace above 170 as fast. |
| **Filler words** | The number of filler words for each 100 words. The app uses word lists for English, Dutch and Spanish, for example "um", "uh", "euh", "eh" and "este". The app counts "like" only when commas separate it from the other words. The phrase "I like cats" is not a filler. | Up to 2 for each 100 words is clean. More than 5 needs work. |
| **Pauses** | Gaps of 0.8 seconds or more between two spoken lines. The app shows the number of pauses and the longest pause. | A few pauses help the listener. More than about 6 pauses each minute shows hesitation. |
| **Stumbles** | Repeated words (for example "the the") and stuttered starts (for example "b-b-because"). The app does not count words that you repeat for emphasis ("very, very") or single letters. | None is best. |
| **Length** | The duration and the number of words. | Not applicable. |

## Tips and symbols

Each measure has a short tip. A symbol shows the type of the tip. In this way, the meaning does not depend on color only.

| Symbol | Meaning |
| --- | --- |
| ✓ | Good |
| ! | Needs work |
| i | Information |

## Progress over time

The app saves each analysis of **25 words or more**. The app compares a new run with your last run in the same language. For example, the app can show "Fewer fillers than last time" or "Pace moved toward the sweet spot".

Under **Your progress**, click **Delete** to remove one run. Click **Clear all** to remove all runs. The app analyses shorter recordings, but it does not save them. The numbers of a short recording are not stable enough to compare.

## Recording

- The app saves a recording as 16 kHz mono audio. The app uses the recording as it is.
- If a recording is shorter than one second, the app discards it. The app assumes that you clicked by accident.
- A recording stops by itself after **30 minutes**.
- If no sound comes in for about three seconds, a warning shows. Make sure that the microphone is connected, that it is not muted and that your system allows access to it.
- The app keeps your recording while you work with it. You can analyse it again, for example in another language. The app deletes the recording when you record again or select another file. The app never deletes a file that you selected yourself.
- When the app starts, it removes recordings that are more than one day old and that it did not delete before.

### Microphone permission

- **Windows:** If the recording fails or is silent, open **Settings**, then **Privacy and security**, then **Microphone**. Allow desktop apps to use the microphone.
- **macOS:** At the first recording, macOS asks for permission. If you selected **Don't Allow**, open **System Settings**, then **Privacy and Security**, then **Microphone**. Turn on Transcriber.

## How the app counts filler words

A speech model normally removes "um" and "uh" from the text. This hides the words that this tab must measure. For this reason, the Practice tab sets the model to keep filler words and stutters. **What was heard** shows the result, so you can see which words the app kept.

The count of filler words is only as good as this text. A clear recording that is close to the microphone gives the most reliable numbers.

## Written feedback with Ollama (optional)

[Ollama](https://ollama.com) is a program that runs language models on your computer. If you use Ollama, the Practice tab can ask one of your models for a short written review of your transcript and measures.

1. Install Ollama. Download a model.
2. Make sure that Ollama is running.
3. Open the Practice tab. Analyse a recording. If the app finds models, a model list and the **Get written feedback** button show.

The app sends the request to Ollama on your own computer (`localhost`). The request does not go to the internet. If Ollama is not running, the tab shows how to set it up. All other functions continue to work.

## Where the app stores your history

The history is a small file with the name `practice-history.json`. It is in the local data folder of the app. On Windows, this folder is `%LOCALAPPDATA%\Transcriber`. The file contains the measures and the title of each run. It does not contain your audio.

## Special cases

| Situation | Result |
| --- | --- |
| **Less than 25 words** | The app analyses the recording. It does not save the run in your history. |
| **Recording shorter than one second** | The app discards the recording and shows a hint. |
| **No microphone** | A message shows that the app found no microphone. Click **Choose file** and select a recording that you made with other software. |
| **Microphone blocked or muted** | The recording is silent. After a few seconds, a warning shows. |
| **A pause that the app did not count** | The app finds pauses in the gaps between spoken lines. It can miss a very short hesitation inside a line. |
| **A filler word that the app did not find** | The model can remove a filler word from the text. Read **What was heard**. |
| **You select a different language than you speak** | The results are poor. Select the language that you speak. |
| **Your language is not in the list** | The Practice tab supports English, Dutch and Spanish only. For other languages, use the Transcribe tab. |
| **Background noise or more than one voice** | The app assumes that one person speaks. It counts all voices as yours. |
| **A very fast speaker** | The app shows the real pace, for example 220 words each minute for a fast conversation. The tip tells you to speak slower. |
