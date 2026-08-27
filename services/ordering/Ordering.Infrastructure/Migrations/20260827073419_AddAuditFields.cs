using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ordering",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ordering",
                table: "orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ordering",
                table: "orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "ordering",
                table: "order_line",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "ordering",
                table: "order_line",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "ordering",
                table: "order_line",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "ordering",
                table: "order_line",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "ordering",
                table: "order_line");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "ordering",
                table: "order_line");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "ordering",
                table: "order_line");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "ordering",
                table: "order_line");
        }
    }
}
