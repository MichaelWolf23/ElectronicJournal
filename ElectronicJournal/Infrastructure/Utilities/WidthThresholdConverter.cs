using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ElectronicJournal.Utilities;

public sealed class WidthThresholdConverter : IValueConverter
{
    public double Threshold { get; set; } = 900;

    public bool IsLessThan { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var width = value switch
        {
            double number => number,
            Rect bounds => bounds.Width,
            Size size => size.Width,
            _ => 0
        };

        var isWideEnough = width >= Threshold;
        return IsLessThan ? !isWideEnough : isWideEnough;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
