using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using DerivaSharp.Models;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Pricing engine for barrier options using finite difference methods with barrier monitoring.
/// </summary>
/// <param name="scheme">The finite difference scheme to use.</param>
/// <param name="priceStepCount">The number of price steps in the grid.</param>
/// <param name="timeStepCount">The number of time steps in the grid.</param>
public sealed class FdBarrierEngine(FiniteDifferenceScheme scheme, int priceStepCount, int timeStepCount)
    : BsmFiniteDifferenceEngine<BarrierOption>(scheme, priceStepCount, timeStepCount)
{
    private bool[]? _isObservationTime;
    private double[]? _observationTimes;

    protected override bool UseTradingDayGrid => true;

    protected override double CalculateValue(BarrierOption option, BsmModelParameters parameters, double assetPrice, DateOnly valuationDate)
    {
        if (valuationDate == option.ExpirationDate)
        {
            double x = option.StrikePrice;
            double h = option.BarrierPrice;
            double k = option.Rebate;
            double s = assetPrice;
            int z = (int)option.OptionType;
            double intrinsic = Math.Max(z * (s - x), 0);

            return option.BarrierType switch
            {
                BarrierType.UpAndIn => s >= h ? intrinsic : k,
                BarrierType.DownAndIn => s <= h ? intrinsic : k,
                BarrierType.UpAndOut => s >= h ? k : intrinsic,
                BarrierType.DownAndOut => s <= h ? k : intrinsic,
                _ => ThrowHelper.ThrowArgumentException<double>(ExceptionMessages.InvalidBarrierType),
            };
        }

        return base.CalculateValue(option, parameters, assetPrice, valuationDate);
    }

    protected override void InitializeGrid(BarrierOption option, BsmModelParameters parameters, DateOnly valuationDate)
    {
        if (option.ObservationInterval == 0) // continuous observation
        {
            switch (option.BarrierType)
            {
                case BarrierType.UpAndOut:
                case BarrierType.UpAndIn:
                    MinPrice = 0;
                    MaxPrice = option.BarrierPrice;
                    break;

                case BarrierType.DownAndOut:
                case BarrierType.DownAndIn:
                    MinPrice = option.BarrierPrice;
                    MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
                    break;

                default:
                    Debug.Fail(ExceptionMessages.InvalidBarrierType);
                    break;
            }
        }
        else // discrete observation
        {
            MinPrice = 0;
            MaxPrice = 4 * Math.Max(option.StrikePrice, option.BarrierPrice);
        }

        _observationTimes = option.ObservationInterval > 0 ? BuildObservationTimes(option.ObservationDates, valuationDate) : null;

        base.InitializeGrid(option, parameters, valuationDate);

        if (option.ObservationInterval > 0)
        {
            BuildObservationSchedule();
        }
        else
        {
            _isObservationTime = null;
        }
    }

    protected override void SetTerminalCondition(BarrierOption option)
    {
        double x = option.StrikePrice;
        double k = option.Rebate;
        int z = (int)option.OptionType;

        if (option.BarrierType is BarrierType.UpAndOut or BarrierType.DownAndOut)
        {
            for (int j = 0; j < PriceVector.Length; j++)
            {
                ValueMatrixSpan[^1, j] = Math.Max(z * (PriceVector[j] - x), 0);
            }
        }
        else // knock-in
        {
            for (int j = 0; j < PriceVector.Length; j++)
            {
                ValueMatrixSpan[^1, j] = k;
            }
        }
    }

    protected override void SetBoundaryConditions(BarrierOption option, BsmModelParameters parameters)
    {
        double x = option.StrikePrice;
        double k = option.Rebate;
        int z = (int)option.OptionType;
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;

        double maxTime = TimeVector[^1];
        double minPrice = PriceVector[0];
        double maxPrice = PriceVector[^1];

        if (option.ObservationInterval == 0) // continuous observation
        {
            for (int i = 0; i < TimeVector.Length; i++)
            {
                double tau = maxTime - TimeVector[i];
                double dfq = Math.Exp(-q * tau);
                double dfr = Math.Exp(-r * tau);
                double pvRebate = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * dfr;

                switch (option.BarrierType)
                {
                    case BarrierType.UpAndOut:
                        ValueMatrixSpan[i, 0] = Math.Max(z * (minPrice * dfq - x * dfr), 0);
                        ValueMatrixSpan[i, ^1] = pvRebate;
                        break;
                    case BarrierType.DownAndOut:
                        ValueMatrixSpan[i, 0] = pvRebate;
                        ValueMatrixSpan[i, ^1] = Math.Max(z * (maxPrice * dfq - x * dfr), 0);
                        break;
                    case BarrierType.UpAndIn:
                        ValueMatrixSpan[i, 0] = Math.Max(z * (minPrice * dfq - x * dfr), 0);
                        ValueMatrixSpan[i, ^1] = BsmCalculator.CalculateValue(z, maxPrice, x, tau, vol, r, q);
                        break;
                    case BarrierType.DownAndIn:
                        ValueMatrixSpan[i, 0] = BsmCalculator.CalculateValue(z, minPrice, x, tau, vol, r, q);
                        ValueMatrixSpan[i, ^1] = Math.Max(z * (maxPrice * dfq - x * dfr), 0);
                        break;
                    default:
                        Debug.Fail(ExceptionMessages.InvalidBarrierType);
                        break;
                }
            }
        }
        else // discrete observation
        {
            for (int i = 0; i < TimeVector.Length - 1; i++)
            {
                double tau = maxTime - TimeVector[i];
                double dfq = Math.Exp(-q * tau);
                double dfr = Math.Exp(-r * tau);

                switch (option.OptionType)
                {
                    case OptionType.Call:
                        ValueMatrixSpan[i, 0] = 0.0;
                        ValueMatrixSpan[i, ^1] = maxPrice * dfq - x * dfr;
                        break;
                    case OptionType.Put:
                        ValueMatrixSpan[i, 0] = x * dfr - minPrice * dfq;
                        ValueMatrixSpan[i, ^1] = 0.0;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentException(ExceptionMessages.InvalidOptionType);
                        break;
                }
            }
        }
    }

    protected override void ApplyStepConditions(int i, BarrierOption option, BsmModelParameters parameters)
    {
        // Continuous observation: nothing to do (already handled by boundary conditions)
        if (option.ObservationInterval == 0)
        {
            return;
        }

        Debug.Assert(_isObservationTime is not null);
        if (!_isObservationTime[i])
        {
            return;
        }

        Span<double> currentValues = ValueMatrixSpan.GetRowSpan(i);
        double x = option.StrikePrice;
        double barrier = option.BarrierPrice;
        double k = option.Rebate;
        int z = (int)option.OptionType;
        double tau = TimeVector[^1] - TimeVector[i];
        double vol = parameters.Volatility;
        double r = parameters.RiskFreeRate;
        double q = parameters.DividendYield;
        double pvRebate = option.RebatePaymentType == PaymentType.PayAtHit ? k : k * Math.Exp(-parameters.RiskFreeRate * tau);

        switch (option.BarrierType)
        {
            case BarrierType.UpAndOut:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] >= barrier)
                    {
                        currentValues[j] = pvRebate;
                    }
                }

                break;

            case BarrierType.DownAndOut:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] <= barrier)
                    {
                        currentValues[j] = pvRebate;
                    }
                }

                break;

            case BarrierType.UpAndIn:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] >= barrier)
                    {
                        currentValues[j] = BsmCalculator.CalculateValue(z, PriceVector[j], x, tau, vol, r, q);
                    }
                }

                break;

            case BarrierType.DownAndIn:
                for (int j = 0; j < PriceVector.Length; j++)
                {
                    if (PriceVector[j] <= barrier)
                    {
                        currentValues[j] = BsmCalculator.CalculateValue(z, PriceVector[j], x, tau, vol, r, q);
                    }
                }

                break;

            default:
                Debug.Fail(ExceptionMessages.InvalidBarrierType);
                break;
        }
    }

    private static double[] BuildObservationTimes(DateOnly[] observationDates, DateOnly valuationDate)
    {
        if (observationDates.Length == 0)
        {
            return [];
        }

        double[] times = new double[observationDates.Length];
        int t0 = valuationDate.DayNumber;

        for (int i = 0; i < observationDates.Length; i++)
        {
            times[i] = (observationDates[i].DayNumber - t0) / 365.0;
        }

        return times;
    }

    private void BuildObservationSchedule()
    {
        Debug.Assert(_observationTimes is not null);

        int nTimes = TimeVector.Length;
        Debug.Assert(nTimes >= 2);

        _isObservationTime = _isObservationTime is { Length: var length } && length == nTimes
            ? _isObservationTime
            : new bool[nTimes];

        MapObservationFlags(_observationTimes, _isObservationTime);
    }
}
