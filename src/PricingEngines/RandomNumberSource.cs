using CommunityToolkit.Diagnostics;
using TorchSharp;

namespace DerivaSharp.PricingEngines;

/// <summary>
///     Generates and caches random numbers for Monte Carlo simulations using antithetic variates.
/// </summary>
public sealed class RandomNumberSource : IDisposable
{
    private readonly torch.Device _device;
    private readonly int _pathCount;
    private readonly int _stepCount;
    private torch.Tensor? _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RandomNumberSource" /> class.
    /// </summary>
    /// <param name="pathCount">The number of simulation paths.</param>
    /// <param name="stepCount">The number of time steps per path.</param>
    /// <param name="device">The device on which to allocate tensors.</param>
    public RandomNumberSource(int pathCount, int stepCount, torch.Device device)
    {
        Guard.IsGreaterThan(pathCount, 0);
        Guard.IsGreaterThan(stepCount, 0);

        _pathCount = pathCount;
        _stepCount = stepCount;
        _device = device;
    }

    public void Dispose() => _cache?.Dispose();

    /// <summary>
    ///     Gets a matrix of standard normal random numbers with antithetic variates applied.
    /// </summary>
    /// <returns>A tensor of shape [pathCount, stepCount] containing random numbers.</returns>
    public torch.Tensor GetRandomNumberMatrix()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        using (torch.NewDisposeScope())
        {
            int totalPathCount = _pathCount % 2 != 0 ? _pathCount + 1 : _pathCount;
            int halfPathCount = totalPathCount / 2;
            torch.Tensor halfTensor = torch.randn([halfPathCount, _stepCount], torch.float64, _device);
            torch.Tensor fullTensor = torch.cat([halfTensor, -halfTensor]);

            _cache = fullTensor.MoveToOuterDisposeScope();
        }

        return _cache;
    }
}
