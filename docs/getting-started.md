---
title: Getting started
nav_order: 2
---

# Getting started
{: .no_toc }

1. TOC
{:toc}

## Download the app

Download the latest version from the [Releases page](https://github.com/Eomar21/transcriber/releases/latest).

| Platform | File | Note |
| --- | --- | --- |
| Windows 10 or 11 (64-bit) | `Transcriber-<version>-win-x64-setup.exe` | This file is the installer. |
| Windows, no installation | `Transcriber-<version>-win-x64-portable.zip` | Unzip the file. Run `Transcriber.Maui.exe`. |
| macOS, Apple Silicon | `Transcriber-<version>-mac-arm64.dmg` | Open the file. Drag Transcriber to the Applications folder. |

Each file is about 550 MB. The file contains everything the app needs: the speech model, the voice model, the speaker models and ffmpeg. For this reason, the app does not download files at first start.

{: .note }
The app does not run on Intel Macs. It runs on Apple Silicon Macs only (M1 and later).

## Install and open the app

### Windows

1. Run the installer. Or unzip the portable file.
2. Start **Transcriber**.
3. At the first start, Windows SmartScreen can show a warning. The app does not have a code signature yet. Click **More info**. Then click **Run anyway**.

### macOS

1. Open the `.dmg` file.
2. Drag **Transcriber** to the **Applications** folder.
3. At the first start, right-click the app. Select **Open**. Then confirm. The app does not have a code signature yet.
4. To record in the Practice tab, macOS asks for microphone access. Select **OK**. To change this setting later, open **System Settings**, then **Privacy and Security**, then **Microphone**.

## Make your first transcript

1. Start Transcriber. The **Transcribe** tab is open.
2. Drop an audio file or a video file on the window. Or click **Choose files**.
3. Do not change the settings.
4. Click **Transcribe**.
5. Wait for the message **Transcript saved**. The text shows below the message.

The app saves a file next to your recording. The name of the file is `<your file>_transcript.txt`.

## Make your first practice run

1. Open the **Practice** tab.
2. Click **Record**.
3. Speak for about one minute. Speak about the subject that you want to practise.
4. Click **Stop**.
5. Click **Analyse my speaking**.
6. Read the results and the tips.

## Next steps

- Read the [Transcribe guide]({{ '/transcribe' | relative_url }}) for all the settings.
- Read the [Practice guide]({{ '/practice' | relative_url }}) to see what the app measures.
