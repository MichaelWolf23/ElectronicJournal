using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ElectronicJournal.Utilities;

public sealed class NumberStateConverter : IValueConverter
{
    public bool IsZero { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var number = value switch
        {
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            _ => 0
        };

        var hasValue = number > 0;
        return IsZero ? !hasValue : hasValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
