using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ElectronicJournal.Models.Dto;

namespace ElectronicJournal.Utilities;

public sealed class NotificationBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var kind = value is AppNotificationKind notificationKind
            ? notificationKind
            : AppNotificationKind.Info;
        var role = parameter?.ToString() ?? "background";

        return (kind, role) switch
        {
            (AppNotificationKind.Success, "background") => Brush.Parse("#EAF7EF"),
            (AppNotificationKind.Success, "border") => Brush.Parse("#16803C"),
            (AppNotificationKind.Success, "foreground") => Brush.Parse("#166534"),

            (AppNotificationKind.Warning, "background") => Brush.Parse("#FFF7ED"),
            (AppNotificationKind.Warning, "border") => Brush.Parse("#B45309"),
            (AppNotificationKind.Warning, "foreground") => Brush.Parse("#92400E"),

            (AppNotificationKind.Error, "background") => Brush.Parse("#FEF2F2"),
            (AppNotificationKind.Error, "border") => Brush.Parse("#DC2626"),
            (AppNotificationKind.Error, "foreground") => Brush.Parse("#991B1B"),

            (_, "background") => Brush.Parse("#EFF6FF"),
            (_, "border") => Brush.Parse("#2563EB"),
            (_, "foreground") => Brush.Parse("#1D4ED8"),
            _ => Brush.Parse("#EFF6FF")
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
