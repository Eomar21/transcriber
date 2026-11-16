# Demo video tools

These tools make the demo video of the app. The video shows the real app. Code draws the title card, the captions, the zoom and the pointer.

## How it works

1. The UI test `DemoVideoTests` starts the app. The test moves the real mouse and records the window with ffmpeg.
2. The test writes `timeline.json`. The file lists the clicks, the pointer path, the zoom targets, the captions and the parts to cut or to speed up.
3. `compose.cs` reads the recording and the timeline. It draws each frame and sends the frames to ffmpeg.
4. `music.cs` makes the music. The program makes the sound from sine waves and noise. The music has no third-party licence.

## Make the video

The steps are for Windows. ffmpeg must be available. The app build puts a copy in `source/Transcriber/Transcriber.Maui/obj/ffmpeg`.

1. Build the app in the Debug configuration.
2. Do not touch the mouse during the next step. The test moves the mouse for about two minutes.
3. Set the environment variables, then run the test.

   ```powershell
   $env:TRANSCRIBER_UI_TESTS = "1"
   $env:TRANSCRIBER_DEMO_VIDEO = "1"
   $env:FFMPEG_EXE = "<path of ffmpeg.exe>"
   $env:DEMO_VIDEO_OUT = "<empty folder>"
   dotnet test source/Transcriber/Transcriber.UiTests --filter "FullyQualifiedName~DemoVideoTests"
   ```

4. Find the length of the video.

   ```powershell
   dotnet run tools/demo-video/compose.cs -- "$env:DEMO_VIDEO_OUT/capture.mkv" "$env:DEMO_VIDEO_OUT/timeline.json" demo.mp4 --length
   ```

5. Make the music. Use the length from step 4, and add two seconds.

   ```powershell
   dotnet run tools/demo-video/music.cs -- 66 "$env:DEMO_VIDEO_OUT/music.wav"
   ```

6. Make the video.

   ```powershell
   dotnet run tools/demo-video/compose.cs -- "$env:DEMO_VIDEO_OUT/capture.mkv" "$env:DEMO_VIDEO_OUT/timeline.json" demo.mp4 "$env:DEMO_VIDEO_OUT/music.wav"
   ```

To check the look without a full render, add `--stills 3,10,25` to the command. The program writes one picture for each time.

## Change the video

- To change the scenes or the captions, edit `DemoVideoTests.cs`. Use `Say` for a caption and `ZoomTo` for a zoom.
- To change the look, edit `compose.cs`. The colours and the fonts are at the top.
- To change the music, edit the tempo, the chords and the mix in `music.cs`.
