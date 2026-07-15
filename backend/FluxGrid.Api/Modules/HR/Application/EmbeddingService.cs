using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluxGrid.Api.Modules.HR.Domain.Entities;

namespace FluxGrid.Api.Modules.HR.Application;

public partial class EmbeddingService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private const int MaxRetries = 3;
    private const int ExpectedDimensions = 1536;

    public EmbeddingService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _httpFactory = httpFactory;
        _config = config;
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var groqResult = await TryGroqEmbeddingAsync(text, ct);
        if (groqResult is not null) return groqResult;

        var openAiResult = await TryOpenAiEmbeddingAsync(text, ct);
        return openAiResult;
    }

    private async Task<float[]?> TryGroqEmbeddingAsync(string text, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("GroqApi");
        var model = _config["Groq:Model"] ?? "llama3-70b-8192";

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var payload = new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = "You are an embedding generator. Return ONLY a JSON array of 1536 floating-point numbers representing the semantic embedding of the input text. No explanation, no markdown, no code fences." },
                        new { role = "user", content = $"Generate a 1536-dimensional embedding vector for the following text. Return ONLY a JSON array of 1536 floats:\n\n{text}" }
                    },
                    temperature = 0.0,
                    max_tokens = 4000
                };

                var response = await client.PostAsync(
                    "chat/completions",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                    ct);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError)
                    {
                        if (attempt == MaxRetries) break;
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt) * Random.Shared.NextDouble()), ct);
                        continue;
                    }
                    return null;
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                var vector = ExtractEmbeddingVector(json);
                if (vector is not null) return vector;
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt) * Random.Shared.NextDouble()), ct);
            }
        }

        return null;
    }

    private async Task<float[]?> TryOpenAiEmbeddingAsync(string text, CancellationToken ct)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey)) return null;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model = "text-embedding-3-small",
                input = text
            };

            var response = await client.PostAsync(
                "https://api.openai.com/v1/embeddings",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (json.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
            {
                var embedding = data[0].GetProperty("embedding");
                var values = new List<float>();
                foreach (var item in embedding.EnumerateArray())
                    values.Add(item.GetSingle());
                return values.Count == ExpectedDimensions ? values.ToArray() : null;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static float[]? ExtractEmbeddingVector(JsonElement response)
    {
        if (!response.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return null;

        var content = choices[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content)) return null;

        var cleaned = Regex.Replace(content, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline).Trim();

        try
        {
            var array = JsonSerializer.Deserialize<float[]>(cleaned);
            if (array is not null && array.Length == ExpectedDimensions)
                return array;
        }
        catch (JsonException)
        {
        }

        return null;
    }

    public static string ComposeCandidateText(Candidate candidate)
    {
        var parts = new List<string>();

        if (candidate.Skills.Count > 0)
            parts.Add($"Skills: {string.Join(", ", candidate.Skills.Select(s => s.SkillName))}.");

        if (candidate.Experience.Count > 0)
        {
            var expTexts = candidate.Experience.Select(e =>
            {
                var years = e.StartDate.HasValue
                    ? $"{e.StartDate.Value.Year}-{(e.EndDate?.Year?.ToString() ?? "Present")}"
                    : "";
                return $"{years} {e.Role} at {e.Company}. {e.Description}".Trim();
            });
            parts.Add($"Experience: {string.Join(" ", expTexts)}");
        }

        if (candidate.Education.Count > 0)
        {
            var eduTexts = candidate.Education.Select(e =>
            {
                var field = !string.IsNullOrEmpty(e.FieldOfStudy) ? $" in {e.FieldOfStudy}" : "";
                return $"{e.Degree}{field} from {e.Institution}";
            });
            parts.Add($"Education: {string.Join(", ", eduTexts)}.");
        }

        return string.Join(" ", parts);
    }

    public static string ComposeJobText(JobPosting job)
    {
        var parts = new List<string>
        {
            $"Title: {job.Title}",
            $"Description: {job.Description}"
        };

        if (!string.IsNullOrEmpty(job.Requirements))
            parts.Add($"Requirements: {job.Requirements}");

        if (job.RequiredSkills.Length > 0)
            parts.Add($"Required Skills: {string.Join(", ", job.RequiredSkills)}");

        return string.Join(". ", parts);
    }
}
