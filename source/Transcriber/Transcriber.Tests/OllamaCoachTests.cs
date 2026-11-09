using System.Net;
using System.Text;
using Transcriber.Coach.Exceptions;
using Transcriber.Coach.Feedback;
using Transcriber.Coach.Models;

/// <summary>The optional local Ollama feedback, tested against a stand-in server.</summary>
public class OllamaCoachTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Url, string Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add((request.Method, request.RequestUri!.ToString(), request.Content is null ? "" : await request.Content.ReadAsStringAsync(ct)));
            return respond(request);
        }
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode code = HttpStatusCode.OK) =>
        new(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static readonly SpeechReport Report = new()
    {
        WordCount = 100, WordsPerMinute = 150, Duration = TimeSpan.FromMinutes(1), SpeakingTime = TimeSpan.FromMinutes(1),
        Fillers = [new FillerCount("um", 5)],
    };

    private static OllamaCoach Coach(Handler handler) => new(new HttpClient(handler), new OllamaPromptBuilder());

    [Fact]
    public async Task Installed_models_are_listed()
    {
        var handler = new Handler(_ => Json("""{"models":[{"name":"llama3.2:3b"},{"name":"qwen2.5:7b"}]}"""));

        var models = await Coach(handler).ListModelsAsync();

        Assert.Equal(["llama3.2:3b", "qwen2.5:7b"], models);
        Assert.Equal("http://localhost:11434/api/tags", handler.Requests[0].Url);
    }

    [Fact]
    public async Task When_ollama_is_not_running_there_are_simply_no_models()
    {
        var handler = new Handler(_ => throw new HttpRequestException("refused"));

        Assert.Empty(await Coach(handler).ListModelsAsync());
    }

    [Fact]
    public async Task An_error_answer_or_nonsense_means_no_models()
    {
        Assert.Empty(await Coach(new Handler(_ => Json("{}", HttpStatusCode.InternalServerError))).ListModelsAsync());
        Assert.Empty(await Coach(new Handler(_ => Json("not json"))).ListModelsAsync());
    }

    [Fact]
    public async Task Cancelling_is_not_hidden_as_no_models()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new Handler(_ => throw new TaskCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Coach(handler).ListModelsAsync(cts.Token));
    }

    [Fact]
    public async Task Feedback_sends_the_model_and_a_prompt_and_returns_the_answer()
    {
        var handler = new Handler(_ => Json("""{"response":"  Nice pace, cut the ums.  "}"""));

        var text = await Coach(handler).GetFeedbackAsync("llama3.2:3b", "Hello um world", Report);

        Assert.Equal("Nice pace, cut the ums.", text);
        var (method, url, body) = handler.Requests[0];
        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal("http://localhost:11434/api/generate", url);
        Assert.Contains("\"model\":\"llama3.2:3b\"", body);
        Assert.Contains("\"stream\":false", body);
        Assert.Contains("Hello um world", body);
    }

    [Fact]
    public async Task A_failure_is_reported_in_plain_words()
    {
        var down = new Handler(_ => throw new HttpRequestException("refused"));
        var missing = new Handler(_ => Json("""{"error":"model not found"}""", HttpStatusCode.NotFound));
        var empty = new Handler(_ => Json("""{"response":"   "}"""));

        Assert.Contains("Is it running", (await Assert.ThrowsAsync<OllamaException>(() => Coach(down).GetFeedbackAsync("m", "t", Report))).Message);
        Assert.Contains("\"m\" is installed", (await Assert.ThrowsAsync<OllamaException>(() => Coach(missing).GetFeedbackAsync("m", "t", Report))).Message);
        Assert.Contains("empty", (await Assert.ThrowsAsync<OllamaException>(() => Coach(empty).GetFeedbackAsync("m", "t", Report))).Message);
    }

    [Fact]
    public void The_prompt_carries_the_measurements_and_the_transcript()
    {
        var prompt = new OllamaPromptBuilder().Build("Um, hello there.", Report);

        Assert.Contains("150 words per minute", prompt);
        Assert.Contains("\"um\" x5", prompt);
        Assert.Contains("Um, hello there.", prompt);
        Assert.Contains("same language as the transcript", prompt);
    }

    [Fact]
    public void A_custom_address_is_used()
    {
        var handler = new Handler(_ => Json("""{"models":[]}"""));

        new OllamaCoach(new HttpClient(handler), new OllamaPromptBuilder(), new OllamaOptions { BaseAddress = "http://example.test:9999/" }).ListModelsAsync().GetAwaiter().GetResult();

        Assert.Equal("http://example.test:9999/api/tags", handler.Requests[0].Url);
    }
}
