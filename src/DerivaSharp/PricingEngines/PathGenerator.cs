using TorchSharp;

namespace DerivaSharp.PricingEngines;

public static class PathGenerator
{
    public static torch.Tensor Generate(double s0, double mu, double sigma, torch.Tensor dtVector, RandomNumberSource source)
    {
        torch.Tensor z = source.GetRandomNumberMatrix();

        using DisposeScope scope = torch.NewDisposeScope();

        torch.Tensor sqrtDt = torch.sqrt(dtVector).unsqueeze(0);
        torch.Tensor drift = ((mu - 0.5 * sigma * sigma) * dtVector).unsqueeze(0);

        torch.Tensor logReturns = torch.addcmul(drift, z, sqrtDt, sigma);
        torch.Tensor prices = torch.exp_(torch.cumsum(logReturns, 1)).mul_(s0);

        torch.Tensor paths = torch.empty([prices.shape[0], prices.shape[1] + 1], z.dtype, z.device);
        paths[.., 0] = s0;
        paths[.., 1..] = prices;

        return paths.MoveToOuterDisposeScope();
    }
}
