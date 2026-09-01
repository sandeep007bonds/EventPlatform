using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlugsPoliciesAndTicketTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TicketTypeId",
                schema: "catalog",
                table: "seats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TicketTypeId",
                schema: "catalog",
                table: "seat_map_ga_sections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<long>(
                name: "BookingFeePerTicketMinor",
                schema: "catalog",
                table: "events",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                schema: "catalog",
                table: "events",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                schema: "catalog",
                table: "events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "policy_documents",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_policy_documents_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "catalog",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ticket_types",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SalesStartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SalesEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxPerBuyer = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_types", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_Slug",
                schema: "catalog",
                table: "events",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_policy_documents_EventId",
                schema: "catalog",
                table: "policy_documents",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_policy_documents_TenantId_EventId_Kind",
                schema: "catalog",
                table: "policy_documents",
                columns: new[] { "TenantId", "EventId", "Kind" },
                unique: true,
                filter: "\"EventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_policy_documents_TenantId_Kind",
                schema: "catalog",
                table: "policy_documents",
                columns: new[] { "TenantId", "Kind" },
                unique: true,
                filter: "\"EventId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_types_EventId",
                schema: "catalog",
                table: "ticket_types",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_types_EventId_Name",
                schema: "catalog",
                table: "ticket_types",
                columns: new[] { "EventId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policy_documents",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "ticket_types",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_events_Slug",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "TicketTypeId",
                schema: "catalog",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "TicketTypeId",
                schema: "catalog",
                table: "seat_map_ga_sections");

            migrationBuilder.DropColumn(
                name: "BookingFeePerTicketMinor",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Slug",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                schema: "catalog",
                table: "events");
        }
    }
}
