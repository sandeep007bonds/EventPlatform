using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "communication",
                table: "processed_notification_event",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "communication",
                table: "processed_notification_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "communication",
                table: "processed_notification_event",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "communication",
                table: "processed_notification_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "communication",
                table: "delivery_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "communication",
                table: "delivery_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "communication",
                table: "delivery_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "communication",
                table: "delivery_log",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "communication",
                table: "processed_notification_event");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "communication",
                table: "processed_notification_event");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "communication",
                table: "processed_notification_event");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "communication",
                table: "processed_notification_event");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "communication",
                table: "delivery_log");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "communication",
                table: "delivery_log");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "communication",
                table: "delivery_log");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "communication",
                table: "delivery_log");
        }
    }
}
