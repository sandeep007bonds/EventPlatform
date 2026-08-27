using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "inventory_ledger",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "inventory_ledger",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "inventory_ledger",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "inventory_ledger",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "inventory_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "inventory_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "inventory_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "inventory_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "hold_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "hold_general_admission_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold_general_admission_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold_general_admission_item",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold_general_admission_item",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "general_admission_allocation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "general_admission_allocation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "general_admission_allocation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "general_admission_allocation",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "inventory",
                table: "event_inventory_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "inventory",
                table: "event_inventory_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "inventory",
                table: "event_inventory_settings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "inventory",
                table: "event_inventory_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "inventory_ledger");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "inventory_ledger");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "inventory_ledger");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "inventory_ledger");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "inventory_item");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "inventory_item");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "inventory_item");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "inventory_item");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "hold_item");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold_item");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold_item");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold_item");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "hold_general_admission_item");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold_general_admission_item");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold_general_admission_item");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold_general_admission_item");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "hold");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "hold");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "hold");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "general_admission_allocation");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "general_admission_allocation");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "general_admission_allocation");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "general_admission_allocation");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "inventory",
                table: "event_inventory_settings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "inventory",
                table: "event_inventory_settings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "inventory",
                table: "event_inventory_settings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "inventory",
                table: "event_inventory_settings");
        }
    }
}
