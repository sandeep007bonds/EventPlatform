using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Communication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CorrelationId",
                schema: "communication",
                table: "delivery_log",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CausationId",
                schema: "communication",
                table: "delivery_log",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dead_letters",
                schema: "communication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CausationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeadLetteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dead_letters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_delivery_log_CausationId",
                schema: "communication",
                table: "delivery_log",
                column: "CausationId");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letters_CorrelationId",
                schema: "communication",
                table: "dead_letters",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letters_MessageId",
                schema: "communication",
                table: "dead_letters",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_dead_letters_ResolvedAt",
                schema: "communication",
                table: "dead_letters",
                column: "ResolvedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dead_letters",
                schema: "communication");

            migrationBuilder.DropIndex(
                name: "IX_delivery_log_CausationId",
                schema: "communication",
                table: "delivery_log");

            migrationBuilder.DropColumn(
                name: "CausationId",
                schema: "communication",
                table: "delivery_log");

            migrationBuilder.AlterColumn<Guid>(
                name: "CorrelationId",
                schema: "communication",
                table: "delivery_log",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
