using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TrackPointV.Converters
{
    public class LowStockConverter : IValueConverter
    {
        public int Threshold { get; set; } = 10;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double percentage && targetType == typeof(Color))
            {
                double warningThreshold = 20.0;
                if (parameter is string paramStr && double.TryParse(paramStr, out double threshold))
                {
                    warningThreshold = threshold;
                }

                if (percentage > warningThreshold)
                {
                    // Critical level - red
                    return Color.FromArgb("#ef4444");
                }
                else if (percentage > warningThreshold / 2)
                {
                    // Warning level - orange
                    return Color.FromArgb("#f59e0b");
                }
                else
                {
                    // Normal level - default orange for low stock section
                    return Color.FromArgb("#fb923c");
                }
            }
            else if (value is int stock && targetType == typeof(bool))
            {
                // Original functionality for boolean output
                return stock <= Threshold;
            }

            // Fallback
            return targetType == typeof(Color) ? Colors.Gray : false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}