﻿using DerivaSharp.Instruments;
using DerivaSharp.PricingEngines;

namespace DerivaSharp.Tests;

public class McAmericanEngineTest
{
    private readonly McAmericanEngine _engine = new(100000, 250);

    [Theory]
    [InlineData(OptionType.Call, 100, 12.5682)]
    [InlineData(OptionType.Put, 100, 10.8264)]
    public void Value_IsAccurate(OptionType optionType, double assetPrice, double expected)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        AmericanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext ctx = new(assetPrice, effectiveDate, 0.3, 0.04, 0.02);

        double tolerance = Math.Abs(expected) * 0.02;
        Assert.Equal(expected, _engine.Value(option, ctx), tolerance);
    }

    [Theory]
    [InlineData(OptionType.Call, 110, 10)]
    [InlineData(OptionType.Put, 90, 10)]
    public void Value_AtExpiry_ReturnsIntrinsicValue(OptionType optionType, double assetPrice, double expected)
    {
        const double strike = 100;
        DateOnly effectiveDate = new(2025, 1, 6);
        DateOnly expirationDate = effectiveDate.AddDays(365);

        AmericanOption option = new(optionType, strike, effectiveDate, expirationDate);
        PricingContext ctx = new(assetPrice, expirationDate, 0.3, 0.04, 0.01);

        const int precision = 6;
        Assert.Equal(expected, _engine.Value(option, ctx), precision);
    }
}
