using System.Text.Json;

namespace Transcriber.Coach;

/// <summary>One saved practice run.</summary>
public sealed record PracticeSession(Guid Id, DateTimeOffset When, string Title, string Language, SpeechReport Report);

public interface IPracticeHistory
{
    /// <summary>All sessions, oldest first.</summary>
    IReadOnlyList<PracticeSession> Load();

    void Add(PracticeSession session);

    void Delete(Guid id);

    void Clear();
}

/// <summary>Keeps sessions in a small JSON file in the user's app data folder. Nothing leaves the computer.</summary>
public sealed class JsonPracticeHistory(string path) : IPracticeHistory
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly object _gate = new();

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Transcriber", "practice-history.json");

    public IReadOnlyList<PracticeSession> Load()
    {
        lock (_gate) return Read();
    }

    public void Add(PracticeSession session)
    {
        lock (_gate)
        {
            var all = Read();
            all.Add(session);
            Write(all);
        }
    }

    public void Delete(Guid id)
    {
        lock (_gate)
        {
            var all = Read();
            if (all.RemoveAll(s => s.Id == id) > 0) Write(all);
        }
    }

    public void Clear()
    {
        lock (_gate) Write([]);
    }

    // A missing or damaged file just means no history yet; it must never stop the app.
    private List<PracticeSession> Read()
    {
        try
        {
            if (!File.Exists(path)) return [];
            var sessions = JsonSerializer.Deserialize<List<PracticeSession>>(File.ReadAllText(path), Options) ?? [];
            return sessions.OrderBy(s => s.When).ToList();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private void Write(List<PracticeSession> sessions)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        // Write to a temporary file first so a crash cannot leave a half-written history behind.
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(sessions, Options));
        File.Move(temp, path, overwrite: true);
    }
}
