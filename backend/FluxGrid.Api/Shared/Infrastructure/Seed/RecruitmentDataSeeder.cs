using FluxGrid.Api.Modules.HR.Domain.Entities;
using FluxGrid.Api.Modules.HR.Domain.Enums;
using FluxGrid.Api.Shared.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FluxGrid.Api.Shared.Infrastructure.Seed;

public static class RecruitmentDataSeeder
{
    private static DateTime U(int y, int m, int d) => new(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(AppDbContext db, Guid tenantId)
    {
        if (await db.Candidates.AnyAsync(c => c.TenantId == tenantId))
            return;

        var rng = new Random(42);
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        var adminId = admin?.Id ?? Guid.NewGuid();

        var candidates = new List<Candidate>
        {
            new()
            {
                Id = Guid.NewGuid(), Name = "Rizky Pratama", Email = "rizky.pratama@email.com", Phone = "081111111111",
                Location = "Jakarta", Summary = "Full-stack developer with 5 years experience in React and Node.js",
                TotalExperienceMonths = 60, ExpectedSalaryMin = 15_000_000, ExpectedSalaryMax = 20_000_000,
                NoticePeriodDays = 30, Status = CandidateStatus.Active, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "React", SkillCategory = "Frontend", ProficiencyLevel = "Advanced", YearsExperience = 4 },
                           new CandidateSkill { SkillName = "Node.js", SkillCategory = "Backend", ProficiencyLevel = "Advanced", YearsExperience = 4 },
                           new CandidateSkill { SkillName = "TypeScript", SkillCategory = "Language", ProficiencyLevel = "Intermediate", YearsExperience = 3 },
                           new CandidateSkill { SkillName = "PostgreSQL", SkillCategory = "Database", ProficiencyLevel = "Intermediate", YearsExperience = 3 } },
                Experience = { new CandidateExperience { Company = "TechCorp", Role = "Full-stack Developer", StartDate = U(2021, 1, 1), EndDate = null, IsCurrent = true, Description = "Built internal dashboard with React and Node.js" },
                               new CandidateExperience { Company = "StartupXYZ", Role = "Junior Developer", StartDate = U(2019, 3, 1), EndDate = U(2020, 12, 31), Description = "Maintained REST APIs and frontend components" } },
                Education = { new CandidateEducation { Institution = "Universitas Indonesia", Degree = "S.Kom", FieldOfStudy = "Computer Science", StartDate = U(2015, 8, 1), EndDate = U(2019, 6, 1), Gpa = 3.65m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Sarah Dewi", Email = "sarah.dewi@email.com", Phone = "081222222222",
                Location = "Bandung", Summary = "Frontend engineer specializing in React and UI/UX",
                TotalExperienceMonths = 48, ExpectedSalaryMin = 12_000_000, ExpectedSalaryMax = 18_000_000,
                NoticePeriodDays = 30, Status = CandidateStatus.Active, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "React", SkillCategory = "Frontend", ProficiencyLevel = "Advanced", YearsExperience = 4 },
                           new CandidateSkill { SkillName = "Next.js", SkillCategory = "Frontend", ProficiencyLevel = "Intermediate", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "CSS/Sass", SkillCategory = "Frontend", ProficiencyLevel = "Advanced", YearsExperience = 4 },
                           new CandidateSkill { SkillName = "Figma", SkillCategory = "Design", ProficiencyLevel = "Intermediate", YearsExperience = 2 } },
                Experience = { new CandidateExperience { Company = "WebStudio", Role = "Frontend Engineer", StartDate = U(2022, 3, 1), IsCurrent = true, Description = "Developed component library and design system" },
                               new CandidateExperience { Company = "Digital Agency", Role = "UI Developer", StartDate = U(2020, 1, 1), EndDate = U(2022, 2, 28), Description = "Built responsive web apps for clients" } },
                Education = { new CandidateEducation { Institution = "ITB", Degree = "S.T.", FieldOfStudy = "Informatics", StartDate = U(2016, 8, 1), EndDate = U(2020, 6, 1), Gpa = 3.50m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Andi Wijaya", Email = "andi.wijaya@email.com", Phone = "081333333333",
                Location = "Jakarta", Summary = "Backend engineer with strong .NET and cloud experience",
                TotalExperienceMonths = 72, ExpectedSalaryMin = 20_000_000, ExpectedSalaryMax = 28_000_000,
                NoticePeriodDays = 45, Status = CandidateStatus.Active, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "C#", SkillCategory = "Language", ProficiencyLevel = "Advanced", YearsExperience = 6 },
                           new CandidateSkill { SkillName = ".NET Core", SkillCategory = "Backend", ProficiencyLevel = "Advanced", YearsExperience = 5 },
                           new CandidateSkill { SkillName = "Azure", SkillCategory = "Cloud", ProficiencyLevel = "Intermediate", YearsExperience = 3 },
                           new CandidateSkill { SkillName = "SQL Server", SkillCategory = "Database", ProficiencyLevel = "Advanced", YearsExperience = 5 } },
                Experience = { new CandidateExperience { Company = "Enterprise Solutions", Role = "Senior Backend Engineer", StartDate = U(2022, 1, 1), IsCurrent = true, Description = "Architected microservices with .NET 8 and Azure" },
                               new CandidateExperience { Company = "Software House", Role = "Backend Developer", StartDate = U(2019, 6, 1), EndDate = U(2021, 12, 31), Description = "Developed REST APIs and ETL pipelines" },
                               new CandidateExperience { Company = "Tech Startup", Role = "Junior Developer", StartDate = U(2018, 1, 1), EndDate = U(2019, 5, 31), Description = "Built CRUD apps with ASP.NET MVC" } },
                Education = { new CandidateEducation { Institution = "Universitas Gadjah Mada", Degree = "S.T.", FieldOfStudy = "Computer Engineering", StartDate = U(2013, 8, 1), EndDate = U(2017, 6, 1), Gpa = 3.45m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Mega Putri", Email = "mega.putri@email.com", Phone = "081444444444",
                Location = "Surabaya", Summary = "DevOps engineer with Kubernetes and CI/CD expertise",
                TotalExperienceMonths = 36, ExpectedSalaryMin = 14_000_000, ExpectedSalaryMax = 20_000_000,
                NoticePeriodDays = 30, Status = CandidateStatus.Parsed, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "Docker", SkillCategory = "DevOps", ProficiencyLevel = "Advanced", YearsExperience = 3 },
                           new CandidateSkill { SkillName = "Kubernetes", SkillCategory = "DevOps", ProficiencyLevel = "Intermediate", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "Terraform", SkillCategory = "DevOps", ProficiencyLevel = "Intermediate", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "AWS", SkillCategory = "Cloud", ProficiencyLevel = "Intermediate", YearsExperience = 3 } },
                Experience = { new CandidateExperience { Company = "CloudHost", Role = "DevOps Engineer", StartDate = U(2021, 6, 1), IsCurrent = true, Description = "Managed K8s clusters and CI/CD pipelines" } },
                Education = { new CandidateEducation { Institution = "ITS Surabaya", Degree = "S.T.", FieldOfStudy = "Electrical Engineering", StartDate = U(2016, 8, 1), EndDate = U(2020, 6, 1), Gpa = 3.30m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Doni Kusuma", Email = "doni.kusuma@email.com", Phone = "081555555555",
                Location = "Jakarta", Summary = "Data analyst with BI and SQL expertise",
                TotalExperienceMonths = 24, ExpectedSalaryMin = 10_000_000, ExpectedSalaryMax = 15_000_000,
                NoticePeriodDays = 14, Status = CandidateStatus.Parsed, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "SQL", SkillCategory = "Database", ProficiencyLevel = "Advanced", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "Python", SkillCategory = "Language", ProficiencyLevel = "Intermediate", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "Tableau", SkillCategory = "BI", ProficiencyLevel = "Intermediate", YearsExperience = 1 },
                           new CandidateSkill { SkillName = "Excel", SkillCategory = "Productivity", ProficiencyLevel = "Advanced", YearsExperience = 3 } },
                Experience = { new CandidateExperience { Company = "DataWorks", Role = "Data Analyst", StartDate = U(2022, 6, 1), IsCurrent = true, Description = "Built dashboards and automated reports" } },
                Education = { new CandidateEducation { Institution = "Universitas Brawijaya", Degree = "S.Si", FieldOfStudy = "Statistics", StartDate = U(2017, 8, 1), EndDate = U(2021, 6, 1), Gpa = 3.55m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Indah Lestari", Email = "indah.lestari@email.com", Phone = "081666666666",
                Location = "Yogyakarta", Summary = "QA engineer with automation and manual testing experience",
                TotalExperienceMonths = 30, ExpectedSalaryMin = 9_000_000, ExpectedSalaryMax = 14_000_000,
                NoticePeriodDays = 21, Status = CandidateStatus.Active, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "Selenium", SkillCategory = "Testing", ProficiencyLevel = "Advanced", YearsExperience = 2 },
                           new CandidateSkill { SkillName = "Cypress", SkillCategory = "Testing", ProficiencyLevel = "Intermediate", YearsExperience = 1 },
                           new CandidateSkill { SkillName = "Postman", SkillCategory = "Testing", ProficiencyLevel = "Advanced", YearsExperience = 3 },
                           new CandidateSkill { SkillName = "JIRA", SkillCategory = "Project Mgmt", ProficiencyLevel = "Intermediate", YearsExperience = 3 } },
                Experience = { new CandidateExperience { Company = "QualityFirst", Role = "QA Engineer", StartDate = U(2022, 1, 1), IsCurrent = true, Description = "Automated E2E tests and managed bug tracking" },
                               new CandidateExperience { Company = "StartupHQ", Role = "Junior QA", StartDate = U(2021, 3, 1), EndDate = U(2021, 12, 31), Description = "Manual testing and test documentation" } },
                Education = { new CandidateEducation { Institution = "Universitas Diponegoro", Degree = "S.Kom", FieldOfStudy = "Information Systems", StartDate = U(2016, 8, 1), EndDate = U(2020, 6, 1), Gpa = 3.40m } }
            },
            new()
            {
                Id = Guid.NewGuid(), Name = "Fajar Nugraha", Email = "fajar.nugraha@email.com", Phone = "081777777777",
                Location = "Bandung", Summary = "Backend developer with experience in building scalable APIs",
                TotalExperienceMonths = 18, ExpectedSalaryMin = 8_000_000, ExpectedSalaryMax = 12_000_000,
                NoticePeriodDays = 14, Status = CandidateStatus.Rejected, TenantId = tenantId, UploadedBy = adminId,
                Skills = { new CandidateSkill { SkillName = "Go", SkillCategory = "Language", ProficiencyLevel = "Intermediate", YearsExperience = 1 },
                           new CandidateSkill { SkillName = "PostgreSQL", SkillCategory = "Database", ProficiencyLevel = "Intermediate", YearsExperience = 1 },
                           new CandidateSkill { SkillName = "Redis", SkillCategory = "Cache", ProficiencyLevel = "Beginner", YearsExperience = 1 } },
                Experience = { new CandidateExperience { Company = "TechLab", Role = "Backend Developer", StartDate = U(2023, 1, 1), IsCurrent = true, Description = "Built REST APIs with Go and PostgreSQL" } },
                Education = { new CandidateEducation { Institution = "Telkom University", Degree = "S.T.", FieldOfStudy = "Software Engineering", StartDate = U(2018, 8, 1), EndDate = U(2022, 6, 1), Gpa = 3.25m } }
            },
        };

