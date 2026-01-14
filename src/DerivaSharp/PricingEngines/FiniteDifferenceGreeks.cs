namespace DerivaSharp.PricingEngines;

internal static class FiniteDifferenceGreeks
{
    public static double[] ComputeDeltas(double[] assetPrices, double[] values)
    {
        int count = assetPrices.Length;
        double ds = assetPrices[1] - assetPrices[0];

        double[] deltas = new double[count];
        deltas[0] = (values[1] - values[0]) / ds;
        deltas[^1] = (values[^1] - values[^2]) / ds;

        for (int i = 1; i < count - 1; i++)
        {
            deltas[i] = (values[i + 1] - values[i - 1]) / (2 * ds);
        }

        return deltas;
    }

    public static double[] ComputeGammas(double[] assetPrices, double[] values)
    {
        int count = assetPrices.Length;
        double ds = assetPrices[1] - assetPrices[0];
        double dsSquared = ds * ds;

        double[] gammas = new double[count];
        gammas[0] = (values[2] - 2 * values[1] + values[0]) / dsSquared;
        gammas[^1] = (values[^1] - 2 * values[^2] + values[^3]) / dsSquared;

        for (int i = 1; i < count - 1; i++)
        {
            gammas[i] = (values[i + 1] - 2 * values[i] + values[i - 1]) / dsSquared;
        }

        return gammas;
    }
}
