using Whisper.net.Ggml;
using Whisper.net.Logger;
using Whisper.net;

public class WhisperService : IWhisperService
{
    private const string ModelFileName = "ggml-base.bin";
    private readonly GgmlType _ggmlType = GgmlType.Base;

    public async Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath)
    {
        // Ensure the model exists. If not, download it.
        if (!File.Exists(ModelFileName))
        {
            await DownloadModel(ModelFileName, _ggmlType);
        }

        // Optional logging from the native library
        using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Debug);

        // Create the whisper factory and processor.
        using var whisperFactory = WhisperFactory.FromPath(ModelFileName);
        using var processor = whisperFactory.CreateBuilder()
            .WithLanguage("auto")
            .Build();

        var segments = new List<TranscriptSegment>();

        // Open the audio file.
        using var fileStream = File.OpenRead(audioFilePath);

        // Process the audio and add each segment to the list.
        await foreach (var result in processor.ProcessAsync(fileStream))
        {
            segments.Add(new TranscriptSegment
            {
                Start = TimeSpan.FromSeconds(result.Start.TotalSeconds), // TODO - check if this is correct
                End = TimeSpan.FromSeconds(result.End.TotalSeconds), // TODO - check if this is correct
                Text = result.Text
            });
        }

        // Write the transcript segments to a file next to the input audio file.
        string transcriptPath = await WriteTranscriptToFileAsync(segments, audioFilePath);
        Console.WriteLine($"Transcript saved at: {transcriptPath}");

        return segments;
    }

    private async Task<string> WriteTranscriptToFileAsync(List<TranscriptSegment> segments, string audioFilePath)
    {
        // Get directory of the loaded resource. If not available, use current directory.
        string directory = Path.GetDirectoryName(audioFilePath) ?? Directory.GetCurrentDirectory();
        // Create a new file name by appending _transcript to the original file name.
        string transcriptFileName = Path.GetFileNameWithoutExtension(audioFilePath) + "_transcript.txt";
        string transcriptFilePath = Path.Combine(directory, transcriptFileName);

        var lines = new List<string>();
        foreach (var segment in segments)
        {
            lines.Add($"{segment.Start} -> {segment.End}: {segment.Text}");
        }

        await File.WriteAllLinesAsync(transcriptFilePath, lines);
        return transcriptFilePath;
    }

    private static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
        using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }
}