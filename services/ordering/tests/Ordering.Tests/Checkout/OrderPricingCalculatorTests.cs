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
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

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
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(750);
        pricing.TotalMinor.ShouldBe(6750);
    }

    [Fact]
    public void PercentageDiscount_AppliesOnlyToTheNamedTiers()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 5000), Line("GA", 2500)],
            Percentage(10m, "VIP"),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

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
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(500);
    }

    [Fact]
    public void Discount_IsZero_WhenNoLineMatchesTheCodesTiers()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 2500)],
            Percentage(10m, "VIP"),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(2500);
    }

    [Fact]
    public void FixedDiscount_IsConvertedFromMajorUnitsToMinor()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 5000)],
            Fixed(12.50m),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(1250);
        pricing.TotalMinor.ShouldBe(3750);
    }

    [Fact]
    public void FixedDiscount_IsClampedToTheEligibleSubtotal_NeverProducingANegativeTotal()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("VIP", 1000), Line("GA", 9000)],
            Fixed(500m, "VIP"),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

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
            taxRatePercent: 18m,
            bookingFeePerTicketMinor: 0);

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
            taxRatePercent: 2.55m,
            bookingFeePerTicketMinor: 0);

        pricing.TaxMinor.ShouldBe(26);
    }

    [Fact]
    public void PercentageDiscount_RoundsHalfAwayFromZero()
    {
        // 333 × 50% = 166.5 minor units exactly.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 333)],
            Percentage(50m),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(167);
        pricing.TotalMinor.ShouldBe(166);
    }

    [Fact]
    public void ZeroTaxRate_AddsNoTax()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 1000)],
            terms: null,
            taxRatePercent: 0m,
            bookingFeePerTicketMinor: 0);

        pricing.TaxMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(1000);
    }

    [Fact]
    public void FullyDiscountedOrder_IsFree_AndAttractsNoTax()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 4000)],
            Percentage(100m),
            taxRatePercent: 18m,
            bookingFeePerTicketMinor: 0);

        pricing.DiscountMinor.ShouldBe(4000);
        pricing.TaxMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(0);
    }

    [Fact]
    public void BookingFee_IsChargedPerTicket_NotPerLine()
    {
        // One general-admission line of four admissions. A fee charged per line would return 3000
        // here, and would under-charge exactly the orders that are largest.
        var pricing = OrderPricingCalculator.Calculate(
            [GeneralAdmissionLine("GA", unitPriceMinor: 2000, quantity: 4)],
            terms: null,
            taxRatePercent: null,
            bookingFeePerTicketMinor: 3000);

        pricing.SubtotalMinor.ShouldBe(8000);
        pricing.BookingFeeMinor.ShouldBe(12000);
        pricing.TotalMinor.ShouldBe(20000);
    }

    [Fact]
    public void BookingFee_IsNotReducedByAPromoCode()
    {
        // The code discounts the tickets; it does not discount what the platform charges to sell
        // them. 50% of 10000 comes off the subtotal, and the fee is untouched.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 10000)],
            Percentage(50m),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 2500);

        pricing.DiscountMinor.ShouldBe(5000);
        pricing.BookingFeeMinor.ShouldBe(2500);
        pricing.TotalMinor.ShouldBe(7500);
    }

    [Fact]
    public void BookingFee_IsTaxed_AlongsideTheDiscountedTickets()
    {
        // 10000 − 2000 discount = 8000 tickets, plus a 1000 fee. At 18%: 1440 + 180.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 10000)],
            Percentage(20m),
            taxRatePercent: 18m,
            bookingFeePerTicketMinor: 1000);

        pricing.BookingFeeMinor.ShouldBe(1000);
        pricing.TaxMinor.ShouldBe(1620);
        pricing.TotalMinor.ShouldBe(10620);
    }

    [Fact]
    public void RefundableAmount_ExcludesTheFeeAndTheTaxOnIt()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 10000)],
            terms: null,
            taxRatePercent: 18m,
            bookingFeePerTicketMinor: 1000);

        // Charged 10000 + 1000 fee + 1800 + 180 tax. Cancelling returns the tickets and their tax
        // only; the platform keeps the fee and the tax it collected on the fee.
        pricing.TotalMinor.ShouldBe(12980);
        pricing.RefundableMinor.ShouldBe(11800);
        (pricing.TotalMinor - pricing.RefundableMinor).ShouldBe(1180);
    }

    [Fact]
    public void TaxIsRoundedPerComponent_SoTheRefundIsExact()
    {
        // The case the two-part tax exists for, and it is not hypothetical: 1001 and 102 at 18%
        // round to 180 and 18, but their sum 1103 rounds to 199 — one more than 180 + 18.
        //
        // Rounding once over the combined base would charge 1302 and then, refunding by subtracting
        // a re-derived fee tax of 18, return 1182 while the tickets and their tax are only worth
        // 1181. A minor unit, every time, in the buyer's favour and out of the platform's pocket.
        // Taxing each component at source makes the refund an exact subtraction instead.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 1001)],
            terms: null,
            taxRatePercent: 18m,
            bookingFeePerTicketMinor: 102);

        pricing.TaxMinor.ShouldBe(198);
        pricing.TotalMinor.ShouldBe(1301);
        pricing.RefundableMinor.ShouldBe(1181);
        (pricing.TotalMinor - pricing.RefundableMinor).ShouldBe(120);
    }

    [Fact]
    public void NegativeBookingFee_IsTreatedAsZero_NotAsACredit()
    {
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 5000)],
            terms: null,
            taxRatePercent: null,
            bookingFeePerTicketMinor: -1000);

        pricing.BookingFeeMinor.ShouldBe(0);
        pricing.TotalMinor.ShouldBe(5000);
    }

    [Fact]
    public void FreeOrderWithAFee_StillChargesTheFee()
    {
        // A 100%-off code makes the tickets free. The fee is not a ticket price, so it survives —
        // and the buyer is charged something rather than nothing.
        var pricing = OrderPricingCalculator.Calculate(
            [Line("GA", 4000)],
            Percentage(100m),
            taxRatePercent: null,
            bookingFeePerTicketMinor: 500);

        pricing.DiscountMinor.ShouldBe(4000);
        pricing.BookingFeeMinor.ShouldBe(500);
        pricing.TotalMinor.ShouldBe(500);
        pricing.RefundableMinor.ShouldBe(0);
    }

    private static OrderLineSpec Line(string priceTier, long priceMinor) =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, 1, priceTier, priceMinor, priceMinor);

    private static OrderLineSpec GeneralAdmissionLine(string priceTier, long unitPriceMinor, int quantity) =>
        new(null, null, Guid.NewGuid(), quantity, priceTier, unitPriceMinor, unitPriceMinor * quantity);

    private static PromoCodeTerms Percentage(decimal value, params string[] tiers) =>
        new(PromoDiscountType.Percentage, value, tiers);

    private static PromoCodeTerms Fixed(decimal value, params string[] tiers) =>
        new(PromoDiscountType.FixedAmount, value, tiers);
}
