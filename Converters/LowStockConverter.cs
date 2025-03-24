using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace TrackPointV.Converters
{
    public class LowStockConverter : IValueConverter
    {
        public int Threshold { get; set; } = 10;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int stock)
            {
                return stock <= Threshold;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}