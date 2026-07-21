using FluxGrid.Api.Modules.HR.Application;
using FluxGrid.Api.Modules.HR.Domain.Entities;

namespace FluxGrid.Api.Tests.HR;

public class EmbeddingServiceTests
{
    [Fact]
    public void ComposeCandidateText_IncludesSkills()
    {
        var c = new Candidate
        {
            Skills = [new CandidateSkill { SkillName = "C#" }, new CandidateSkill { SkillName = "React" }]
        };
        var result = EmbeddingService.ComposeCandidateText(c);
        Assert.Contains("Skills: C#, React.", result);
    }

    [Fact]
    public void ComposeCandidateText_IncludesExperience()
    {
        var c = new Candidate
        {
            Experience =
            [
                new CandidateExperience
                {
                    Role = "Developer", Company = "Acme",
                    StartDate = new DateTime(2020, 1, 1),
                    Description = "Built stuff"
                }
            ]
        };
        var result = EmbeddingService.ComposeCandidateText(c);
        Assert.Contains("2020-Present Developer at Acme. Built stuff", result);
    }

    [Fact]
    public void ComposeCandidateText_IncludesEducation()
    {
        var c = new Candidate
        {
            Education =
            [
                new CandidateEducation { Degree = "BSc", FieldOfStudy = "CS", Institution = "MIT" }
            ]
        };
        var result = EmbeddingService.ComposeCandidateText(c);
        Assert.Contains("Education: BSc in CS from MIT.", result);
    }

    [Fact]
    public void ComposeCandidateText_ReturnsEmptyStringWhenNoData()
    {
        var c = new Candidate();
        var result = EmbeddingService.ComposeCandidateText(c);
        Assert.Equal("", result);
    }

    [Fact]
    public void ComposeCandidateText_DoesNotIncludePiiFields()
    {
        var c = new Candidate
        {
            Name = "John Doe",
            Email = "john@test.com",
            Phone = "1234567890",
            Skills = [new CandidateSkill { SkillName = "Python" }]
        };
        var result = EmbeddingService.ComposeCandidateText(c);
        Assert.Contains("Skills: Python.", result);
        Assert.DoesNotContain("John Doe", result);
        Assert.DoesNotContain("john@test.com", result);
        Assert.DoesNotContain("1234567890", result);
    }

    [Fact]
    public void ComposeJobText_IncludesTitleAndDescription()
    {
        var j = new JobPosting { Title = "Engineer", Description = "Build things" };
        var result = EmbeddingService.ComposeJobText(j);
        Assert.Contains("Title: Engineer", result);
        Assert.Contains("Description: Build things", result);
    }

    [Fact]
    public void ComposeJobText_IncludesRequirements()
    {
        var j = new JobPosting { Title = "T", Description = "D", Requirements = "5yr exp" };
        var result = EmbeddingService.ComposeJobText(j);
        Assert.Contains("Requirements: 5yr exp", result);
    }

    [Fact]
    public void ComposeJobText_IncludesRequiredSkills()
    {
        var j = new JobPosting { Title = "T", Description = "D", RequiredSkills = ["C#", "SQL"] };
        var result = EmbeddingService.ComposeJobText(j);
        Assert.Contains("Required Skills: C#, SQL", result);
    }

    [Fact]
    public void ComposeJobText_OmitsOptionalFieldsWhenNull()
    {
        var j = new JobPosting { Title = "T", Description = "D" };
        var result = EmbeddingService.ComposeJobText(j);
        Assert.DoesNotContain("Requirements", result);
    }
}