        db.Candidates.AddRange(candidates);
        await db.SaveChangesAsync();

        if (!await db.JobPostings.AnyAsync(j => j.TenantId == tenantId))
        {
            var jobPostings = new List<JobPosting>
            {
                new()
                {
                    Id = Guid.NewGuid(), Title = "Senior Full-stack Developer", Description = "We are looking for a senior full-stack developer to lead our core product team. You will work on both frontend and backend systems, architect solutions, and mentor junior developers.",
                    Requirements = "5+ years of experience in full-stack development. Strong problem-solving skills. Experience with React and Node.js is required.", RequiredSkills = ["React", "Node.js", "TypeScript", "PostgreSQL", "AWS"],
                    MinExperienceYears = 5, MaxExperienceYears = 10, Location = "Jakarta", SalaryMin = 25_000_000, SalaryMax = 40_000_000,
                    Status = JobPostingStatus.Published, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-14), UpdatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Frontend Engineer — React", Description = "Join our frontend team to build beautiful, responsive web applications using React and Next.js. You will collaborate closely with designers and backend engineers.",
                    Requirements = "3+ years of React experience. Proficient in TypeScript and modern CSS. Experience with Next.js is a plus.", RequiredSkills = ["React", "TypeScript", "Next.js", "CSS", "Figma"],
                    MinExperienceYears = 3, MaxExperienceYears = 7, Location = "Bandung", SalaryMin = 15_000_000, SalaryMax = 25_000_000,
                    Status = JobPostingStatus.Published, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-7), UpdatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Backend .NET Developer", Description = "Build and maintain high-performance backend services using .NET 8 and Azure. You will design APIs, optimize database queries, and ensure system reliability.",
                    Requirements = "4+ years of experience with C# and .NET. Experience with Entity Framework Core, REST APIs, and SQL databases. Azure experience preferred.", RequiredSkills = ["C#", ".NET Core", "Azure", "SQL Server", "REST APIs"],
                    MinExperienceYears = 4, MaxExperienceYears = 8, Location = "Jakarta", SalaryMin = 18_000_000, SalaryMax = 30_000_000,
                    Status = JobPostingStatus.Draft, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-3), UpdatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(), Title = "DevOps Engineer", Description = "Manage and improve our cloud infrastructure. You will maintain Kubernetes clusters, CI/CD pipelines, and monitoring systems.",
                    Requirements = "3+ years of DevOps experience. Hands-on with Docker, Kubernetes, and Terraform. AWS or Azure certification is a plus.", RequiredSkills = ["Docker", "Kubernetes", "Terraform", "AWS", "CI/CD"],
                    MinExperienceYears = 3, MaxExperienceYears = 6, Location = "Surabaya", SalaryMin = 16_000_000, SalaryMax = 28_000_000,
                    Status = JobPostingStatus.Published, TenantId = tenantId, CreatedAt = DateTime.UtcNow.AddDays(-10), UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
            };

            foreach (var job in jobPostings)
            {
                if (job.Status == JobPostingStatus.Published)
                {
                    var embedding = new float[1536];
                    for (var i = 0; i < 1536; i++)
                        embedding[i] = (float)(rng.NextDouble() * 2 - 1);
                    job.Embedding = embedding;
                }
            }

            db.JobPostings.AddRange(jobPostings);
            await db.SaveChangesAsync();

            // Assign fake embeddings to ACTIVE candidates for demo matching
            foreach (var candidate in candidates.Where(c => c.Status == CandidateStatus.Active))
            {
                var embedding = new float[1536];
                for (var i = 0; i < 1536; i++)
                    embedding[i] = (float)(rng.NextDouble() * 2 - 1);
                candidate.Embedding = embedding;
                candidate.EmbeddingStatus = null;
            }

            // Also give embeddings to PARSED candidates with a different random seed for variety
            foreach (var candidate in candidates.Where(c => c.Status == CandidateStatus.Parsed))
            {
                var embedding = new float[1536];
                for (var i = 0; i < 1536; i++)
                    embedding[i] = (float)(rng.NextDouble() * 2 - 1);
                candidate.Embedding = embedding;
                candidate.EmbeddingStatus = null;
            }

            await db.SaveChangesAsync();
        }
    }
}
