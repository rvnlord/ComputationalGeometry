using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WPFComputationalGeometry.Source.Common.Converters;
using WPFComputationalGeometry.Source.Common.Extensions;

namespace WPFComputationalGeometry.Source.Common
{
    public class NumericBox
    {
        public static Dictionary<TextBox, NumericBox> NumericBoxes = new Dictionary<TextBox, NumericBox>();

        public TextBox TxtBox { get; }
        public int DecimalPlaces { get; set; }
        public decimal MinValue { get; set; }
        public string CurrencySymbol { get; set; }
        public decimal MaxValue { get; set; }

        public decimal Value
        {
            get => TxtBox.Text.ToDecimal();
            set => TxtBox.Text = Math.Max(Math.Min(value, MaxValue), MinValue) + 
                                 (CurrencySymbol.IsNullOrWhiteSpace() ? "" : " " + CurrencySymbol);
        }

        public NumericBox(TextBox txtBox, int decimals, decimal minValue, decimal maxValue, string currency)
        {
            TxtBox = txtBox;
            DecimalPlaces = decimals;
            MinValue = minValue;
            MaxValue = maxValue;
            CurrencySymbol = currency;
            Value = 0;

            TxtBox.TextAlignment = TextAlignment.Right;
            TxtBox.GotFocus += NumericBox_GotFocus;
            TxtBox.LostFocus += NumericBox_LostFocus;

            NumericBoxes[TxtBox] = this;
        }

        private void NumericBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var txtBox = (TextBox)sender;

            txtBox.ResetValue().ClearValue();

            txtBox.Text = txtBox.Text.Remove(" " + CurrencySymbol);
            txtBox.LogicalAncestor<Window>().Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                txtBox.Focus();
                txtBox.CaretIndex = txtBox.Text.Length;
            }));
        }

        private void NumericBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var txtBox = (TextBox)sender;

            txtBox.ResetValue();

            var strVal = txtBox.Text;
            decimal val;
            if (!Regex.IsMatch(strVal, @"^[-]?([0-9]+(?:[\.][0-9]*)?|\.[0-9]+)$"))
                val = 0;
            else if (strVal.ToDecimal().DecimalPlaces() > DecimalPlaces)
                val = (strVal.BeforeFirst(".") + "." + strVal.AfterFirst(".").Take(DecimalPlaces)).ToDecimal();
            else
                val = strVal.ToDecimal();
            val = Math.Min(val, MaxValue);
            val = Math.Max(val, MinValue);
            txtBox.Text = val + (CurrencySymbol.IsNullOrWhiteSpace() ? "" : " " + CurrencySymbol);
        }
    }
}
