using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using ElectronicJournal.Models.Dto;

namespace ElectronicJournal.Services;

public sealed class NotificationService
{
    public static NotificationService Instance { get; } = new();

    private NotificationService()
    {
    }

    public ObservableCollection<AppNotification> Notifications { get; } = new();

    public void Success(string message, string title = "Готово") =>
        Show(AppNotificationKind.Success, title, message);

    public void Warning(string message, string title = "Проверьте") =>
        Show(AppNotificationKind.Warning, title, message, 5);

    public void Error(string message, string title = "Ошибка") =>
        Show(AppNotificationKind.Error, title, message, 6);

    public void Info(string message, string title = "Информация") =>
        Show(AppNotificationKind.Info, title, message);

    private void Show(AppNotificationKind kind, string title, string message, double seconds = 4)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var notification = new AppNotification(Guid.NewGuid(), kind, title, message.Trim());
        if (Dispatcher.UIThread.CheckAccess())
        {
            Notifications.Add(notification);
        }
        else
        {
            Dispatcher.UIThread.Post(() => Notifications.Add(notification));
        }

        _ = RemoveLater(notification, seconds);
    }

    private async Task RemoveLater(AppNotification notification, double seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        await Dispatcher.UIThread.InvokeAsync(() => Notifications.Remove(notification));
    }
}
