---
title: Settings file
nav_order: 11
---

# Settings file
{: .no_toc }

1. TOC
{:toc}

The app has default settings. These settings are in the app. You can change a setting if you put it in a settings file.

Most people do not need this page. Use it if you want to change a model, a language or a limit.

## Where the settings are

| File | Purpose |
| --- | --- |
| `appsettings.json` | The default settings. This file is in the app. Do not change it. |
| `appsettings.user.json` | Your settings. You make this file. The app reads it after the default settings. |

The app finds your file in this folder:

| System | Folder |
| --- | --- |
| Windows | `%LOCALAPPDATA%\Transcriber` |
| macOS | `~/.local/share/Transcriber` |

If the file does not exist, the app uses the default settings only.

## Change a setting

1. Open the folder in the table above. Make the folder if it does not exist.
2. Make a file with the name `appsettings.user.json`.
3. Write only the settings that you want to change.
4. Save the file.
5. Close the app and open it again.

The app reads the settings one time, when it starts.

### Example

This file changes the default model and the limit for speech detection.

```json
{
  "Models": {
    "DefaultModelId": "medium-q5_0"
  },
  "Transcription": {
    "NoSpeechThreshold": 0.5
  }
}
```

All other settings stay the same.

## Settings that you can change

| Setting | Meaning |
| --- | --- |
| `Models:DefaultModelId` | The model that the Transcribe tab selects at start. |
| `Models:SuggestedModelId` | The model that the app suggests when a result can be more accurate. |
| `Models:PracticeModelId` | The model that the Practice tab uses. |
| `Models:Directory` | The folder where the app keeps downloaded models. |
| `Transcription:NoSpeechThreshold` | A number between 0 and 1. A lower number keeps more quiet speech. |

The model identifiers are in the `Models:Catalog` list of the default file. Use only an identifier that is in that list.

{: .note }
Write a list in your file only if you must. The app combines lists by position. A short list in your file does not remove the extra items from the default list.

## If the settings are not correct

The app checks all settings when it starts. If a setting cannot work, the app does not start. The error names the setting. Examples:

- `Models:DefaultModelId 'x' is not in Models:Catalog.`
- `Transcription:NoSpeechThreshold must be between 0 and 1.`

If the file has a JSON error, the error says that the settings cannot be read.

To correct the problem:

1. Open `appsettings.user.json`.
2. Correct the value, or remove the setting.
3. Save the file and open the app again.

To use the default settings again, delete `appsettings.user.json`.
