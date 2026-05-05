using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ElectronicJournal.Utilities;

public sealed class StatusBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Новое" => Brushes.Firebrick,
            "Прочитано" => Brushes.DarkGoldenrod,
            "Закрыто" => Brushes.Gray,
            "Обучается" => Brushes.ForestGreen,
            "Отчислен" or "Выпустился" => Brushes.Gray,
            "Академический отпуск" or "Переведен" => Brushes.DarkGoldenrod,
            _ => Brushes.DimGray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
