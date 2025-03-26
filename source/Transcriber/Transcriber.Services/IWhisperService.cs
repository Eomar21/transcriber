public interface IWhisperService
{
    Task<List<TranscriptSegment>> TranscribeAsync(string audioFilePath);
}