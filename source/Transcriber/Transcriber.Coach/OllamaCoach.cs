using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Transcriber.Coach;

public sealed class OllamaException(string message) : Exception(message);

/// <summary>
/// Optional written feedback from a model running locally in Ollama (https://ollama.com).
/// The transcript is sent only to the address given here, which defaults to this computer.
/// </summary>
public sealed class OllamaCoach(HttpClient http, string baseAddress = OllamaCoach.DefaultAddress) : ICoachFeedback
{
    public const string DefaultAddress = "http://localhost:11434";

    private readonly Uri _baseAddress = new(baseAddress.TrimEnd('/') + "/");

    /// <summary>The models installed in Ollama, or an empty list when Ollama is not running.</summary>
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync(new Uri(_baseAddress, "api/tags"), ct);
            if (!response.IsSuccessStatusCode) return [];

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return json.RootElement.TryGetProperty("models", out var models)
                ? models.EnumerateArray().Select(m => m.GetProperty("name").GetString() ?? "").Where(n => n.Length > 0).ToList()
                : [];
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException)
        {
            if (ct.IsCancellationRequested) throw;
            return [];
        }
    }

    public async Task<string> GetFeedbackAsync(string model, string transcript, SpeechReport report, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                new Uri(_baseAddress, "api/generate"),
                new { model, prompt = BuildPrompt(transcript, report), stream = false }, ct);
            if (!response.IsSuccessStatusCode)
                throw new OllamaException($"Ollama answered {(int)response.StatusCode}. Check that the model \"{model}\" is installed.");

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var text = json.RootElement.TryGetProperty("response", out var r) ? r.GetString() : null;
            return string.IsNullOrWhiteSpace(text) ? throw new OllamaException("Ollama returned an empty answer.") : text.Trim();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException)
        {
            throw new OllamaException("Could not reach Ollama. Is it running?");
        }
    }

    /// <summary>The instructions and numbers sent to the model. Public so it can be checked and tuned.</summary>
    public static string BuildPrompt(string transcript, SpeechReport report)
    {
        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine("You are a friendly, honest speaking coach. Read the transcript of a practice recording and the measurements below.");
        sb.AppendLine("Give feedback in the same language as the transcript: two things done well, up to three things to work on, and one exercise for the next attempt.");
        sb.AppendLine("Be specific, reference the speaker's actual words, keep it under 200 words, and do not repeat the numbers back.");
        sb.AppendLine();
        sb.AppendLine("Measurements:");
        sb.AppendLine(inv, $"- Pace: {report.WordsPerMinute:0} words per minute");
        sb.AppendLine(inv, $"- Filler words: {report.FillerTotal} ({report.FillersPer100Words:0.#} per 100 words){(report.Fillers.Count > 0 ? ", mostly " + string.Join(", ", report.Fillers.Take(3).Select(f => $"\"{f.Filler}\" x{f.Count}")) : "")}");
        sb.AppendLine(inv, $"- Pauses of 0.8 seconds or more: {report.Pauses.Count}, longest {report.LongestPause.TotalSeconds:0.#} seconds");
        sb.AppendLine(inv, $"- Repeated words or stumbled starts: {report.Repeats.Count}");
        sb.AppendLine();
        sb.AppendLine("Transcript:");
        sb.AppendLine(transcript);
        return sb.ToString();
    }
}

/// <summary>Written feedback from a model, kept behind an interface so the app and its tests do not depend on Ollama itself.</summary>
public interface ICoachFeedback
{
    /// <summary>The models that can give feedback; empty when the service is not running.</summary>
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);

    /// <exception cref="OllamaException">The service could not be reached or gave no answer.</exception>
    Task<string> GetFeedbackAsync(string model, string transcript, SpeechReport report, CancellationToken ct = default);
}
