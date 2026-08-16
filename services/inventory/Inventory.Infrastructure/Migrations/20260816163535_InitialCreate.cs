using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "event_inventory_settings",
                schema: "inventory",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxTicketsPerBuyer = table.Column<int>(type: "integer", nullable: true),
                    OnSaleAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiresQueue = table.Column<bool>(type: "boolean", nullable: false),
                    SalesPaused = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_inventory_settings", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "general_admission_allocation",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogSectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalCapacity = table.Column<int>(type: "integer", nullable: false),
                    HeldCount = table.Column<int>(type: "integer", nullable: false),
                    SoldCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_general_admission_allocation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hold",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hold", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_item",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_item", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_ledger",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneralAdmissionAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Cause = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RefId = table.Column<Guid>(type: "uuid", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_ledger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hold_general_admission_item",
                schema: "inventory",
                columns: table => new
                {
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneralAdmissionAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hold_general_admission_item", x => new { x.HoldId, x.GeneralAdmissionAllocationId });
                    table.ForeignKey(
                        name: "FK_hold_general_admission_item_hold_HoldId",
                        column: x => x.HoldId,
                        principalSchema: "inventory",
                        principalTable: "hold",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hold_item",
                schema: "inventory",
                columns: table => new
                {
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hold_item", x => new { x.HoldId, x.InventoryItemId });
                    table.ForeignKey(
                        name: "FK_hold_item_hold_HoldId",
                        column: x => x.HoldId,
                        principalSchema: "inventory",
                        principalTable: "hold",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_general_admission_allocation_EventId_CatalogSectionId",
                schema: "inventory",
                table: "general_admission_allocation",
                columns: new[] { "EventId", "CatalogSectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hold_EventId_Status",
                schema: "inventory",
                table: "hold",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_hold_ExpiresAt",
                schema: "inventory",
                table: "hold",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_EventId_SeatId",
                schema: "inventory",
                table: "inventory_item",
                columns: new[] { "EventId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_EventId_Status",
                schema: "inventory",
                table: "inventory_item",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_InventoryItemId",
                schema: "inventory",
                table: "inventory_ledger",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_PublishedAt",
                schema: "inventory",
                table: "outbox",
                column: "PublishedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_inventory_settings",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "general_admission_allocation",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "hold_general_admission_item",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "hold_item",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_item",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "inventory_ledger",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "hold",
                schema: "inventory");
        }
    }
}
