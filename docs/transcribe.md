---
title: Transcribe
nav_order: 3
---

# Transcribe
{: .no_toc }

Use this mode to change speech in a recording into text with time stamps. You can also show which person speaks each line.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

![Transcribe mode]({{ '/transcribe.gif' | relative_url }})

## Use cases

- Make notes of a meeting, an interview or a lecture.
- Make subtitles for a video (`.srt` or `.vtt`).
- Find the time when a person said a word in a long recording.
- Transcribe a podcast or a voice memo.
- Transcribe a recording that must stay on your computer.

## Procedure

1. Add your files. Drop them on the window, or click **Choose files**. You can add more than one file and transcribe them in one run.
2. Set the language. Keep **Auto-detect**, or select the language of the speech. If you know the language, select it. This gives better results for short recordings. The list shows the languages that the selected model transcribes well.
3. Set the model. The default model is **Small (balanced)**. It is already in the app. Refer to [Models and languages]({{ '/models-and-languages' | relative_url }}).
4. Set the format. Select **Text (.txt)**, **.srt**, **.vtt** or **JSON**. Refer to [Output formats]({{ '/output-formats' | relative_url }}).
5. Type names or special terms in the **Names or special terms** field (optional). For example, type `Eindhoven, Dr. Haddad`. This helps the app to write these words correctly.
6. Turn on **Label who is speaking** (optional). Refer to [Speaker labels]({{ '/speaker-labels' | relative_url }}).
7. Click **Transcribe**. The page scrolls to the progress bar. A small runner moves along the bar. One bar covers all the files. A burst of sparks shows when the work is complete. To stop the work, click **Cancel**.

When the work is done, the app shows **Transcript saved** and the location of the file. Two buttons show: **Open transcript** and **Show in folder**. The text also shows in the list below.

## Supported files

The app supports the common audio formats and video formats, for example mp3, wav, m4a, flac, opus, mp4, mov and mkv. Refer to [Supported formats]({{ '/supported-formats' | relative_url }}) for the full list.

The app takes the audio from a video file. You do not have to convert the file. If you drop many files and the app does not support some of them, the app adds the supported files only. A message shows how many files the app added.

## Use the result

- **Hear a line.** Click a line. The recording plays from that point. A player bar shows. Use the bar to pause or stop the recording.
- **Search.** Type a word in the search box above the list.
- **Copy all.** Click **Copy all** to copy the full transcript. The copy includes the speaker labels, if you used them.
- **Open the file.** Click **Open transcript** to open the saved file. Click **Show in folder** to show the file in Explorer or Finder.

## Where the app saves the file

The app saves the file in the folder of the recording. The name is `<recording name>_transcript.<extension>`. For example, the text transcript of `interview.mp3` is `interview_transcript.txt`.

{: .warning }
If you transcribe the same file again in the same format, the app replaces the earlier transcript. The app does not ask first. Copy or rename a transcript that you want to keep.

## Settings

| Setting | Function |
| --- | --- |
| **Language** | Sets automatic detection, or the language of the speech. The list depends on the model. |
| **Model** | Sets the speech model. A larger model is more accurate and slower. |
| **Save as** | Sets the output format. |
| **Names or special terms** | Helps the app to write names and special words correctly. |
| **Use graphics card when available** | Uses the graphics card (Vulkan) for more speed. If no graphics card is available, the app uses the processor. A change has effect after you restart the app. |
| **Label who is speaking** | Adds Speaker 1, Speaker 2 and so on to each line. This setting adds some processing time. |

## Special cases

| Situation | Result |
| --- | --- |
| **Silence, music or background noise** | The app first finds the speech. It does not send silent parts to the speech model. This prevents invented text and keeps the time stamps correct. |
| **A recording with almost no speech** | The transcript can be empty or very short. This is normal. It is not an error. |
| **Text that nobody said** | On unclear audio, a speech model can invent phrases. The app removes the clear cases, for example a phrase that repeats. Some mistakes can stay. To check a line, click it and listen. |
| **A very long recording** | The app processes the recording in parts and joins them. It takes more time. You can cancel at any time. |
| **More than one language in one recording** | Auto-detect can write only one language for a part of the speech. The result then shows **Some speech may be missing**. Select the main language yourself. Refer to [Models and languages]({{ '/models-and-languages' | relative_url }}#recordings-with-more-than-one-language). |
| **Dutch with the Small model** | The Small model makes more mistakes in Dutch. A hint shows next to **Language** and with the result. Click **Use the Medium model** to get a better result. |
| **A name or a technical term is wrong** | Add the term to **Names or special terms**. Or use a larger model. |
| **Two files with the same name in the same folder** | Their transcripts get the same name. The second transcript replaces the first. Transcribe the files one at a time, or rename one file. |
| **The app cannot read a file** | The app shows an error for that file. Refer to [Troubleshooting]({{ '/troubleshooting' | relative_url }}). |
