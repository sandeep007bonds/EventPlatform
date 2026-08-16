using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ticketing");

            migrationBuilder.CreateTable(
                name: "event_scan_context",
                schema: "ticketing",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoorsOpenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_scan_context", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ga_allocation_gate",
                schema: "ticketing",
                columns: table => new
                {
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryGateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ga_allocation_gate", x => x.AllocationId);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "ticketing",
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
                name: "seat_entry_gate",
                schema: "ticketing",
                columns: table => new
                {
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryGateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_entry_gate", x => x.SeatId);
                });

            migrationBuilder.CreateTable(
                name: "ticket",
                schema: "ticketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CatalogEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: true),
                    GeneralAdmissionAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ga_allocation_gate_EventId",
                schema: "ticketing",
                table: "ga_allocation_gate",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_PublishedAt",
                schema: "ticketing",
                table: "outbox",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_seat_entry_gate_EventId",
                schema: "ticketing",
                table: "seat_entry_gate",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_OrderId",
                schema: "ticketing",
                table: "ticket",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_OrderId_SeatId",
                schema: "ticketing",
                table: "ticket",
                columns: new[] { "OrderId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ticket_Token",
                schema: "ticketing",
                table: "ticket",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_scan_context",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "ga_allocation_gate",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "seat_entry_gate",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "ticket",
                schema: "ticketing");
        }
    }
}
