---
title: Troubleshooting
nav_order: 10
---

# Troubleshooting
{: .no_toc }

1. TOC
{:toc}

## Install and open the app

### Windows shows a SmartScreen warning

The app does not have a code signature yet. For this reason, Windows does not know the app. Click **More info**. Then click **Run anyway**. The warning shows only the first time.

### macOS says that it cannot open the app

Right-click the app (or Control-click). Select **Open**. Then confirm. You must do this one time. The app does not have a code signature yet.

### The app does not run on my Mac

The app runs on Apple Silicon Macs only (M1 and later). It does not run on Intel Macs. To find your chip, open the Apple menu. Select **About This Mac**.

### The download is very large

The file is about 550 MB. It contains the speech model, the voice model, the speaker models and ffmpeg. For this reason, the app works at once and without a network.

## Transcribe

### The app cannot read a file

The app shows one of these messages for the file:

| Message | Meaning and action |
| --- | --- |
| **This file has no audio track, so there is nothing to transcribe.** | The video has no sound. Select a file that has sound. |
| **This file could not be read. It may be damaged, or it may not be an audio or video file.** | The file is damaged, or it is not a media file. Play the file in a different player. |

If nothing happens when you add a file, do these checks:

- Make sure that the format is in the list on [Supported formats]({{ '/supported-formats' | relative_url }}).
- Play the file in a different player. The app cannot transcribe a damaged file.
- If the file is in a folder that a cloud service synchronises, make sure that the service has downloaded the file.

### The transcript is empty or very short

The recording can have almost no speech, or the speech can be very quiet. Check the volume. Select a larger model. Select the language yourself.

### The transcript has wrong words, names or numbers

- Check for the hint **Some speech may be missing** or a hint about the Medium model. Follow the hint.
- Select the language. Do not use **Auto-detect**.
- Add names and special terms to **Names or special terms**.
- Select a larger model. Refer to [Models and languages]({{ '/models-and-languages' | relative_url }}).
- Use a cleaner recording, if possible.

### The transcript has text that nobody said

On unclear audio, a speech model can invent text. The app removes the clear cases, but not all of them. Click the line and listen to the recording.

### The app is slow

- Make sure that **Use graphics card when available** is on. A change has effect on the next run.
- Select a smaller model.
- A longer recording needs more time. You can cancel at any time.
- Speaker labels add time.

### I transcribed again and the earlier transcript is gone

The app replaces a transcript that has the same name and format. Rename or copy a transcript that you want to keep.

## Speaker labels

### The speaker option does not show

The switch shows only when the app finds the speaker models. Install the app again from the release download. On macOS, the speaker detection program is a part of the app. An app copy that is damaged or not complete can cause this problem.

### The speaker numbers are wrong, or a speaker is missing

The app numbers the speakers in the order of their first speech. The first speaker is **Speaker 1**. The numbers have no gaps. If you see different numbers, you can have version 1.1.0 or an earlier version. These versions can show the internal numbers of the detector, for example Speaker 5 with no Speaker 1. Newer versions number the speakers correctly. Install the latest release.

### One person has more than one label, or many people have one label

Select the number of speakers yourself. Do not use **Detect automatically**. Refer to [Speaker labels]({{ '/speaker-labels' | relative_url }}).

### A line has the wrong speaker

The labels follow the sound of the voices. Short replies, interruptions and two people who speak at the same time are the most difficult cases. To improve the result, select the number of speakers.

## Practice

### The message "No sound is coming in" shows

The microphone is silent. Do these checks:

1. Make sure that the microphone is connected and not muted. Make sure that its volume is not zero.
2. Make sure that your system uses this microphone as the default input.
3. **Windows:** Open **Settings**, then **Privacy and security**, then **Microphone**. Allow desktop apps to use the microphone.
4. **macOS:** Open **System Settings**, then **Privacy and Security**, then **Microphone**. Turn on Transcriber.

### The message "No microphone was found" shows

Connect a microphone. Or click **Choose file** and select a recording that you made with other software.

### The recording stopped by itself

A recording stops after 30 minutes. Start a new recording for the rest.

### The filler word count is 0, but I said "um"

The speech model can remove a filler word from the text. Read **What was heard** to see which words the app counted. A clear recording that is close to the microphone gives better results.

### The pace looks wrong

The pace is the number of words for each minute of speaking time. Long silences do not count. A fast conversation can show more than 200. A recording with music or other voices gives results that are not correct.

### My run is not in the progress list

The app saves only recordings of **25 words or more**. This makes the comparison between runs useful.

### The button "Get written feedback" does not show

The button shows only when Ollama is running on your computer and has at least one model. Install Ollama. Download a model. Start Ollama. Then open the Practice tab again.

### The written feedback fails or takes a long time

The model runs on your computer. A large model on a slow computer can take many minutes. Select a smaller model.

### The result says that some speech may be missing

The recording can have more than one language. Or the audio can be unclear. Auto-detect can write only one language for a part of the speech.

1. Select the main language. Do not use **Auto-detect**.
2. Use a larger model.
3. Transcribe the recording again.

## If the problem stays

Open an issue on [GitHub](https://github.com/Eomar21/transcriber/issues). Include this information:

- Your system (Windows or macOS) and its version.
- The version of the app. It is on the [releases page](https://github.com/Eomar21/transcriber/releases).
- What you did, what you expected and what happened.

Do not attach recordings that must stay private.
