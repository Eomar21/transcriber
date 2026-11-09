---
title: Getting started
nav_order: 2
---

# Getting started
{: .no_toc }

1. TOC
{:toc}

## Download

Get the latest build from the [Releases page](https://github.com/Eomar21/transcriber/releases/latest).

| Platform | File | Notes |
| --- | --- | --- |
| Windows 10 or 11 (64-bit) | `Transcriber-<version>-win-x64-setup.exe` | The installer. |
| Windows, no installation | `Transcriber-<version>-win-x64-portable.zip` | Unzip it anywhere and run `Transcriber.Maui.exe`. |
| macOS, Apple Silicon | `Transcriber-<version>-mac-arm64.dmg` | Open the image and drag Transcriber to Applications. |

The downloads are large, about 550 MB, because everything is included: the speech model, the voice and speaker models and ffmpeg. This is why nothing is downloaded when you first start the app.

{: .note }
Intel Macs are not supported. The macOS build is for Apple Silicon (M1 and later) only.

## Install and open

### Windows

1. Run the installer, or unzip the portable version.
2. Start **Transcriber**.
3. The first time, Windows SmartScreen may say it protected your PC. The app is not code signed yet. Choose **More info**, then **Run anyway**.

### macOS

1. Open the `.dmg` and drag **Transcriber** to **Applications**.
2. The first time, right-click the app and choose **Open**, then confirm. This is needed because the app is not code signed yet.
3. If you use the Practice tab to record, macOS asks for microphone access. Choose **OK**. You can change it later in **System Settings, Privacy and Security, Microphone**.

## Your first transcript

1. Start Transcriber. You are on the **Transcribe** tab.
2. Drop an audio or video file onto the window, or press **Choose files**.
3. Leave the settings as they are for now.
4. Press **Transcribe**.
5. When it says **Transcript saved**, the text appears below, and a file named `<your file>_transcript.txt` is saved next to your recording.

## Your first practice run

1. Open the **Practice** tab.
2. Press **Record**, say a minute or so of what you would like to practise, and press **Stop**.
3. Press **Analyse my speaking**.
4. Read the results and the tips.

## What next

- [Transcribe guide]({{ '/transcribe' | relative_url }}) for every setting
- [Practice guide]({{ '/practice' | relative_url }}) for what is measured and how to read it
