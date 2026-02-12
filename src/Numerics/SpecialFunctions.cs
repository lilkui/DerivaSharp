using System.Runtime.CompilerServices;

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

    // Cephes coefficients for erf on [0, 1].
    private static ReadOnlySpan<double> ErfT =>
    [
        9.60497373987051638749E0,
        9.00260197203842689217E1,
        2.23200534594684319226E3,
        7.00332514112805075473E3,
        5.55923013010394962768E4,
    ];

    // Cephes coefficients for erf denominator on [0, 1].
    private static ReadOnlySpan<double> ErfU =>
    [
        3.35617141647503099647E1,
        5.21357949780152679795E2,
        4.59432382970980127987E3,
        2.26290000613890934246E4,
        4.92673942608635921086E4,
    ];

    // Cephes coefficients for erfc on [1, 8].
    private static ReadOnlySpan<double> ErfcP =>
    [
        2.46196981473530512524E-10,
        5.64189564831068821977E-1,
        7.46321056442269912687E0,
        4.86371970985681366614E1,
        1.96520832956077098242E2,
        5.26445194995477358631E2,
        9.34528527171957607540E2,
        1.02755188689515710272E3,
        5.57535335369399327526E2,
    ];

    // Cephes coefficients for erfc denominator on [1, 8].
    private static ReadOnlySpan<double> ErfcQ =>
    [
        1.32281951154744992508E1,
        8.67072140885989742329E1,
        3.54937778887819891062E2,
        9.75708501743205489753E2,
        1.82390916687909736289E3,
        2.24633760818710981792E3,
        1.65666309194161350182E3,
        5.57535340817727675546E2,
    ];

    // Cephes coefficients for erfc on [8, +∞).
    private static ReadOnlySpan<double> ErfcR =>
    [
        5.64189583547755073984E-1,
        1.27536670759978104416E0,
        5.01905042251180477414E0,
        6.16021097993053585195E0,
        7.40974269950448939160E0,
        2.97886665372100240670E0,
    ];

    // Cephes coefficients for erfc denominator on [8, +∞).
    private static ReadOnlySpan<double> ErfcS =>
    [
        2.26052863220117276590E0,
        9.39603524938001434673E0,
        1.20489539808096656605E1,
        1.70814450747565897222E1,
        9.60896809063285878198E0,
        3.36907645100081516050E0,
    ];

    /// <summary>
    ///     Computes the complementary error function erfc(x) = 1 - erf(x).
    /// </summary>
    /// <param name="x">The input value.</param>
    /// <returns>The value of erfc(x), computed using rational approximations from Cephes.</returns>
    public static double Erfc(double x)
    {
        if (double.IsNaN(x))
        {
            return double.NaN;
        }

        if (double.IsNegativeInfinity(x))
        {
            return 2;
        }

        if (double.IsPositiveInfinity(x))
        {
            return 0;
        }

        double ax = Math.Abs(x);
        if (ax < 1)
        {
            return 1 - Erf(x);
        }

        double z = -x * x;
        if (z < -MaxLog)
        {
            return x < 0 ? 2 : 0;
        }

        double p;
        double q;
        if (ax < 8)
        {
            p = Polevl(ax, ErfcP);
            q = P1evl(ax, ErfcQ);
        }
        else
        {
            p = Polevl(ax, ErfcR);
            q = P1evl(ax, ErfcS);
        }

        double y = Math.Exp(z) * p / q;
        return x < 0 ? 2 - y : y;
    }

    /// <summary>
    ///     Computes the error function erf(x).
    /// </summary>
    private static double Erf(double x)
    {
        double ax = Math.Abs(x);
        if (ax > 1)
        {
            return 1 - Erfc(x);
        }

        double z = x * x;
        return x * Polevl(z, ErfT) / P1evl(z, ErfU);
    }

    /// <summary>
    ///     Evaluates a polynomial at x using coefficients in normal order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Polevl(double x, ReadOnlySpan<double> coefficients)
    {
        double result = coefficients[0];
        for (int i = 1; i < coefficients.Length; i++)
        {
            result = Math.FusedMultiplyAdd(result, x, coefficients[i]);
        }

        return result;
    }

    /// <summary>
    ///     Evaluates a polynomial at x assuming a leading coefficient of 1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double P1evl(double x, ReadOnlySpan<double> coefficients)
    {
        double result = x + coefficients[0];
        for (int i = 1; i < coefficients.Length; i++)
        {
            result = Math.FusedMultiplyAdd(result, x, coefficients[i]);
        }

        return result;
    }
}
