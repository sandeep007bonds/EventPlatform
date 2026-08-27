using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ticketing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ticketing",
                table: "ticket",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ticketing",
                table: "ticket",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "ticket",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "ticket",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ticketing",
                table: "seat_entry_gate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ticketing",
                table: "seat_entry_gate",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "seat_entry_gate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "seat_entry_gate",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ticketing",
                table: "ga_allocation_gate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ticketing",
                table: "ga_allocation_gate",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "ga_allocation_gate",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "ga_allocation_gate",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ticketing",
                table: "event_scan_context",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ticketing",
                table: "event_scan_context",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "event_scan_context",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "event_scan_context",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "ticketing",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ticketing",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "ticket");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "ticketing",
                table: "seat_entry_gate");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ticketing",
                table: "seat_entry_gate");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "seat_entry_gate");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "seat_entry_gate");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "ticketing",
                table: "ga_allocation_gate");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ticketing",
                table: "ga_allocation_gate");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "ga_allocation_gate");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "ga_allocation_gate");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "ticketing",
                table: "event_scan_context");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ticketing",
                table: "event_scan_context");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ticketing",
                table: "event_scan_context");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ticketing",
                table: "event_scan_context");
        }
    }
}
