using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class NotificationsPageViewModel : PageViewModelBase
{
    private readonly NotificationRepository notificationRepository;
    private readonly AuthenticatedUser currentUser;
    private List<CuratorNotificationItem> allNotifications = new();

    [ObservableProperty]
    private ObservableCollection<CuratorNotificationItem> notifications = new();

    [ObservableProperty]
    private CuratorNotificationItem? selectedNotification;

    [ObservableProperty]
    private string selectedStatusFilter = StatusFilters[0];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int newCount;

    [ObservableProperty]
    private int readCount;

    [ObservableProperty]
    private int closedCount;

    [ObservableProperty]
    private int visibleNotificationCount;

    [ObservableProperty]
    private string selectedNotificationTitle = "Выберите уведомление";

    [ObservableProperty]
    private string selectedNotificationDetails = "После выбора строки здесь появятся студент, группа и дата.";

    [ObservableProperty]
    private string selectedNotificationMessage = "Текст уведомления не выбран.";

    [ObservableProperty]
    private string resultMessage = "Уведомления кураторам.";

    public static IReadOnlyList<string> StatusFilters { get; } =
    [
        "Все",
        "Новое",
        "Прочитано",
        "Закрыто"
    ];

    public NotificationsPageViewModel(
        NotificationRepository notificationRepository,
        AuthenticatedUser currentUser)
        : base("Уведомления")
    {
        this.notificationRepository = notificationRepository;
        this.currentUser = currentUser;
        Load();
    }

    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedNotificationChanged(CuratorNotificationItem? value)
    {
        if (value is null)
        {
            SelectedNotificationTitle = "Выберите уведомление";
            SelectedNotificationDetails = "После выбора строки здесь появятся студент, группа и дата.";
            SelectedNotificationMessage = "Текст уведомления не выбран.";
            return;
        }

        SelectedNotificationTitle = $"{value.Title} - {value.Status}";
        SelectedNotificationDetails =
            $"Куратор: {value.CuratorName}. Студент: {value.StudentName ?? "не указан"}. " +
            $"Группа: {value.GroupName ?? "не указана"}. Создано: {value.CreatedAt}.";
        SelectedNotificationMessage = value.Message;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            allNotifications = currentUser.RoleName == "Куратор группы"
                ? notificationRepository.GetNotificationsByCurator(currentUser.UserId)
                : notificationRepository.GetNotifications();
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

    [RelayCommand]
    private async Task DeleteSelectedNotification()
    {
        if (SelectedNotification is null)
        {
            ResultMessage = "Сначала выберите уведомление.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить уведомление",
            $"Удалить уведомление \"{SelectedNotification.Title}\"?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            notificationRepository.DeleteNotification(SelectedNotification.NotificationId);
            SelectedNotification = null;
            ResultMessage = "Уведомление удалено.";
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить уведомление: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

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

        var query = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(item =>
                Contains(item.Title, query) ||
                Contains(item.Message, query) ||
                Contains(item.CuratorName, query) ||
                Contains(item.StudentName, query) ||
                Contains(item.GroupName, query));
        }

        var visibleNotifications = filtered.ToList();
        Notifications = new ObservableCollection<CuratorNotificationItem>(visibleNotifications);
        VisibleNotificationCount = visibleNotifications.Count;
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private void UpdateCounters()
    {
        NewCount = allNotifications.Count(item => item.Status == "Новое");
        ReadCount = allNotifications.Count(item => item.Status == "Прочитано");
        ClosedCount = allNotifications.Count(item => item.Status == "Закрыто");
    }
}
