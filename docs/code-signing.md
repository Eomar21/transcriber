---
title: Code signing policy
nav_order: 12
---

# Code signing policy
{: .no_toc }

1. TOC
{:toc}

This page describes how the project signs the Windows downloads. A signature shows who made a file. It also shows that nobody changed the file after it was signed.

## Status

The signing service is not active yet. The releases that exist now are not signed. Windows can show a warning when you run an unsigned installer. This page applies when the signing service starts.

## Who signs

Free code signing is provided by [SignPath.io](https://signpath.io). The certificate is from the [SignPath Foundation](https://signpath.org).

## Roles

| Role | Person |
| --- | --- |
| Author of the code | [Eomar21](https://github.com/Eomar21) |
| Reviewer of changes | [Eomar21](https://github.com/Eomar21) |
| Approver of each signing request | [Eomar21](https://github.com/Eomar21) |

## Rules

- The project signs only files that GitHub Actions builds from this repository. A person does not sign files by hand.
- The build starts when a version tag is pushed. The tag has the form `v1.2.3`.
- The approver must approve each signing request in SignPath. Without the approval, the file stays unsigned.
- The project signs only its own files: the app and the installer.
- The project does not sign the programs of other projects that are in the download. Examples are ffmpeg and the speaker detection program of sherpa-onnx (`sherpa-onnx-offline-speaker-diarization.exe`), which contains ONNX Runtime. These programs are in the download as their projects released them. Their licences are in the [README](https://github.com/Eomar21/transcriber#licences-and-notices).

## Privacy

The app does all its work on your computer. It sends no audio and no text to a server. Refer to [Privacy]({{ '/privacy' | relative_url }}).

## Check a signature

1. Open the folder that has the downloaded file.
2. Right-click the file. Click **Properties**.
3. Click the **Digital Signatures** tab. If the tab is not there, the file is not signed.
4. Select the signature. Click **Details**.
5. Make sure that the message says "This digital signature is OK".

You can also use PowerShell. Type this command:

```powershell
Get-AuthenticodeSignature .\Transcriber-setup.exe | Format-List Status, SignerCertificate
```

The value of `Status` must be `Valid`.
