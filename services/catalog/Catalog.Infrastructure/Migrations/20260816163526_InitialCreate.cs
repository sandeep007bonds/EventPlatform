using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "entry_gates",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entry_gates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_groups",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactMobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_groups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "catalog",
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
                name: "seat_maps",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_maps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_group_social_links",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_group_social_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_group_social_links_event_groups_EventGroupId",
                        column: x => x.EventGroupId,
                        principalSchema: "catalog",
                        principalTable: "event_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SalesPaused = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DoorsOpenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OnSaleAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    BookingEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaxTicketsPerBuyer = table.Column<int>(type: "integer", nullable: true),
                    RequiresQueue = table.Column<bool>(type: "boolean", nullable: false),
                    AgeRestriction = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactMobile = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_events_event_groups_EventGroupId",
                        column: x => x.EventGroupId,
                        principalSchema: "catalog",
                        principalTable: "event_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seat_map_ga_sections",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatMapId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    EntryGateId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_map_ga_sections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seat_map_ga_sections_seat_maps_SeatMapId",
                        column: x => x.SeatMapId,
                        principalSchema: "catalog",
                        principalTable: "seat_maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatMapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PriceTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Row = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    EntryGateId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seats_seat_maps_SeatMapId",
                        column: x => x.SeatMapId,
                        principalSchema: "catalog",
                        principalTable: "seat_maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_social_links",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Platform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_social_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_social_links_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "catalog",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entry_gates_EventId",
                schema: "catalog",
                table: "entry_gates",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_event_group_social_links_EventGroupId",
                schema: "catalog",
                table: "event_group_social_links",
                column: "EventGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_event_groups_TenantId_Id",
                schema: "catalog",
                table: "event_groups",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_event_social_links_EventId",
                schema: "catalog",
                table: "event_social_links",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_events_EventGroupId",
                schema: "catalog",
                table: "events",
                column: "EventGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_events_TenantId_Id",
                schema: "catalog",
                table: "events",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_PublishedAt",
                schema: "catalog",
                table: "outbox",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_seat_map_ga_sections_SeatMapId_SectionName",
                schema: "catalog",
                table: "seat_map_ga_sections",
                columns: new[] { "SeatMapId", "SectionName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seat_maps_EventId",
                schema: "catalog",
                table: "seat_maps",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seat_maps_TenantId_EventId",
                schema: "catalog",
                table: "seat_maps",
                columns: new[] { "TenantId", "EventId" });

            migrationBuilder.CreateIndex(
                name: "IX_seats_SeatMapId_Section_Row_Number",
                schema: "catalog",
                table: "seats",
                columns: new[] { "SeatMapId", "Section", "Row", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entry_gates",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "event_group_social_links",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "event_social_links",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "seat_map_ga_sections",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "seats",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "events",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "seat_maps",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "event_groups",
                schema: "catalog");
        }
    }
}
