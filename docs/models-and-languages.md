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

The **Transcribe** tab can detect the language automatically. You can also select the language. The app is tested with English, Dutch, Arabic and Spanish. The speech model understands many more languages.

The **Practice** tab supports **English, Dutch and Spanish**. The lists of filler words and stumbles are written for these languages.

| Function | Languages |
| --- | --- |
| Transcribe | Automatic, or select one. Tested: English, Dutch, Arabic, Spanish. |
| Speaker labels | Based on the sound of the voices, not on the words. Tested most with English and Dutch. |
| Practice measures | English, Dutch, Spanish. |

### Tips for better results

- Select the language yourself if you know it. Automatic detection looks at the first part of the speech. It can select the wrong language for a short recording or a noisy recording.
- Add names and special terms to the **Names or special terms** field.
- For a recording with more than one language, select the main language. Expect lower accuracy for the other languages.

## Speed and the graphics card

The Transcribe tab can use your graphics card (Vulkan). This is usually much faster than the processor. If no graphics card is available, the app uses the processor. The result screen shows which one the app used, for example "Processed with Vulkan". The setting is **Use graphics card when available**. A change has effect after you restart the app.
