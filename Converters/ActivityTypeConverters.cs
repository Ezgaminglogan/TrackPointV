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
                    "Inventory" => "\uf07b", // folder
                    "Purchase" => "\uf07a", // shopping-cart
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
                    "Product" => Color.FromArgb("#22c55e"),  // Green
                    "Sale" => Color.FromArgb("#6366f1"),     // Indigo/Purple (primary)
                    "User" => Color.FromArgb("#9333ea"),     // Purple
                    "Inventory" => Color.FromArgb("#14b8a6"), // Teal
                    "Purchase" => Color.FromArgb("#0ea5e9"), // Sky blue
                    _ => Color.FromArgb("#64748b")           // Slate
                };
            }
            return Color.FromArgb("#64748b"); // Default color
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}