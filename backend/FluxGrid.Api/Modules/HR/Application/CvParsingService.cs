using System.Globalization;
using System.Text.Json;
using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Audit;
using FluxGrid.Api.Shared.Infrastructure.Data;
using FluxGrid.Api.Shared.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Modules.HR.Application;

public class CvParsingService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _storage;
    private readonly AuditService _audit;
    private readonly PdfTextExtractor _pdfExtractor;
    private readonly DocxTextExtractor _docxExtractor;
    private readonly GroqApiService _groq;
    private readonly ILogger<CvParsingService> _logger;
    private readonly string _bucketName;

    public CvParsingService(
        AppDbContext db,
        IFileStorageService storage,
        AuditService audit,
        PdfTextExtractor pdfExtractor,
        DocxTextExtractor docxExtractor,
        GroqApiService groq,
        ILogger<CvParsingService> logger,
        IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _pdfExtractor = pdfExtractor;
        _docxExtractor = docxExtractor;
        _groq = groq;
        _logger = logger;
        _bucketName = config["Storage:BucketName"] ?? "fluxgrid-cvs";
    }

    public async Task ParseCandidateAsync(Guid candidateId, Guid userId, Guid tenantId,
        string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        var candidate = await _db.Candidates
            .Include(c => c.Education)
            .Include(c => c.Experience)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == candidateId && c.TenantId == tenantId, ct);

        if (candidate == null) return;
        if (candidate.Status != CandidateStatus.Draft) return;

        try
        {
            _logger.LogInformation("Starting CV parsing for candidate {Id}", candidateId);
            var objectKey = $"{tenantId}/{candidate.FileHash}/{candidate.OriginalFilename}";
            _logger.LogInformation("Reading file: bucket={Bucket}, key={Key}", _bucketName, objectKey);
            var fileBytes = await _storage.ReadFileAsync(_bucketName, objectKey);

            var rawText = ExtractText(fileBytes, candidate.FileType);
            candidate.RawText = rawText;

            _logger.LogInformation("Extracted text length: {Len}", rawText?.Length ?? 0);

            if (PdfTextExtractor.IsScannedDocument(rawText!))
            {
                _logger.LogWarning("Scanned document detected, text length={Len}", rawText?.Length ?? 0);
                candidate.Status = CandidateStatus.ParseFailed;
                await _db.SaveChangesAsync(ct);
                await _audit.LogAsync(userId, tenantId, "CV_PARSE_FAILED", "candidate",
                    candidate.Id, ipAddress, userAgent, null,
                    new { reason = "scanned_document", textLength = rawText!.Length });
                return;
            }

            _logger.LogInformation("Calling Groq API...");
            var parsed = await _groq.ParseCvTextAsync(rawText, ct);
            _logger.LogInformation("Groq response received, kind={Kind}", parsed.ValueKind);

            if (parsed.ValueKind == JsonValueKind.Undefined || parsed.ValueKind == JsonValueKind.Null ||
                !parsed.TryGetProperty("firstName", out _))
            {
                _logger.LogWarning("Groq returned invalid/empty response");
                candidate.Status = CandidateStatus.ParseFailed;
                await _db.SaveChangesAsync(ct);
                await _audit.LogAsync(userId, tenantId, "CV_PARSE_FAILED", "candidate",
                    candidate.Id, ipAddress, userAgent, null,
                    new { reason = "invalid_groq_response" });
                return;
            }

            UpdateCandidateFromParsed(candidate, parsed);
            SaveEducation(candidate, parsed);
            SaveExperience(candidate, parsed);
            SaveSkills(candidate, parsed);

            candidate.Status = CandidateStatus.Parsed;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(userId, tenantId, "CV_PARSED", "candidate",
                candidate.Id, ipAddress, userAgent, null,
                new { parsedData = parsed });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CV parsing failed for candidate {Id}", candidateId);
            candidate.Status = CandidateStatus.ParseFailed;
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(userId, tenantId, "CV_PARSE_FAILED", "candidate",
                candidate.Id, ipAddress, userAgent, null,
                new { reason = ex.Message });
        }
    }

    private string ExtractText(byte[] fileBytes, string? fileType)
    {
        var ext = fileType?.ToLower();
        if (ext == "docx")
            return _docxExtractor.ExtractText(fileBytes);
        return _pdfExtractor.ExtractText(fileBytes);
    }

    private static void UpdateCandidateFromParsed(Candidate candidate, JsonElement parsed)
    {
        if (parsed.TryGetProperty("firstName", out var fn)) candidate.Name = fn.GetString() ?? candidate.Name;
        if (parsed.TryGetProperty("email", out var em) && em.GetString() is { Length: > 0 } email)
            candidate.Email = email;
        if (parsed.TryGetProperty("phone", out var ph)) candidate.Phone = ph.GetString();
        if (parsed.TryGetProperty("linkedInUrl", out var li) && li.GetString() is { Length: > 0 } linkedIn)
            candidate.LinkedInUrl = linkedIn;
        if (parsed.TryGetProperty("githubUrl", out var gh) && gh.GetString() is { Length: > 0 } gitHub)
            candidate.GitHubUrl = gitHub;
        if (parsed.TryGetProperty("portfolioUrl", out var po) && po.GetString() is { Length: > 0 } portfolio)
            candidate.PortfolioUrl = portfolio;
        if (parsed.TryGetProperty("summary", out var sm)) candidate.Summary = sm.GetString();
    }

    private void SaveEducation(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("education", out var edu) || edu.ValueKind != JsonValueKind.Array) return;

        var items = new List<CandidateEducation>();
        foreach (var item in edu.EnumerateArray())
        {
            items.Add(new CandidateEducation
            {
                CandidateId = candidate.Id,
                Institution = item.GetProperty("institution").GetString() ?? "",
                Degree = item.GetProperty("degree").GetString() ?? "",
                FieldOfStudy = item.TryGetProperty("fieldOfStudy", out var fs) ? fs.GetString() : null,
                StartDate = TryParseDate(item, "startDate"),
                EndDate = TryParseDate(item, "endDate"),
                Gpa = ParseGpa(item)
            });
        }
        _db.CandidateEducations.AddRange(items);
    }

    private void SaveExperience(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("experience", out var exp) || exp.ValueKind != JsonValueKind.Array) return;

        var items = new List<CandidateExperience>();
        foreach (var item in exp.EnumerateArray())
        {
            items.Add(new CandidateExperience
            {
                CandidateId = candidate.Id,
                Company = item.GetProperty("company").GetString() ?? "",
                Role = item.GetProperty("role").GetString() ?? "",
                StartDate = TryParseDate(item, "startDate"),
                EndDate = TryParseDate(item, "endDate"),
                IsCurrent = item.TryGetProperty("isCurrent", out var ic) && ic.GetBoolean(),
                Description = item.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Location = item.TryGetProperty("location", out var loc) ? loc.GetString() : null
            });
        }
        _db.CandidateExperiences.AddRange(items);
    }

    private void SaveSkills(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array) return;

        var items = new List<CandidateSkill>();
        foreach (var item in skills.EnumerateArray())
        {
            if (!item.TryGetProperty("skillName", out var sn)) continue;
            items.Add(new CandidateSkill
            {
                CandidateId = candidate.Id,
                SkillName = sn.GetString() ?? "",
                SkillCategory = item.TryGetProperty("skillCategory", out var sc) ? sc.GetString() : null,
                ProficiencyLevel = item.TryGetProperty("proficiencyLevel", out var pl) ? pl.GetString() : null,
                YearsExperience = item.TryGetProperty("yearsExperience", out var ye) && ye.ValueKind == JsonValueKind.Number ? ye.GetInt32() : null
            });
        }
        _db.CandidateSkills.AddRange(items);
    }

    private static decimal? ParseGpa(JsonElement item)
    {
        if (!item.TryGetProperty("gpa", out var gpa)) return null;
        if (gpa.ValueKind == JsonValueKind.Number) return gpa.GetDecimal();
        if (gpa.ValueKind == JsonValueKind.String)
        {
            var s = gpa.GetString()?.Trim().Split('/')[0].Trim();
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
        }
        return null;
    }

    private static readonly Dictionary<string, int> IdMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["feb"] = 2, ["mar"] = 3, ["apr"] = 4, ["mei"] = 5, ["jun"] = 6,
        ["jul"] = 7, ["agu"] = 8, ["sep"] = 9, ["okt"] = 10, ["nov"] = 11, ["des"] = 12
    };

    private static DateTime? TryParseDate(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var val) || val.ValueKind != JsonValueKind.String)
            return null;
        var str = val.GetString();
        if (string.IsNullOrEmpty(str)) return null;

        // Bersihin en-dash dan karakter dash unicode lainnya
        str = str.Replace("\u2013", "-").Replace("\u2014", "-").Replace("\u2011", "-").TrimStart('-', ' ');

        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // Handle Indonesian: "1 Agu 2025"
        var parts = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 3 && int.TryParse(parts[0], out var day) && int.TryParse(parts[2], out var year))
        {
            if (IdMonths.TryGetValue(parts[1], out var month))
                return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }

        if (int.TryParse(str, out var y)) return new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return null;
    }
}
