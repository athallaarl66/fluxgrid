using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPostingAndEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double[]>(
                name: "Embedding",
                table: "candidates",
                type: "double precision[]",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingStatus",
                table: "candidates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "job_postings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Requirements = table.Column<string>(type: "text", nullable: true),
                    RequiredSkills = table.Column<string[]>(type: "text[]", nullable: false),
                    MinExperienceYears = table.Column<int>(type: "integer", nullable: true),
                    MaxExperienceYears = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SalaryMin = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    SalaryMax = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "DRAFT"),
                    Embedding = table.Column<double[]>(type: "double precision[]", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_postings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_Status",
                table: "job_postings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_TenantId_Status",
                table: "job_postings",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_postings");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "candidates");

            migrationBuilder.DropColumn(
                name: "EmbeddingStatus",
                table: "candidates");
        }
    }
}
