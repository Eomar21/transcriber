using Transcriber.Coach.Models;

namespace Transcriber.Coach.Interfaces;

/// <summary>The saved practice runs.</summary>
public interface IPracticeHistory
{
    /// <summary>All sessions, oldest first.</summary>
    IReadOnlyList<PracticeSession> Load();

    void Add(PracticeSession session);

    void Delete(Guid id);

    void Clear();
}
