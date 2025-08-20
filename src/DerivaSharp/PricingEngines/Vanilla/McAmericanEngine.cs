﻿using CommunityToolkit.Diagnostics;
using DerivaSharp.Instruments;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

public sealed class McAmericanEngine(int pathCount, int stepCount, bool useCuda = false) : TorchSharpPricingEngine<AmericanOption>(useCuda)
{
    public double Value(AmericanOption option, PricingContext context, RandomNumberSource source)
    {
        Guard.IsGreaterThan(stepCount, 2);

        double x = option.StrikePrice;
        int z = (int)option.OptionType;
        double tau = GetYearsToExpiration(option, context);
        double dt = tau / (stepCount - 1);
        double df = Math.Exp(-context.RiskFreeRate * dt);

        using torch.Tensor priceMatrix = PathGenerator.Generate(context.AssetPrice, context.RiskFreeRate - context.DividendYield, context.Volatility, dt, source);

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor cashFlows = (z * (priceMatrix.select(1, stepCount - 1) - x)).clamp_min_(0);

        for (int j = stepCount - 2; j >= 1; j--)
        {
            cashFlows.mul_(df);

            torch.Tensor priceAtStep = priceMatrix.select(1, j);
            torch.Tensor intrinsicValues = (z * (priceAtStep - x)).clamp_min_(0);
            torch.Tensor itmMask = intrinsicValues > 0;

            long itmCount = itmMask.sum().item<long>();
            if (itmCount <= 2)
            {
                continue;
            }

            torch.Tensor itmPathsX = priceAtStep.masked_select(itmMask);
            torch.Tensor itmPathsY = cashFlows.masked_select(itmMask);

            torch.Tensor vandermondeMatrix = torch.vander(itmPathsX, 3, true);
            torch.Tensor p = torch.linalg.lstsq(vandermondeMatrix, itmPathsY).Solution;
            torch.Tensor continuationValues = torch.matmul(vandermondeMatrix, p);

            torch.Tensor exerciseMask = intrinsicValues.masked_select(itmMask) > continuationValues;
            cashFlows[itmMask] = torch.where(exerciseMask, intrinsicValues.masked_select(itmMask), itmPathsY);
        }

        double average = torch.mean(cashFlows).item<double>();
        return average * df;
    }

    protected override double CalculateValue(AmericanOption option, PricingContext context)
    {
        if (context.ValuationDate == option.ExpirationDate)
        {
            double z = (int)option.OptionType;
            return Math.Max(z * (context.AssetPrice - option.StrikePrice), 0);
        }

        using RandomNumberSource source = new(pathCount, stepCount - 1, Device);
        return Value(option, context, source);
    }
}
