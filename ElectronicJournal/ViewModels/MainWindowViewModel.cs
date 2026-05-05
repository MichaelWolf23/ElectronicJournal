using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Services;

namespace ElectronicJournal.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string databaseStatus = "Проверка подключения...";

    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private int tableCount;

    [ObservableProperty]
    private bool isDatabaseAvailable;

    [ObservableProperty]
    private PageViewModelBase? currentPage;

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public MainWindowViewModel()
        : this(new DatabaseService())
    {
    }

    public MainWindowViewModel(DatabaseService databaseService)
    {
        var health = databaseService.CheckConnection();

        IsDatabaseAvailable = health.IsAvailable;
        DatabasePath = health.DatabasePath;
        TableCount = health.TableCount;
        DatabaseStatus = health.IsAvailable
            ? $"База данных подключена. Найдено таблиц: {health.TableCount}."
            : $"Ошибка подключения к базе данных: {health.ErrorMessage}";

        InitializeNavigation();
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is not null)
        {
            CurrentPage = value.Page;
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedNavigationItem = item;
        CurrentPage = item.Page;
    }

    private void InitializeNavigation()
    {
        NavigationItems.Add(new NavigationItem(
            "Студенты",
            new PlaceholderPageViewModel("Студенты", "Список студентов, поиск, фильтр по группе и редактирование.")));
        NavigationItems.Add(new NavigationItem(
            "Оценки",
            new PlaceholderPageViewModel("Оценки", "Журнал оценок, добавление оценок и расчет среднего балла.")));
        NavigationItems.Add(new NavigationItem(
            "Посещаемость",
            new PlaceholderPageViewModel("Посещаемость", "Отметки присутствия, отсутствия, опоздания и уважительной причины.")));
        NavigationItems.Add(new NavigationItem(
            "Занятия",
            new PlaceholderPageViewModel("Занятия", "Темы занятий, расписание, группы, предметы и аудитории.")));
        NavigationItems.Add(new NavigationItem(
            "Должники",
            new PlaceholderPageViewModel("Должники", "Студенты с оценками ниже минимальной положительной.")));
        NavigationItems.Add(new NavigationItem(
            "Статистика",
            new PlaceholderPageViewModel("Статистика", "Сводные показатели по группам и успеваемости.")));
        NavigationItems.Add(new NavigationItem(
            "Уведомления",
            new PlaceholderPageViewModel("Уведомления", "Уведомления кураторам о задолженностях и проблемах.")));
        NavigationItems.Add(new NavigationItem(
            "Настройки",
            new PlaceholderPageViewModel("Настройки", "Параметры системы и текущий учебный период.")));

        SelectedNavigationItem = NavigationItems[0];
        CurrentPage = SelectedNavigationItem.Page;
    }
}
