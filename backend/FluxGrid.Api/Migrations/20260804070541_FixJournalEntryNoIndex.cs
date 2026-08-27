using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxGrid.Api.Shared.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixJournalEntryNoIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_journal_entries_EntryNo",
                table: "journal_entries");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_TenantId_EntryNo",
                table: "journal_entries",
                columns: new[] { "TenantId", "EntryNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_journal_entries_TenantId_EntryNo",
                table: "journal_entries");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_EntryNo",
                table: "journal_entries",
                column: "EntryNo",
                unique: true);
        }
    }
}
