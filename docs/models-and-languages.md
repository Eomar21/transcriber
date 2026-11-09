---
title: Models and languages
nav_order: 7
---

# Models and languages
{: .no_toc }

1. TOC
{:toc}

## Models

The speech model sets the accuracy of the transcript and the time that the app needs. A larger model makes fewer mistakes. It is also slower and uses more disk space.

| Model | Size | Note |
| --- | --- | --- |
| **Base** (fast, lowest accuracy) | about 148 MB | This model is the fastest. Use it for clear and simple speech. |
| **Small** (balanced) | about 488 MB | **This is the default model. It is in the app.** |
| **Small, compressed** | about 190 MB | The file is smaller. The accuracy is almost the same as Small. |
| **Medium, compressed** (better) | about 539 MB | This model is more accurate for difficult audio. It is slower. |
| **Large v3 turbo, compressed** (best) | about 574 MB | This is the most accurate model in the list. |

The app downloads each model, other than the default model, the first time that you select it. The app keeps the model on your computer. After that, the model works offline. The model list shows which models are installed. The default model and everything else that the app needs are already in the app.

On Windows, the app stores downloaded models in `%LOCALAPPDATA%\Transcriber\models`.

### Which model to select

- **Clear recording, fast result:** Small (the default).
- **Names, accents, noise or many speakers:** Medium or Large v3 turbo.
- **Old computer or very long files:** Base or Small compressed.

The Practice tab always uses **Small**. The results of the Practice tab do not depend on the model that you set for the Transcribe tab.

## Languages

The app shows only the languages that the selected model transcribes well. The list changes when you select a different model.

| Model | Languages in the list |
| --- | --- |
| Base | English, Spanish |
| Small (default) and Small compressed | English, Spanish, Portuguese, Italian, German, Dutch |
| Medium and Large v3 turbo | The languages above, and French, Polish, Turkish, Indonesian, Swedish and Ukrainian |

Each list also has **Auto-detect**. The app does not show a language when the model makes too many mistakes in it.

### Dutch with the Small model

The Small model makes more mistakes in Dutch than in the other languages of its list. For this reason, the app shows a hint:

- The picker shows **Dutch (Nederlands) - better with Medium**.
- A message shows under the picker. It tells you to use the Medium model or a larger one.
- The button **Use the Medium model** selects the Medium model for you. The app asks before it starts the download.
- After the transcript is ready, the same hint shows with the result.

The hint does not show for the Medium model or the Large v3 turbo model.

### Practice languages

The **Practice** tab supports **English, Spanish, Portuguese, Italian, German and Dutch**. Each language has its own list of filler words and its own list of words that can repeat for emphasis. The Practice tab always uses the Small model. For this reason, a hint shows under the picker when you select Dutch. In Dutch, the counts of filler words and pauses can be less correct.

| Function | Languages |
| --- | --- |
| Transcribe | Automatic, or select one from the list for the model. |
| Speaker labels | Based on the sound of the voices, not on the words. Tested most with English and Dutch. |
| Practice measures | English, Spanish, Portuguese, Italian, German, Dutch. |

### Recordings with more than one language

Auto-detect selects one language for each part of the speech. If one part has two languages, the model can write only one of them. The other language is then missing from the transcript.

The app finds this problem when a large part of the speech has no text. Then the result shows a message that starts with **Some speech may be missing**.

To get a better result:

1. Select the main language. Do not use **Auto-detect**.
2. Use a larger model.
3. Transcribe the recording again.

### Tips for better results

- Select the language yourself if you know it. Automatic detection can select the wrong language for a short recording or a noisy recording.
- Add names and special terms to the **Names or special terms** field.
- For a recording with more than one language, select the main language. Expect lower accuracy for the other languages.

## Speed and the graphics card

The Transcribe tab can use your graphics card (Vulkan). This is usually much faster than the processor. If no graphics card is available, the app uses the processor. The result screen shows which one the app used, for example "Processed with Vulkan". The setting is **Use graphics card when available**. A change has effect after you restart the app.
