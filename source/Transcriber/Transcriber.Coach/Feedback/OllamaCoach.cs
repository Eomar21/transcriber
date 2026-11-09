using System.Net.Http.Json;
using System.Text.Json;
using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Interfaces;
using Transcriber.Coach.Models;

namespace Transcriber.Coach.Feedback;

/// <summary>
/// Optional written feedback from a model running locally in Ollama (https://ollama.com).
/// The transcript is sent only to the address in the options, which defaults to this computer.
/// </summary>
public sealed class OllamaCoach(HttpClient http, IFeedbackPromptBuilder prompts, OllamaOptions? options = null) : ICoachFeedback
{
    private readonly Uri _baseAddress = new((options ?? new OllamaOptions()).BaseAddress.TrimEnd('/') + "/");

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
                new { model, prompt = prompts.Build(transcript, report), stream = false }, ct);
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
}
