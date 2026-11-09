---
title: Transcribe
nav_order: 3
---

# Transcribe
{: .no_toc }

Turn audio and video into text with timestamps, and optionally say who is speaking.
{: .fs-6 .fw-300 }

1. TOC
{:toc}

![Transcribe mode]({{ '/transcribe.gif' | relative_url }})

## Use cases

- Meeting, interview and lecture notes
- Subtitles for a video (`.srt` or `.vtt`)
- Searching a long recording for the moment something was said
- Podcasts and voice memos
- Anything where the recording must not leave your computer

## Step by step

1. **Add files.** Drop them onto the window, or press **Choose files**. You can add several and transcribe them in one run. You can also start the app with a file, for example by opening it with Transcriber.
2. **Pick the language.** Leave it on **Auto-detect**, or choose the language spoken. Choosing it is more reliable when you know it, especially for short recordings.
3. **Pick the model.** The default, **Small (balanced)**, is already included. See [Models and languages]({{ '/models-and-languages' | relative_url }}).
4. **Pick the format.** **Text (.txt)**, subtitles (**.srt** or **.vtt**) or **JSON**. See [Output formats]({{ '/output-formats' | relative_url }}).
5. **Names or special terms (optional).** Type names or jargon, for example `Eindhoven, Dr. Haddad`. This helps recognition of words the model would otherwise guess.
6. **Label who is speaking (optional).** See [Speaker labels]({{ '/speaker-labels' | relative_url }}).
7. Press **Transcribe**. Progress is shown while it runs, and you can cancel at any time.

When it finishes you see **Transcript saved** with the file's location, and buttons to **Open transcript** and **Show in folder**. The text also appears in the list below.

## Supported files

mp3, wav, mp4, mov, avi, mkv, webm, m4a, flac, ogg and aac.

Video files have their audio extracted automatically, so you do not need to convert anything first. If you drop several files and some are not supported, the supported ones are added and a message says how many were added.

## Working with the result

- **Click a line to hear it.** The recording plays from that moment, and a player bar appears where you can pause or stop it.
- **Search the transcript** with the search box above the list.
- **Copy all** puts the whole transcript, with speaker labels if you used them, on the clipboard.
- **Open transcript** opens the saved file. **Show in folder** reveals it in Explorer or Finder.

## Where the file is saved

Next to the recording, named `<recording name>_transcript.<extension>`. For `interview.mp3` as text, that is `interview_transcript.txt`.

{: .warning }
Transcribing the same file again with the same format **overwrites** the earlier transcript without asking. Copy or rename a transcript you want to keep.

## Settings

| Setting | What it does |
| --- | --- |
| **Language** | Auto-detect, or the language spoken. |
| **Model** | Bigger models are more accurate and slower. |
| **Save as** | The output format. |
| **Names or special terms** | A hint that improves recognition of names and jargon. |
| **Use graphics card when available** | Uses your GPU through Vulkan for speed, and falls back to the processor automatically. A change applies after restarting the app. |
| **Label who is speaking** | Marks each line with Speaker 1, Speaker 2 and so on. Adds some processing time. |

## Edge cases

| Situation | What happens |
| --- | --- |
| **Silence, music or background noise** | A voice detector finds the speech first, so silent stretches are not sent to the speech model. This avoids invented text in quiet parts and keeps timestamps aligned. |
| **A recording with almost no speech** | The transcript can be empty or very short. That is expected, not an error. |
| **Made-up text on unclear audio** | Speech models sometimes invent phrases on poor audio. Obvious cases such as repeated filler phrases are filtered out, but unclear recordings can still produce mistakes. Check important quotes against the audio by clicking the line. |
| **Very long recordings** | They are processed in stretches and joined. They take longer, and a cancel is always available. |
| **Several languages in one recording** | Auto-detect picks one language for the whole file. For mixed-language recordings, results are better when you choose the main language yourself. |
| **Names and technical terms spelled wrongly** | Add them to **Names or special terms**, or use a larger model. |
| **Two files with the same name in the same folder** | Their transcripts would share a name too, and the second overwrites the first. Transcribe them one at a time or rename one. |
| **A file that cannot be read** | The app reports the problem for that file. See [Troubleshooting]({{ '/troubleshooting' | relative_url }}). |
