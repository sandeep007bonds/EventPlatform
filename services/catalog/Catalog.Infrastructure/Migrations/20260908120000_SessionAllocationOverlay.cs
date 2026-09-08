using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SessionAllocationOverlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "TicketTypeId",
                schema: "catalog",
                table: "session_allocations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "CapacityOverride",
                schema: "catalog",
                table: "session_allocations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "catalog",
                table: "session_allocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExcluded",
                schema: "catalog",
                table: "session_allocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CapacityOverride",
                schema: "catalog",
                table: "session_allocations");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "catalog",
                table: "session_allocations");

            migrationBuilder.DropColumn(
                name: "IsExcluded",
                schema: "catalog",
                table: "session_allocations");

            migrationBuilder.AlterColumn<Guid>(
                name: "TicketTypeId",
                schema: "catalog",
                table: "session_allocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
