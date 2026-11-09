---
title: Models and languages
nav_order: 7
---

# Models and languages
{: .no_toc }

1. TOC
{:toc}

## Models

The speech model decides how accurate the transcript is and how long it takes. Bigger models make fewer mistakes but are slower and larger.

| Model | Size | Notes |
| --- | --- | --- |
| **Base** (fast, lowest accuracy) | about 148 MB | Fastest. Fine for clear, simple speech. |
| **Small** (balanced) | about 488 MB | **The default, and already included in the app.** |
| **Small, compressed** | about 190 MB | A smaller file with accuracy close to Small. |
| **Medium, compressed** (better) | about 539 MB | More accurate on difficult audio. Slower. |
| **Large v3 turbo, compressed** (best) | about 574 MB | The most accurate choice offered. |

The models other than the default are downloaded the first time you choose them, and then kept on your computer, so they work offline afterwards. The model list shows which ones are installed. Everything else needed to run, including the default model, is already inside the app.

Downloaded models are stored in `%LOCALAPPDATA%\Transcriber\models` on Windows.

### Which should I pick

- **Clear recording, quick result:** Small (the default).
- **Names, accents, noise or several speakers:** Medium or Large v3 turbo.
- **Older computer or very long files:** Base or Small compressed.

The Practice tab always uses **Small**, so its results do not depend on your Transcribe setting.

## Languages

**Transcribe** supports automatic language detection, or you can choose the language. It has been tested with English, Dutch, Arabic and Spanish, and the speech model understands many more.

**Practice** supports **English, Dutch and Spanish**, because the filler word and stumble lists are written for those languages.

| Feature | Languages |
| --- | --- |
| Transcribe | Automatic, or choose. Tested: English, Dutch, Arabic, Spanish. |
| Speaker labels | Based on how voices sound, not on the words. Tested most on English and Dutch. |
| Practice measures | English, Dutch, Spanish. |

### Tips for better results

- Choose the language yourself when you know it. Automatic detection looks at the first stretch of speech and can guess wrong on short or noisy recordings.
- Add names and special terms to the **Names or special terms** field.
- For mixed-language recordings, choose the main language and expect lower accuracy for the rest.

## Speed and your graphics card

Transcribe can use your graphics card through Vulkan, which is usually much faster than the processor. If no graphics card is available, it switches to the processor on its own. The result screen tells you what was used, for example "Processed with Vulkan". The switch is under **Use graphics card when available**, and a change applies after you restart the app.
