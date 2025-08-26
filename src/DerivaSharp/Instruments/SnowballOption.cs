namespace DerivaSharp.Instruments;

public sealed record SnowballOption : Option
{
    public SnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
        double strikePrice,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
        : base(effectiveDate, expirationDate)
    {
        KnockOutCouponRates = knockOutCouponRates;
        MaturityCouponRate = maturityCouponRate;
        KnockInPrice = knockInPrice;
        KnockOutPrices = knockOutPrices;
        InitialPrice = initialPrice;
        StrikePrice = strikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double[] KnockOutCouponRates { get; init; }

    public double MaturityCouponRate { get; init; }

    public double InitialPrice { get; init; }

    public double KnockInPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double StrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    public static SnowballOption CreateStandardSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    public static SnowballOption CreateStepDownSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        double[] knockOutPrices = knockOutObservationDates
            .Select((_, i) => initialPrice * (knockOutLevelStart - i * knockOutLevelStep))
            .ToArray();
        return new SnowballOption(
            Enumerable.Repeat(couponRate, knockOutObservationDates.Length).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    public static SnowballOption CreateBothDownSnowball(
        double koCouponRateStart,
        double koCouponRateStep,
        double initialPrice,
        double knockInLevel,
        double knockOutLevelStart,
        double knockOutLevelStep,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        double[] koCouponRates = new double[n];
        double[] knockOutPrices = new double[n];
        for (int i = 0; i < knockOutObservationDates.Length; i++)
        {
            koCouponRates[i] = koCouponRateStart - i * koCouponRateStep;
            knockOutPrices[i] = initialPrice * (knockOutLevelStart - i * knockOutLevelStep);
        }

        return new SnowballOption(
            koCouponRates,
            koCouponRates[^1],
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
