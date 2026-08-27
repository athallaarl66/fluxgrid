using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxGrid.Api.Shared.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTenantScopedUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_ShipmentNo",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_ReceiptNo",
                table: "purchase_receipts");

            migrationBuilder.DropIndex(
                name: "IX_employees_EmployeeNo",
                table: "employees");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_TenantId_ShipmentNo",
                table: "shipments",
                columns: new[] { "TenantId", "ShipmentNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_TenantId_ReceiptNo",
                table: "purchase_receipts",
                columns: new[] { "TenantId", "ReceiptNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_TenantId_EmployeeNo",
                table: "employees",
                columns: new[] { "TenantId", "EmployeeNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_shipments_TenantId_ShipmentNo",
                table: "shipments");

            migrationBuilder.DropIndex(
                name: "IX_purchase_receipts_TenantId_ReceiptNo",
                table: "purchase_receipts");

            migrationBuilder.DropIndex(
                name: "IX_employees_TenantId_EmployeeNo",
                table: "employees");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_ShipmentNo",
                table: "shipments",
                column: "ShipmentNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_ReceiptNo",
                table: "purchase_receipts",
                column: "ReceiptNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_EmployeeNo",
                table: "employees",
                column: "EmployeeNo",
                unique: true);
        }
    }
}
