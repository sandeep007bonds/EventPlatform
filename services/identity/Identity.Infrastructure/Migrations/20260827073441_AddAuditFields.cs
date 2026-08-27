using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "identity",
                table: "tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "signing_keys",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "identity",
                table: "signing_keys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "signing_keys",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "phone_verifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "identity",
                table: "phone_verifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "phone_verifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "organizer_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "identity",
                table: "organizer_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "organizer_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "buyer_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "identity",
                table: "buyer_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                schema: "identity",
                table: "buyer_accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "signing_keys");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "signing_keys");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "signing_keys");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "phone_verifications");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "phone_verifications");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "phone_verifications");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "organizer_accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "organizer_accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "organizer_accounts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "identity",
                table: "buyer_accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "identity",
                table: "buyer_accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                schema: "identity",
                table: "buyer_accounts");
        }
    }
}
