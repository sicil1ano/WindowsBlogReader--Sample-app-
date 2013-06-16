using System;
using Windows.UI.Xaml.Data;

namespace WindowsBlogReader.Common
{
    /// <summary>
    /// Convertitore di valori che traduce true in false e viceversa.
    /// </summary>
    public sealed class BooleanNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(value is bool && (bool)value);
        }
    }
}
