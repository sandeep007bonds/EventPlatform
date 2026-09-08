using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Venues.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClientGeneratedKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty. ApplyClientGeneratedKeys changes what EF *infers* from a key it
            // does not generate, not what the column is: no key column here ever had a default or
            // an identity, so there is no DDL to run. The migration exists so the model snapshot
            // and the model agree, which is what the drift check compares.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
