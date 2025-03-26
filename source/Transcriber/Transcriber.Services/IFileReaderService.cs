// Service that chooses the right reader based on file type.
public interface IFileReaderService
{
    Task<string> GetAudioFileFromFileAsync(string filePath);
}