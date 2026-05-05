using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Repositories;
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
    private string currentPeriodName = "Не задан";

    [ObservableProperty]
    private string operationStatus = "Готово";

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
        var settingsRepository = new SettingsRepository(databaseService);
        var studentRepository = new StudentRepository(databaseService);
        var groupRepository = new GroupRepository(databaseService);
        var subjectRepository = new SubjectRepository(databaseService);
        var gradeRepository = new GradeRepository(databaseService);
        var gradeTypeRepository = new GradeTypeRepository(databaseService);
        var gradeRetakeRepository = new GradeRetakeRepository(databaseService);
        var assignmentRepository = new AssignmentRepository(databaseService);
        var lessonRepository = new LessonRepository(databaseService);
        var attendanceRepository = new AttendanceRepository(databaseService);

        IsDatabaseAvailable = health.IsAvailable;
        DatabasePath = health.DatabasePath;
        TableCount = health.TableCount;
        DatabaseStatus = health.IsAvailable
            ? $"База данных подключена. Найдено таблиц: {health.TableCount}."
            : $"Ошибка подключения к базе данных: {health.ErrorMessage}";
        CurrentPeriodName = health.IsAvailable
            ? settingsRepository.GetValue("Текущий учебный период") ?? "Не задан"
            : "Недоступен";
        OperationStatus = health.IsAvailable
            ? "Данные загружены. Выберите раздел в левом меню."
            : "Ошибка: база данных недоступна.";

        InitializeNavigation(
            studentRepository,
            groupRepository,
            subjectRepository,
            gradeRepository,
            gradeTypeRepository,
            gradeRetakeRepository,
            assignmentRepository,
            lessonRepository,
            attendanceRepository);
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is not null)
        {
            CurrentPage = value.Page;
            OperationStatus = $"Открыт раздел: {value.Title}";
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedNavigationItem = item;
        CurrentPage = item.Page;
    }

    private void InitializeNavigation(
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        SubjectRepository subjectRepository,
        GradeRepository gradeRepository,
        GradeTypeRepository gradeTypeRepository,
        GradeRetakeRepository gradeRetakeRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        AttendanceRepository attendanceRepository)
    {
        NavigationItems.Add(new NavigationItem(
            "Студенты",
            new StudentsPageViewModel(studentRepository, groupRepository)));
        NavigationItems.Add(new NavigationItem(
            "Оценки",
            new GradesPageViewModel(
                gradeRepository,
                studentRepository,
                groupRepository,
                subjectRepository,
                gradeTypeRepository,
                assignmentRepository,
                lessonRepository)));
        NavigationItems.Add(new NavigationItem(
            "Пересдачи",
            new RetakesPageViewModel(gradeRepository, gradeRetakeRepository)));
        NavigationItems.Add(new NavigationItem(
            "Посещаемость",
            new AttendancePageViewModel(
                attendanceRepository,
                lessonRepository,
                studentRepository,
                groupRepository,
                subjectRepository)));
        NavigationItems.Add(new NavigationItem(
            "Занятия",
            new LessonsPageViewModel(lessonRepository, assignmentRepository)));
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
