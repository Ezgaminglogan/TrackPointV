using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TrackPointV.Converters
{
    public class ActivityTypeToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string type)
            {
                return type switch
                {
                    "Product" => "\uf466", // box-open
                    "Sale" => "\uf155",    // dollar-sign
                    "User" => "\uf007",    // user
                    _ => "\uf05a"          // info-circle
                };
            }
            return "\uf05a"; // Default icon
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActivityTypeToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string type)
            {
                return type switch
                {
                    "Product" => Color.FromArgb("#4CAF50"), // Green
                    "Sale" => Color.FromArgb("#1a73e8"),    // Blue
                    "User" => Color.FromArgb("#9C27B0"),    // Purple
                    _ => Color.FromArgb("#757575")          // Gray
                };
            }
            return Color.FromArgb("#757575"); // Default color
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}