using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Represents a time grid based on trading days for finite difference methods.
/// </summary>
/// <param name="TimeGrid">The tensor containing time points in years from valuation date.</param>
/// <param name="DtVector">The tensor containing time step sizes between consecutive grid points.</param>
/// <param name="StepCount">The number of time steps in the grid.</param>
/// <param name="TradingDays">The array of trading days corresponding to the time grid.</param>
internal readonly record struct TradingDayGrid(torch.Tensor TimeGrid, torch.Tensor DtVector, int StepCount, DateOnly[] TradingDays);
