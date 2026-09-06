using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TierLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TierLabel",
                schema: "venue",
                table: "venue_sections",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TierLabel",
                schema: "venue",
                table: "admission_areas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TierLabel",
                schema: "venue",
                table: "venue_sections");

            migrationBuilder.DropColumn(
                name: "TierLabel",
                schema: "venue",
                table: "admission_areas");
        }
    }
}
