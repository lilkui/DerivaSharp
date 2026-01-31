using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.Tests.Asian;

public static class AsianOptionTestData
{
    public const double Strike = 100;
    public static readonly DateOnly EffectiveDate = new(2025, 1, 6);
    public static readonly DateOnly ExpirationDate = EffectiveDate.AddDays(180);
    public static readonly BsmModelParameters ModelParameters = new(0.3, 0.04, 0.01);
    public static readonly DateOnly ValuationDate = EffectiveDate.AddDays(90);

    public static TheoryData<OptionType, double, double, double> ArithmeticValueData => new()
    {
        // OptionType, AssetPrice, RealizedAveragePrice, ExpectedValue
        { OptionType.Call, 100, 101, 2.056893 },
        { OptionType.Put, 100, 101, 1.378232 },
    };
}
