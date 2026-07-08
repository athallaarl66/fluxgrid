using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWmsStockLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BalanceQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BalanceValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_balances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Uom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stock_ledger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_ledger", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_ItemId_LocationId_TenantId",
                table: "inventory_balances",
                columns: new[] { "ItemId", "LocationId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_balances_TenantId",
                table: "inventory_balances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_TenantId",
                table: "inventory_items",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_TenantId_Sku",
                table: "inventory_items",
                columns: new[] { "TenantId", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locations_TenantId",
                table: "locations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_TenantId_Code",
                table: "locations",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "item_loc_idx",
                table: "stock_ledger",
                columns: new[] { "ItemId", "LocationId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_TenantId",
                table: "stock_ledger",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_TransactionId",
                table: "stock_ledger",
                column: "TransactionId");

            // Enable RLS on tenant-isolated tables
            migrationBuilder.Sql("ALTER TABLE stock_ledger ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE inventory_balances ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE inventory_items ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE locations ENABLE ROW LEVEL SECURITY;");

            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_stock_ledger ON stock_ledger
                    USING (tenant_id = current_setting('app.tenant_id')::uuid);");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_inventory_balances ON inventory_balances
                    USING (tenant_id = current_setting('app.tenant_id')::uuid);");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_inventory_items ON inventory_items
                    USING (tenant_id = current_setting('app.tenant_id')::uuid);");
            migrationBuilder.Sql(@"
                CREATE POLICY tenant_isolation_locations ON locations
                    USING (tenant_id = current_setting('app.tenant_id')::uuid);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_stock_ledger ON stock_ledger;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_inventory_balances ON inventory_balances;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_inventory_items ON inventory_items;");
            migrationBuilder.Sql("DROP POLICY IF EXISTS tenant_isolation_locations ON locations;");

            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "stock_ledger");
        }
    }
}
