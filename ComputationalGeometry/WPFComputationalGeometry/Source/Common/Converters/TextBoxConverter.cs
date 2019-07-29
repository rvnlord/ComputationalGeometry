using System;
using System.Windows.Controls;

namespace WPFComputationalGeometry.Source.Common.Converters
{
    public static class TextBoxConverter
    {
        public static NumericBox ToNumericBox(this TextBox txtBox, int decimals, decimal minVal, decimal maxVal, string currency = "")
        {
            return new NumericBox(txtBox, decimals, minVal, maxVal, currency);
        }

        public static NumericBox ToNumericBox(this TextBox txtBox, int decimals)
        {
            return new NumericBox(txtBox, decimals, decimal.MinValue, decimal.MaxValue, "");
        }
    }
}
