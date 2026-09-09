namespace Lyo.QRCode.Encoding.Iso;

internal sealed partial class QRIsoEncoder
{
    /// <summary>
    /// One term of a polynomial: a coefficient and an exponent. For example, 3x² is a <see cref="PolynomItem" /> with coefficient 3 and exponent 2.
    /// </summary>
    private struct PolynomItem
    {
        /// <summary>Builds a <see cref="PolynomItem" /> with the given coefficient and exponent.</summary>
        /// <param name="coefficient">Coefficient of the term. In 3x² this is 3.</param>
        /// <param name="exponent">Exponent of the term. In 3x² this is 2.</param>
        public PolynomItem(int coefficient, int exponent)
        {
            Coefficient = coefficient;
            Exponent = exponent;
        }

        /// <summary>Coefficient of the polynomial term.</summary>
        public int Coefficient { get; }

        /// <summary>Exponent of the polynomial term.</summary>
        public int Exponent { get; }
    }
}