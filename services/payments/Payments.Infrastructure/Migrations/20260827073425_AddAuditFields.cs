using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "payments",
                table: "processed_webhook_event",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "payments",
                table: "processed_webhook_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "payments",
                table: "processed_webhook_event",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "payments",
                table: "processed_webhook_event",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "payments",
                table: "payment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "payments",
                table: "payment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "payments",
                table: "payment",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "payments",
                table: "processed_webhook_event");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "payments",
                table: "processed_webhook_event");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "payments",
                table: "processed_webhook_event");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "payments",
                table: "processed_webhook_event");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "payments",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "payments",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "payments",
                table: "payment");
        }
    }
}
