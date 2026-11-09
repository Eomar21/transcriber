---
title: Troubleshooting
nav_order: 10
---

# Troubleshooting
{: .no_toc }

1. TOC
{:toc}

## Installing and opening

### Windows says it protected my PC (SmartScreen)

The app is not code signed yet, so Windows does not recognise it. Choose **More info**, then **Run anyway**. This only happens the first time.

### macOS says the app cannot be opened

Right-click (or Control-click) the app, choose **Open**, then confirm. This is needed once, because the app is not code signed yet.

### The app will not run on my Mac

Only Apple Silicon Macs (M1 and later) are supported. Intel Macs are not. To check, choose **About This Mac** from the Apple menu and look at the chip.

### The download is huge

About 550 MB, because the speech model, the voice and speaker models and ffmpeg are included. That is why the app works immediately and offline.

## Transcribing

### Nothing happens, or it says a file cannot be read

- Check the format is one of mp3, wav, mp4, mov, avi, mkv, webm, m4a, flac, ogg or aac.
- Try playing the file in another player. A damaged file cannot be transcribed.
- Move the file out of a cloud-synced folder that has not downloaded it yet.

### The transcript is empty or very short

The recording may have almost no speech, or the speech may be very quiet. Check the volume. Try a bigger model, and choose the language yourself.

### The transcript has wrong words, names or numbers

- Choose the language instead of **Auto-detect**.
- Add names and jargon to **Names or special terms**.
- Choose a bigger model. See [Models and languages]({{ '/models-and-languages' | relative_url }}).
- Use a cleaner recording when you can.

### The transcript contains text that was not said

Speech models sometimes make up text on unclear audio. Obvious cases are filtered out, but not all. Click the line to hear what was actually said.

### It is slow

- Make sure **Use graphics card when available** is on (a change needs a restart of the app).
- Choose a smaller model.
- Longer recordings take longer. You can cancel at any time.
- Turning on speaker labels adds extra time.

### I transcribed again and lost my earlier transcript

A transcript with the same name and format is overwritten. Rename or copy transcripts you want to keep.

## Speaker labels

### There is no speaker option

The switch only appears when the speaker models are present. Reinstall the app from the release download. On macOS the speaker detection program is part of the app, so a damaged or incomplete copy can cause this.

### The speakers are numbered strangely, or a speaker is missing

Speakers are numbered in the order they first speak, starting at **Speaker 1**, with no gaps. If you see something different, you may be on version 1.1.0 or earlier, which could show the detector's internal numbers (for example Speaker 5 with no Speaker 1). Newer builds number speakers by first appearance, so update to the latest release.

### One person is labelled as several people, or several as one

Choose the number of speakers yourself instead of **Detect automatically**. See [Speaker labels]({{ '/speaker-labels' | relative_url }}).

### A line has the wrong speaker

Labels follow how voices sound. Short replies, interruptions and people talking at the same time are the hardest cases. Choosing the number of speakers helps most.

## Practice

### "No sound is coming in"

The microphone is silent. Check that:

- It is connected and not muted, and its volume is up.
- It is the one your system uses as the default input.
- **Windows:** Settings, Privacy and security, Microphone allows desktop apps.
- **macOS:** System Settings, Privacy and Security, Microphone has Transcriber turned on.

### "No microphone was found"

Connect one, or press **Choose file** and use a recording made with another app.

### The recording stopped by itself

Recordings stop after 30 minutes. Start another one for the rest.

### My filler word count is 0, but I said "um"

The speech model may have left the filler out. Open **What was heard** to see what was counted. A clear recording close to the microphone helps.

### The pace looks wrong

Pace is words per minute while you are speaking, not counting long silences. A very fast conversation can show over 200. A recording with music or other voices will give odd results.

### My run was not added to progress

Only recordings of **25 words or more** are saved, so the comparison between runs is meaningful.

### "Get written feedback" is missing

It appears only when Ollama is running on your computer and has at least one model. Install Ollama, download a model, start it, and open the Practice tab again.

### Written feedback fails or takes long

The model runs on your computer, so a large model on a slow machine can take minutes. Try a smaller model.

## Still stuck

Open an issue on [GitHub](https://github.com/Eomar21/transcriber/issues) and include:

- Your system (Windows or macOS, and the version).
- The app version (shown on the [releases page](https://github.com/Eomar21/transcriber/releases)).
- What you did, what you expected and what happened.

Please do not attach recordings you want to keep private.
