using System;

namespace QRev.Helper
{
    public static class DecimalExtensions
    {
        public static double AsDouble(this Decimal value)
        {
            return Decimal.ToDouble(value);
        }
    }
}
