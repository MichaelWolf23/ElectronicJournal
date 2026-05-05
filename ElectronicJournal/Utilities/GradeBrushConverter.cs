using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ElectronicJournal.Utilities;

public sealed class GradeBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not null && double.TryParse(value.ToString(), out var grade))
        {
            if (grade >= 4.5)
            {
                return Brushes.ForestGreen;
            }

            if (grade >= 3)
            {
                return Brushes.DarkGoldenrod;
            }
        }

        return Brushes.Firebrick;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
