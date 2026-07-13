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
    private readonly string _bucketName;

    public CvParsingService(
        AppDbContext db,
        IFileStorageService storage,
        AuditService audit,
        PdfTextExtractor pdfExtractor,
        DocxTextExtractor docxExtractor,
        GroqApiService groq,
        IConfiguration config)
    {
        _db = db;
        _storage = storage;
        _audit = audit;
        _pdfExtractor = pdfExtractor;
        _docxExtractor = docxExtractor;
        _groq = groq;
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
            var objectKey = $"{tenantId}/{candidate.FileHash}/{candidate.OriginalFilename}";
            var fileBytes = await _storage.ReadFileAsync(_bucketName, objectKey);

            var rawText = ExtractText(fileBytes, candidate.FileType);
            candidate.RawText = rawText;

            if (PdfTextExtractor.IsScannedDocument(rawText))
            {
                candidate.Status = CandidateStatus.ParseFailed;
                await _db.SaveChangesAsync(ct);
                await _audit.LogAsync(userId, tenantId, "CV_PARSE_FAILED", "candidate",
                    candidate.Id, ipAddress, userAgent, null,
                    new { reason = "scanned_document", textLength = rawText.Length });
                return;
            }

            var parsed = await _groq.ParseCvTextAsync(rawText, ct);

            if (parsed.ValueKind == JsonValueKind.Undefined || parsed.ValueKind == JsonValueKind.Null ||
                !parsed.TryGetProperty("firstName", out _))
            {
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
        catch
        {
            candidate.Status = CandidateStatus.ParseFailed;
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(userId, tenantId, "CV_PARSE_FAILED", "candidate",
                candidate.Id, ipAddress, userAgent, null,
                new { reason = "exception" });
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
        if (parsed.TryGetProperty("summary", out var sm)) candidate.Summary = sm.GetString();
    }

    private void SaveEducation(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("education", out var edu) || edu.ValueKind != JsonValueKind.Array) return;

        _db.CandidateEducations.RemoveRange(candidate.Education);
        foreach (var item in edu.EnumerateArray())
        {
            candidate.Education.Add(new CandidateEducation
            {
                CandidateId = candidate.Id,
                Institution = item.GetProperty("institution").GetString() ?? "",
                Degree = item.GetProperty("degree").GetString() ?? "",
                FieldOfStudy = item.TryGetProperty("fieldOfStudy", out var fs) ? fs.GetString() : null,
                StartDate = TryParseDate(item, "startDate"),
                EndDate = TryParseDate(item, "endDate"),
                Gpa = item.TryGetProperty("gpa", out var gpa) && gpa.ValueKind == JsonValueKind.Number ? gpa.GetDecimal() : null
            });
        }
    }

    private void SaveExperience(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("experience", out var exp) || exp.ValueKind != JsonValueKind.Array) return;

        _db.CandidateExperiences.RemoveRange(candidate.Experience);
        foreach (var item in exp.EnumerateArray())
        {
            candidate.Experience.Add(new CandidateExperience
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
    }

    private void SaveSkills(Candidate candidate, JsonElement parsed)
    {
        if (!parsed.TryGetProperty("skills", out var skills) || skills.ValueKind != JsonValueKind.Array) return;

        _db.CandidateSkills.RemoveRange(candidate.Skills);
        foreach (var item in skills.EnumerateArray())
        {
            candidate.Skills.Add(new CandidateSkill
            {
                CandidateId = candidate.Id,
                SkillName = item.GetProperty("skillName").GetString() ?? "",
                SkillCategory = item.TryGetProperty("skillCategory", out var sc) ? sc.GetString() : null,
                ProficiencyLevel = item.TryGetProperty("proficiencyLevel", out var pl) ? pl.GetString() : null,
                YearsExperience = item.TryGetProperty("yearsExperience", out var ye) && ye.ValueKind == JsonValueKind.Number ? ye.GetInt32() : null
            });
        }
    }

    private static DateTime? TryParseDate(JsonElement item, string property)
    {
        if (!item.TryGetProperty(property, out var val) || val.ValueKind != JsonValueKind.String)
            return null;
        var str = val.GetString();
        if (string.IsNullOrEmpty(str)) return null;
        if (DateTime.TryParse(str, out var dt)) return dt;
        if (int.TryParse(str, out var year)) return new DateTime(year, 1, 1);
        return null;
    }
}
