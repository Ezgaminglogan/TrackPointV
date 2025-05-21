using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TrackPointV.Converters
{
    public class ActivityTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type switch
                {
                    "Product" => "\uf466", // box-open
                    "Sale" => "\u20b1",    // ph-sign
                    "User" => "\uf007",    // user
                    "Inventory" => "\uf07b", // folder
                    "Purchase" => "\uf07a", // shopping-cart
                    "Login" => "\uf2f6",    // sign-in
                    _ => "\uf05a"          // info-circle
                };
            }
            return "\uf05a"; // Default icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActivityTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type)
            {
                return type switch
                {
                    "Product" => Colors.Green,  
                    "Sale" => Colors.Purple,
                    "User" => Colors.Purple,
                    "Inventory" => Colors.Teal,
                    "Purchase" => Colors.Blue,
                    "Login" => Colors.Blue,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}