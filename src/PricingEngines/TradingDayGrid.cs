using TorchSharp;

namespace DerivaSharp.PricingEngines;

internal readonly record struct TradingDayGrid(torch.Tensor TimeGrid, torch.Tensor DtVector, int StepCount, DateOnly[] TradingDays);
