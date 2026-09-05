using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CausationId",
                schema: "payments",
                table: "outbox",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                schema: "payments",
                table: "outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "EventVersion",
                schema: "payments",
                table: "outbox",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_CorrelationId",
                schema: "payments",
                table: "outbox",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_CorrelationId",
                schema: "payments",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "CausationId",
                schema: "payments",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "payments",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "EventVersion",
                schema: "payments",
                table: "outbox");
        }
    }
}
