namespace DerivaSharp.Numerics;

/// <summary>
///     Provides special mathematical functions.
/// </summary>
public static class SpecialFunctions
{
    /// <summary>
    ///     Maximum value for which exp(x) can be computed without overflow.
    /// </summary>
    private const double MaxLog = 7.09782712893383996843E2;

    /// <summary>
    ///     Computes the complementary error function erfc(x) = 1 - erf(x).
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <returns>The value of <c>erfc(x)</c>, computed using rational approximations from Cephes.</returns>
    public static double Erfc(double x)
    {
        double ax = Math.Abs(x);
        if (ax < 1)
        {
            double z = x * x;

            double numerator = Math.FusedMultiplyAdd(9.60497373987051638749E0, z, 9.00260197203842689217E1);
            numerator = Math.FusedMultiplyAdd(numerator, z, 2.23200534594684319226E3);
            numerator = Math.FusedMultiplyAdd(numerator, z, 7.00332514112805075473E3);
            numerator = Math.FusedMultiplyAdd(numerator, z, 5.55923013010394962768E4);

            double denominator = z + 3.35617141647503099647E1;
            denominator = Math.FusedMultiplyAdd(denominator, z, 5.21357949780152679795E2);
            denominator = Math.FusedMultiplyAdd(denominator, z, 4.59432382970980127987E3);
            denominator = Math.FusedMultiplyAdd(denominator, z, 2.26290000613890934246E4);
            denominator = Math.FusedMultiplyAdd(denominator, z, 4.92673942608635921086E4);
            return 1 - (x * numerator / denominator);
        }

        double exponent = -x * x;
        if (exponent < -MaxLog)
        {
            return x < 0 ? 2 : 0;
        }

        double p;
        double q;
        if (ax < 8)
        {
            p = Math.FusedMultiplyAdd(2.46196981473530512524E-10, ax, 5.64189564831068821977E-1);
            p = Math.FusedMultiplyAdd(p, ax, 7.46321056442269912687E0);
            p = Math.FusedMultiplyAdd(p, ax, 4.86371970985681366614E1);
            p = Math.FusedMultiplyAdd(p, ax, 1.96520832956077098242E2);
            p = Math.FusedMultiplyAdd(p, ax, 5.26445194995477358631E2);
            p = Math.FusedMultiplyAdd(p, ax, 9.34528527171957607540E2);
            p = Math.FusedMultiplyAdd(p, ax, 1.02755188689515710272E3);
            p = Math.FusedMultiplyAdd(p, ax, 5.57535335369399327526E2);

            q = ax + 1.32281951154744992508E1;
            q = Math.FusedMultiplyAdd(q, ax, 8.67072140885989742329E1);
            q = Math.FusedMultiplyAdd(q, ax, 3.54937778887819891062E2);
            q = Math.FusedMultiplyAdd(q, ax, 9.75708501743205489753E2);
            q = Math.FusedMultiplyAdd(q, ax, 1.82390916687909736289E3);
            q = Math.FusedMultiplyAdd(q, ax, 2.24633760818710981792E3);
            q = Math.FusedMultiplyAdd(q, ax, 1.65666309194161350182E3);
            q = Math.FusedMultiplyAdd(q, ax, 5.57535340817727675546E2);
        }
        else
        {
            p = Math.FusedMultiplyAdd(5.64189583547755073984E-1, ax, 1.27536670759978104416E0);
            p = Math.FusedMultiplyAdd(p, ax, 5.01905042251180477414E0);
            p = Math.FusedMultiplyAdd(p, ax, 6.16021097993053585195E0);
            p = Math.FusedMultiplyAdd(p, ax, 7.40974269950448939160E0);
            p = Math.FusedMultiplyAdd(p, ax, 2.97886665372100240670E0);

            q = ax + 2.26052863220117276590E0;
            q = Math.FusedMultiplyAdd(q, ax, 9.39603524938001434673E0);
            q = Math.FusedMultiplyAdd(q, ax, 1.20489539808096656605E1);
            q = Math.FusedMultiplyAdd(q, ax, 1.70814450747565897222E1);
            q = Math.FusedMultiplyAdd(q, ax, 9.60896809063285878198E0);
            q = Math.FusedMultiplyAdd(q, ax, 3.36907645100081516050E0);
        }

        double y = Math.Exp(exponent) * p / q;
        return x < 0 ? 2 - y : y;
    }
}
