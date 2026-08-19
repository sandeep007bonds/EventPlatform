namespace Ordering.Tests.Checkout;

/// <summary>
/// Arithmetic tests for <see cref="OrderPricingCalculator"/> — the one place in the platform that
/// decides what a buyer is charged. Every case here is a rule an organizer or a tax authority
/// would recognise, not an implementation detail: what a code applies to, how far it can go, and
/// the order discount and tax are applied in.
/// </summary>
public sealed class OrderPricingCalculatorTests
{
    [Fact]
    public void Subtotal_IsTheSumOfTheLines_WithNoCodeOrTax()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 5000), Line("GA", 2500)],
            terms: null,
            taxRatePercent: null);

        pricing.SubtotalMinor.ShouldBe(7500);
        pricing.DiscountMinor.ShouldBe(0);
        pricing.TaxMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(7500);
    }

    [Fact]
    public void PercentageDiscount_AppliesToTheWholeSubtotal_WhenNoTiersAreNamed()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 5000), Line("GA", 2500)],
            Percentage(10m),
            taxRatePercent: null);

        pricing.DiscountMinor.ShouldBe(750);
        pricing.TotalMinor.ShouldBe(6750);
    }

    [Fact]
    public void PercentageDiscount_AppliesOnlyToTheNamedTiers()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 5000), Line("GA", 2500)],
            Percentage(10m, "VIP"),
            taxRatePercent: null);

        // 10% of the VIP line only — the GA line is untouched.
        pricing.DiscountMinor.ShouldBe(500);
        pricing.SubtotalMinor.ShouldBe(7500);
        pricing.TotalMinor.ShouldBe(7000);
    }

    [Fact]
    public void TierMatching_IsCaseInsensitive()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 5000)],
            Percentage(10m, "vip"),
            taxRatePercent: null);

        pricing.DiscountMinor.ShouldBe(500);
    }

    [Fact]
    public void Discount_IsZero_WhenNoLineMatchesTheCodesTiers()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 2500)],
            Percentage(10m, "VIP"),
            taxRatePercent: null);

        pricing.DiscountMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(2500);
    }

    [Fact]
    public void FixedDiscount_IsConvertedFromMajorUnitsToMinor()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 5000)],
            Fixed(12.50m),
            taxRatePercent: null);

        pricing.DiscountMinor.ShouldBe(1250);
        pricing.TotalMinor.ShouldBe(3750);
    }

    [Fact]
    public void FixedDiscount_IsClampedToTheEligibleSubtotal_NeverProducingANegativeTotal()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 1000), Line("GA", 9000)],
            Fixed(500m, "VIP"),
            taxRatePercent: null);

        // The code is worth far more than the one line it applies to; it can only take that line
        // to zero, not eat into the ineligible one.
        pricing.DiscountMinor.ShouldBe(1000);
        pricing.TotalMinor.ShouldBe(9000);
    }

    [Fact]
    public void Tax_IsChargedOnThePostDiscountSubtotal()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 10000)],
            Percentage(50m),
            taxRatePercent: 18m);

        pricing.SubtotalMinor.ShouldBe(10000);
        pricing.DiscountMinor.ShouldBe(5000);

        // 18% of 5000, not of 10000 — the order of operations is the whole point.
        pricing.TaxMinor.ShouldBe(900);
        pricing.TotalMinor.ShouldBe(5900);
    }

    [Fact]
    public void Tax_RoundsHalfAwayFromZero()
    {
        // 1000 × 2.55% = 25.5 minor units exactly.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 1000)],
            terms: null,
            taxRatePercent: 2.55m);

        pricing.TaxMinor.ShouldBe(26);
    }

    [Fact]
    public void PercentageDiscount_RoundsHalfAwayFromZero()
    {
        // 333 × 50% = 166.5 minor units exactly.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 333)],
            Percentage(50m),
            taxRatePercent: null);

        pricing.DiscountMinor.ShouldBe(167);
        pricing.TotalMinor.ShouldBe(166);
    }

    [Fact]
    public void ZeroTaxRate_AddsNoTax()
    {
        var pricing = OrderPricingCalculator.Calculate([Line("GA", 1000)], terms: null, taxRatePercent: 0m);

        pricing.TaxMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(1000);
    }

    [Fact]
    public void FullyDiscountedOrder_IsFree_AndAttractsNoTax()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 4000)],
            Percentage(100m),
            taxRatePercent: 18m);

        pricing.DiscountMinor.ShouldBe(4000);
        pricing.TaxMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(0);
    }

    private static OrderLineSpec Line(string priceTier, long priceMinor) =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, 1, priceTier, priceMinor, priceMinor);

    private static PromoCodeTerms Percentage(decimal value, params string[] tiers) =>
        new(PromoDiscountType.Percentage, value, tiers);

    private static PromoCodeTerms Fixed(decimal value, params string[] tiers) =>
        new(PromoDiscountType.FixedAmount, value, tiers);
}
