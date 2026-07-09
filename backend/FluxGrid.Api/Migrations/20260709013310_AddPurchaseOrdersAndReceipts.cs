using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluxGrid.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrdersAndReceipts : Migration
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
                name: "purchase_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PoDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PoReference = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReceivedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipts", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderedQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_inventory_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PoId",
                        column: x => x.PoId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipt_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderedQty = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyReceived = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyPassed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QtyFailed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PutawayLocId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_receipt_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_inventory_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_locations_PutawayLocId",
                        column: x => x.PutawayLocId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_purchase_receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "purchase_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_purchase_order_lines_ItemId",
                table: "purchase_order_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PoId",
                table: "purchase_order_lines",
                column: "PoId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId",
                table: "purchase_orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId_PoNumber",
                table: "purchase_orders",
                columns: new[] { "TenantId", "PoNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipt_lines_ItemId",
                table: "purchase_receipt_lines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipt_lines_PutawayLocId",
                table: "purchase_receipt_lines",
                column: "PutawayLocId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipt_lines_ReceiptId",
                table: "purchase_receipt_lines",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_ReceiptNo",
                table: "purchase_receipts",
                column: "ReceiptNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_receipts_TenantId",
                table: "purchase_receipts",
                column: "TenantId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "purchase_receipt_lines");

            migrationBuilder.DropTable(
                name: "stock_ledger");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "purchase_receipts");
        }
    }
}
