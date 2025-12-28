namespace DerivaSharp.Instruments;

public sealed record SnowballOption : Option
{
    public SnowballOption(
        double[] knockOutCouponRates,
        double maturityCouponRate,
        double initialPrice,
        double knockInPrice,
        double[] knockOutPrices,
        double upperStrikePrice,
        double lowerStrikePrice,
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
        UpperStrikePrice = upperStrikePrice;
        LowerStrikePrice = lowerStrikePrice;
        KnockOutObservationDates = knockOutObservationDates;
        BarrierTouchStatus = barrierTouchStatus;
    }

    public double[] KnockOutCouponRates { get; init; }

    public double MaturityCouponRate { get; init; }

    public double InitialPrice { get; init; }

    public double KnockInPrice { get; init; }

    public double[] KnockOutPrices { get; init; }

    public double UpperStrikePrice { get; init; }

    public double LowerStrikePrice { get; init; }

    public DateOnly[] KnockOutObservationDates { get; init; }

    public BarrierTouchStatus BarrierTouchStatus { get; init; }

    // 平敲
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
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // 降敲
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
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // 双降
    public static SnowballOption CreateBothDownSnowball(
        double knockOutCouponRateStart,
        double knockOutCouponRateStep,
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
        double[] knockOutCouponRates = new double[n];
        double[] knockOutPrices = new double[n];
        for (int i = 0; i < knockOutObservationDates.Length; i++)
        {
            knockOutCouponRates[i] = knockOutCouponRateStart - i * knockOutCouponRateStep;
            knockOutPrices[i] = initialPrice * (knockOutLevelStart - i * knockOutLevelStep);
        }

        return new SnowballOption(
            knockOutCouponRates,
            knockOutCouponRates[^1],
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // 红利
    public static SnowballOption CreateDualCouponSnowball(
        double knockOutCouponRate,
        double maturityCouponRate,
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
            Enumerable.Repeat(knockOutCouponRate, n).ToArray(),
            maturityCouponRate,
            initialPrice,
            initialPrice * knockInLevel,
            Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray(),
            initialPrice,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // 降落伞
    public static SnowballOption CreateParachuteSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double finalKnockOutLevel,
        DateOnly[] knockOutObservationDates,
        BarrierTouchStatus barrierTouchStatus,
        DateOnly effectiveDate,
        DateOnly expirationDate)
    {
        int n = knockOutObservationDates.Length;
        double[] knockOutPrices = Enumerable.Repeat(initialPrice * knockOutLevel, n).ToArray();
        knockOutPrices[^1] = initialPrice * finalKnockOutLevel;
        return new SnowballOption(
            Enumerable.Repeat(couponRate, n).ToArray(),
            couponRate,
            initialPrice,
            initialPrice * knockInLevel,
            knockOutPrices,
            initialPrice,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // OTM
    public static SnowballOption CreateOtmSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double strikeLevel,
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
            initialPrice * strikeLevel,
            0,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }

    // 限损
    public static SnowballOption CreateLossCappedSnowball(
        double couponRate,
        double initialPrice,
        double knockInLevel,
        double knockOutLevel,
        double floorLevel,
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
            initialPrice * floorLevel,
            knockOutObservationDates,
            barrierTouchStatus,
            effectiveDate,
            expirationDate);
    }
}
