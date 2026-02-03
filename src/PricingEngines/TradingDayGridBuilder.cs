using DerivaSharp.Time;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

internal static class TradingDayGridBuilder
{
    public static TradingDayGrid Build(DateOnly valuationDate, DateOnly expirationDate, torch.Device device)
    {
        DateOnly[] tradingDays = DateUtils.GetTradingDays(valuationDate, expirationDate).ToArray();
        if (tradingDays.Length <= 1)
        {
            return new TradingDayGrid(
                torch.empty(0, torch.float64, device),
                torch.empty(0, torch.float64, device),
                0,
                tradingDays);
        }

        int stepCount = tradingDays.Length - 1;

        double[] yearFractions = new double[tradingDays.Length];
        int t0 = valuationDate.DayNumber;
        for (int i = 0; i < tradingDays.Length; i++)
        {
            yearFractions[i] = (tradingDays[i].DayNumber - t0) / 365.0;
        }

        double[] dtArray = new double[stepCount];
        for (int i = 0; i < stepCount; i++)
        {
            dtArray[i] = yearFractions[i + 1] - yearFractions[i];
        }

        torch.Tensor timeGrid = torch.tensor(yearFractions, torch.float64, device);
        torch.Tensor dtVector = torch.tensor(dtArray, torch.float64, device);

        return new TradingDayGrid(timeGrid, dtVector, stepCount, tradingDays);
    }
}
