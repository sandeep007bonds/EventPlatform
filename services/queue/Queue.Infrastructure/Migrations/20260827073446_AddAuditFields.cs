using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Queue.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "queue",
                table: "queue_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "queue",
                table: "queue_settings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "queue",
                table: "queue_settings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "queue",
                table: "queue_settings");
        }
    }
}
