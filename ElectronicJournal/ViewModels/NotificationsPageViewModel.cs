using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class NotificationsPageViewModel : PageViewModelBase
{
    private readonly NotificationRepository notificationRepository;
    private List<CuratorNotificationItem> allNotifications = new();

    [ObservableProperty]
    private ObservableCollection<CuratorNotificationItem> notifications = new();

    [ObservableProperty]
    private CuratorNotificationItem? selectedNotification;

    [ObservableProperty]
    private string selectedStatusFilter = StatusFilters[0];

    [ObservableProperty]
    private int newCount;

    [ObservableProperty]
    private int readCount;

    [ObservableProperty]
    private int closedCount;

    [ObservableProperty]
    private string resultMessage = "Уведомления кураторам.";

    public static IReadOnlyList<string> StatusFilters { get; } =
    [
        "Все",
        "Новое",
        "Прочитано",
        "Закрыто"
    ];

    public NotificationsPageViewModel(NotificationRepository notificationRepository)
        : base("Уведомления")
    {
        this.notificationRepository = notificationRepository;
        Load();
    }

    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            allNotifications = notificationRepository.GetNotifications();
            UpdateCounters();
            ApplyFilter();
            ResultMessage = $"Загружено уведомлений: {allNotifications.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить уведомления: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void MarkAsNew() => UpdateSelectedStatus("Новое");

    [RelayCommand]
    private void MarkAsRead() => UpdateSelectedStatus("Прочитано");

    [RelayCommand]
    private void MarkAsClosed() => UpdateSelectedStatus("Закрыто");

    private void UpdateSelectedStatus(string status)
    {
        if (SelectedNotification is null)
        {
            ResultMessage = "Сначала выберите уведомление.";
            return;
        }

        try
        {
            notificationRepository.UpdateStatus(SelectedNotification.NotificationId, status);
            ResultMessage = $"Статус уведомления изменен на \"{status}\".";
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось изменить статус: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<CuratorNotificationItem> filtered = allNotifications;

        if (SelectedStatusFilter != "Все")
        {
            filtered = filtered.Where(item => item.Status == SelectedStatusFilter);
        }

        Notifications = new ObservableCollection<CuratorNotificationItem>(filtered);
    }

    private void UpdateCounters()
    {
        NewCount = allNotifications.Count(item => item.Status == "Новое");
        ReadCount = allNotifications.Count(item => item.Status == "Прочитано");
        ClosedCount = allNotifications.Count(item => item.Status == "Закрыто");
    }
}
