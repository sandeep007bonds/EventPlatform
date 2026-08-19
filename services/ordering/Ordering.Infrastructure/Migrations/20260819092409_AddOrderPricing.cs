using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DiscountMinor",
                schema: "ordering",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "PromoCodeId",
                schema: "ordering",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromoCodeText",
                schema: "ordering",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubtotalMinor",
                schema: "ordering",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TaxLabel",
                schema: "ordering",
                table: "orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TaxMinor",
                schema: "ordering",
                table: "orders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercent",
                schema: "ordering",
                table: "orders",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceTier",
                schema: "ordering",
                table: "order_line",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_orders_PromoCodeId",
                schema: "ordering",
                table: "orders",
                column: "PromoCodeId");

            // Hand-added backfill, not generated — keep it if this migration is ever regenerated.
            // SubtotalMinor arrives defaulted to 0, which would make every order placed before this
            // migration show "Subtotal 0.00" next to a real Total on the order page. For those rows
            // the subtotal IS the total: there was no discount or tax mechanism in the platform
            // before this change, so nothing could have separated the two. DiscountMinor and
            // TaxMinor are correct at their 0 default for the same reason.
            migrationBuilder.Sql(
                """
                UPDATE ordering.orders
                SET "SubtotalMinor" = "TotalMinor"
                WHERE "SubtotalMinor" = 0 AND "TotalMinor" <> 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_PromoCodeId",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "DiscountMinor",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeId",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PromoCodeText",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "SubtotalMinor",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TaxLabel",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TaxMinor",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                schema: "ordering",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "PriceTier",
                schema: "ordering",
                table: "order_line");
        }
    }
}
