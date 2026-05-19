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
            "Присутствовал" => Brushes.ForestGreen,
            "Отсутствовал" => Brushes.Firebrick,
            "Опоздал" or "Уважительная причина" => Brushes.DarkGoldenrod,
            "Не отмечен" => Brushes.Gray,
            "сохранено" or "сохранена" => Brushes.ForestGreen,
            "не сохранено" or "не сохранена" => Brushes.Firebrick,
            "изменено" => Brushes.DarkGoldenrod,
            "готово к сохранению" => Brushes.SteelBlue,
            "можно оформить" => Brushes.SteelBlue,
            "уже оформлена" => Brushes.ForestGreen,
            "Низкая оценка" => Brushes.Firebrick,
            "Посещаемость" => Brushes.DarkGoldenrod,
            "Обучается" => Brushes.ForestGreen,
            "Отчислен" or "Выпустился" => Brushes.Gray,
            "Академический отпуск" or "Переведен" => Brushes.DarkGoldenrod,
            _ => Brushes.DimGray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
