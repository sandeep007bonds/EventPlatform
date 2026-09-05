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
                name: "dead_letters",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "general_admission_allocation",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmissionAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalCapacity = table.Column<int>(type: "integer", nullable: false),
                    HeldCount = table.Column<int>(type: "integer", nullable: false),
                    SoldCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    At = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "session_inventory_settings",
                schema: "inventory",
                columns: table => new
                {
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxTicketsPerBuyer = table.Column<int>(type: "integer", nullable: true),
                    OnSaleAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiresQueue = table.Column<bool>(type: "boolean", nullable: false),
                    SalesPaused = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_inventory_settings", x => x.EventSessionId);
                });

            migrationBuilder.CreateTable(
                name: "hold_general_admission_item",
                schema: "inventory",
                columns: table => new
                {
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneralAdmissionAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
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
                name: "IX_dead_letters_CorrelationId",
                schema: "inventory",
                table: "dead_letters",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letters_MessageId",
                schema: "inventory",
                table: "dead_letters",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letters_ResolvedAt",
                schema: "inventory",
                table: "dead_letters",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_general_admission_allocation_EventSessionId_AdmissionAreaId~",
                schema: "inventory",
                table: "general_admission_allocation",
                columns: new[] { "EventSessionId", "AdmissionAreaId", "TicketTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_hold_CatalogEventId_UserId_Status",
                schema: "inventory",
                table: "hold",
                columns: new[] { "CatalogEventId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_hold_EventSessionId_Status",
                schema: "inventory",
                table: "hold",
                columns: new[] { "EventSessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_hold_ExpiresAt",
                schema: "inventory",
                table: "hold",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_EventSessionId_SeatId",
                schema: "inventory",
                table: "inventory_item",
                columns: new[] { "EventSessionId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_item_EventSessionId_Status",
                schema: "inventory",
                table: "inventory_item",
                columns: new[] { "EventSessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_ledger_InventoryItemId",
                schema: "inventory",
                table: "inventory_ledger",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_CorrelationId",
                schema: "inventory",
                table: "outbox",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_PublishedAt",
                schema: "inventory",
                table: "outbox",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_session_inventory_settings_CatalogEventId",
                schema: "inventory",
                table: "session_inventory_settings",
                column: "CatalogEventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letters",
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
                name: "session_inventory_settings",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "hold",
                schema: "inventory");
        }
    }
}
