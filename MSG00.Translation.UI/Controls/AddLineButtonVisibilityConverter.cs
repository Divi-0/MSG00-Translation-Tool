using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;

namespace MSG00.Translation.UI.Controls
{
    public class AddLineButtonVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ((ICollection)value!).Count < 3 ? true : false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
