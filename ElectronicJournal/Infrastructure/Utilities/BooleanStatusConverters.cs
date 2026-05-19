using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ElectronicJournal.Utilities;

public sealed class BooleanStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool boolValue && boolValue ? "активен" : "отключен";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BooleanStatusForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool boolValue && boolValue
            ? new SolidColorBrush(Color.Parse("#16803C"))
            : new SolidColorBrush(Color.Parse("#DC2626"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BooleanStatusBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool boolValue && boolValue
            ? new SolidColorBrush(Color.Parse("#E9F8EF"))
            : new SolidColorBrush(Color.Parse("#FEF2F2"));

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
