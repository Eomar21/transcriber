# Transcriber

Desktop app that transcribes audio to timestamped text locally, using OpenAI Whisper through Whisper.net. Built with .NET MAUI. Work in progress.

## How it works

`WhisperService` loads the `ggml-base.bin` Whisper model (downloaded on first run) and detects the spoken language automatically. Each recognised segment is returned with a start time, end time and text. The transcript is written next to the input file as `<name>_transcript.txt`, one `start -> end: text` line per segment. MP3 and WAV input is passed straight to the model.

## Structure

`source/Transcriber/Transcriber.Services` is a net8.0 class library with the Whisper, audio, file and video services behind interfaces.

`source/Transcriber/Transcriber.Maui` is the .NET MAUI front end.

`.github/workflows/max-installer.yml` is a GitHub Actions workflow that builds the Mac Catalyst app on every push to master.

## Known gaps

Segment timestamp conversion is marked TODO in the code and still needs verification. There are no automated tests yet.
