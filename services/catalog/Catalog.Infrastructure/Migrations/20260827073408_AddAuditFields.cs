using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "seats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "seats",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seats",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "seat_maps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "seat_maps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seat_maps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seat_maps",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "seat_map_ga_sections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "seat_map_ga_sections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seat_map_ga_sections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seat_map_ga_sections",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "promo_codes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "promo_codes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "promo_codes",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "promo_code_tiers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "promo_code_tiers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "promo_code_tiers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "promo_code_tiers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "events",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_social_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_social_links",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_social_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_social_links",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_groups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_groups",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_groups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_group_social_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_group_social_links",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_group_social_links",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_group_social_links",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "catalog",
                table: "entry_gates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "catalog",
                table: "entry_gates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "catalog",
                table: "entry_gates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "catalog",
                table: "entry_gates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "seat_maps");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "seat_maps");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seat_maps");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seat_maps");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "seat_map_ga_sections");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "seat_map_ga_sections");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "seat_map_ga_sections");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "seat_map_ga_sections");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "promo_codes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "promo_codes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "promo_codes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "promo_code_tiers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "promo_code_tiers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "promo_code_tiers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "promo_code_tiers");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_social_links");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_social_links");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_social_links");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_social_links");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_groups");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_groups");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_groups");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_groups");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "event_group_social_links");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "event_group_social_links");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "event_group_social_links");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "event_group_social_links");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "catalog",
                table: "entry_gates");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "catalog",
                table: "entry_gates");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "catalog",
                table: "entry_gates");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "catalog",
                table: "entry_gates");
        }
    }
}
