using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FluxGrid.Api.Modules.HR.Application;

public partial class GroqApiService
{
    private readonly HttpClient _http;
    private readonly string _model;
    private const int MaxRetries = 3;
    private const int MaxTokens = 4000;

    public GroqApiService(IHttpClientFactory httpFactory, IConfiguration config)
    {
        _http = httpFactory.CreateClient("GroqApi");
        var apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not configured");
        _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        _model = config["Groq:Model"] ?? "llama3-70b-8192";
    }

    public async Task<JsonElement> ParseCvTextAsync(string rawText, CancellationToken ct = default)
    {
        var truncated = TruncateToTokens(rawText, MaxTokens);
        var prompt = BuildPrompt(truncated);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var response = await _http.PostAsync("chat/completions", prompt, ct);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
                    return ExtractAndValidateContent(json);
                }
                if (response.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError)
                {
                    if (attempt == MaxRetries) break;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * Random.Shared.NextDouble());
                    await Task.Delay(delay, ct);
                    continue;
                }
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt) * Random.Shared.NextDouble());
                await Task.Delay(delay, ct);
            }
        }
        return JsonDocument.Parse("{}").RootElement.Clone();
    }

    private StringContent BuildPrompt(string text)
    {
        var userMessage = $@"Extract structured candidate data from this CV. Return ONLY valid JSON (no markdown, no code fences).

SCHEMA:
{{
  ""firstName"": ""..."",
  ""lastName"": ""..."",
  ""email"": ""..."",
  ""phone"": ""..."",
  ""linkedInUrl"": ""... or empty"",
  ""githubUrl"": ""... or empty"",
  ""portfolioUrl"": ""... or empty"",
  ""summary"": ""..."",
  ""experience"": [{{ ""company"": ""..."", ""role"": ""..."", ""startDate"": ""..."", ""endDate"": ""..."", ""isCurrent"": false, ""description"": ""..."", ""location"": ""..."" }}],
  ""education"": [{{ ""institution"": ""..."", ""degree"": ""..."", ""fieldOfStudy"": ""..."", ""startDate"": ""..."", ""endDate"": ""..."", ""gpa"": null }}],
  ""skills"": [{{ ""skillName"": ""..."", ""skillCategory"": null, ""proficiencyLevel"": null, ""yearsExperience"": null }}]
}}

GUIDELINES (read carefully):
- CV can be Indonesian or English. Recognize both.
- Summary: from ""Summary"", ""Objective"", ""Profil"", ""Tentang Saya"", or first paragraph.
- Phone: handle any format (+62, 08, 62-, etc.) — output digits and + only.
- LinkedIn/GitHub/Portfolio: find URLs anywhere in the CV.
- Education: recognize ""S1/S2/S3/D3"", ""Bachelor/Master"", ""SMK/SMA"", ""IPK"" = gpa.
- Dates: handle ""2019-2023"", ""2019 – 2023"", ""1 Agu 2025"", ""Aug 2025"", ""2025"". Output as-is, any format.
- Experience: recognize ""Pengalaman Kerja"", ""Work Experience"", ""Riwayat Pekerjaan"", ""Employment"".
- Skills: extract EVERY skill mentioned — from skill sections, tag clouds, or inline text. skillCategory/proficiencyLevel/yearsExperience are optional (set null).
- If a field is missing, use empty string """" or null. Never invent data.
- isCurrent: true if endDate says ""Present"", ""Sekarang"", ""Current"", or similar.

CV TEXT:
{text}";

        var body = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = "You are a CV parsing assistant. Extract structured data from CV text. Return ONLY valid JSON matching the requested schema. Never include markdown formatting or code fences." },
                new { role = "user", content = userMessage }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }

    private static JsonElement ExtractAndValidateContent(JsonElement response)
    {
        if (!response.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            return JsonDocument.Parse("{}").RootElement.Clone();

        var message = choices[0].GetProperty("message");
        var content = message.GetProperty("content").GetString() ?? "{}";

        try
        {
            return JsonDocument.Parse(content).RootElement.Clone();
        }
        catch (JsonException)
        {
            var cleaned = Regex.Replace(content, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.Multiline);
            try { return JsonDocument.Parse(cleaned).RootElement.Clone(); }
            catch { return JsonDocument.Parse("{}").RootElement.Clone(); }
        }
    }

    public static string RedactPii(string text)
    {
        var result = EmailRegex().Replace(text, "[EMAIL]");
        result = PhoneRegex().Replace(result, "[PHONE]");
        result = AddressRegex().Replace(result, "[ADDRESS]");
        return result;
    }

    public static string TruncateToTokens(string text, int maxTokens)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var approxTokens = text.Length / 4;
        if (approxTokens <= maxTokens) return text;
        return text[..(maxTokens * 4)];
    }

    [GeneratedRegex(@"\b[\w\.-]+@[\w\.-]+\.\w+\b")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(Jl\.?\s|Jalan\s|Gg\.?\s|Gang\s|Dusun\s|Ds\.?\s|Perum\s|Komplek\s|RT\s?\d|RW\s?\d).{5,80}", RegexOptions.IgnoreCase)]
    private static partial Regex AddressRegex();
}
