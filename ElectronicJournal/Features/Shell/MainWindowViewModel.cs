using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
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
    private string currentUserName = "Пользователь";

    [ObservableProperty]
    private string currentUserRole = "Роль не задана";

    [ObservableProperty]
    private bool isDarkTheme;

    [ObservableProperty]
    private string themeButtonText = "Темная тема";

    [ObservableProperty]
    private PageViewModelBase? currentPage;

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    [ObservableProperty]
    private double sidebarWidth = 248;

    [ObservableProperty]
    private bool isSidebarExpanded = true;

    [ObservableProperty]
    private string sidebarToggleText = "<";

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public event Action? LogoutRequested;

    public MainWindowViewModel()
        : this(
            new DatabaseService(),
            new AuthenticatedUser(0, 0, "Администратор", "design", "Режим разработки", null))
    {
    }

    public MainWindowViewModel(DatabaseService databaseService, AuthenticatedUser currentUser)
    {
        CurrentUserName = currentUser.FullName;
        CurrentUserRole = currentUser.RoleName;
        IsDarkTheme = ThemeService.IsDarkTheme;
        ThemeButtonText = IsDarkTheme ? "Светлая тема" : "Темная тема";

        var health = databaseService.CheckConnection();
        IsDatabaseAvailable = health.IsAvailable;
        DatabasePath = health.DatabasePath;
        TableCount = health.TableCount;
        DatabaseStatus = health.IsAvailable
            ? $"База данных подключена. Найдено таблиц: {health.TableCount}."
            : $"Ошибка подключения к базе данных: {health.ErrorMessage}";

        if (!health.IsAvailable)
        {
            CurrentPeriodName = "Недоступен";
            OperationStatus = "База данных недоступна. Проверьте файл electronic_journal.db рядом с приложением.";
            var unavailablePage = new PlaceholderPageViewModel(
                "База данных недоступна",
                $"Приложение запущено, но не может открыть электронный журнал. {health.ErrorMessage} Путь: {health.DatabasePath}");
            NavigationItems.Add(new NavigationItem("Состояние", unavailablePage, NavigationIcons.Warning, "База недоступна"));
            SelectedNavigationItem = NavigationItems[0];
            CurrentPage = unavailablePage;
            return;
        }

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
        var notificationRepository = new NotificationRepository(databaseService);
        var finalGradeRepository = new FinalGradeRepository(databaseService);
        var userRepository = new UserRepository(databaseService);
        var backupService = new BackupService(databaseService);
        var reportRepository = new ReportRepository(databaseService);

        CurrentPeriodName = settingsRepository.GetValue("Текущий учебный период") ?? "Не задан";
        OperationStatus = "Данные загружены. Выберите раздел в левом меню.";

        InitializeNavigation(
            studentRepository,
            groupRepository,
            subjectRepository,
            gradeRepository,
            gradeTypeRepository,
            gradeRetakeRepository,
            assignmentRepository,
            lessonRepository,
            attendanceRepository,
            notificationRepository,
            settingsRepository,
            finalGradeRepository,
            userRepository,
            backupService,
            reportRepository,
            currentUser);
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is not null)
        {
            CurrentPage = value.Page;
            value.Page.OnNavigatedTo();
            OperationStatus = $"Открыт раздел: {value.Title}";
        }
    }

    [RelayCommand]
    private void Navigate(NavigationItem item)
    {
        SelectedNavigationItem = item;
        CurrentPage = item.Page;
    }

    [RelayCommand]
    private void Logout()
    {
        LogoutRequested?.Invoke();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = ThemeService.ToggleTheme();
        ThemeButtonText = IsDarkTheme ? "Светлая тема" : "Темная тема";
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        SidebarWidth = IsSidebarCollapsed ? 72 : 248;
        IsSidebarExpanded = !IsSidebarCollapsed;
        SidebarToggleText = IsSidebarCollapsed ? ">" : "<";
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
        AttendanceRepository attendanceRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        FinalGradeRepository finalGradeRepository,
        UserRepository userRepository,
        BackupService backupService,
        ReportRepository reportRepository,
        AuthenticatedUser currentUser)
    {
        var dashboardPage = new DashboardPageViewModel(
            currentUser,
            studentRepository,
            groupRepository,
            assignmentRepository,
            lessonRepository,
            gradeRepository,
            notificationRepository,
            settingsRepository,
            userRepository);
        dashboardPage.NavigateRequested += SelectSection;
        NavigationItems.Add(new NavigationItem("Главная", dashboardPage, NavigationIcons.Home, "Рабочий стол"));

        AddNavigationItem(currentUser, new NavigationItem(
            "Пользователи",
            new UsersPageViewModel(userRepository),
            NavigationIcons.Users,
            "Аккаунты и роли"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Студенты",
            new StudentsPageViewModel(studentRepository, groupRepository, currentUser),
            NavigationIcons.Student,
            "Карточки студентов"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Группы",
            new GroupsPageViewModel(groupRepository),
            NavigationIcons.Groups,
            "Курсы и группы"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Назначения",
            new AssignmentsPageViewModel(
                assignmentRepository,
                userRepository,
                groupRepository,
                subjectRepository),
            NavigationIcons.Assignment,
            "Преподаватели и кураторы"));
        var studentProfilePage = new StudentProfilePageViewModel(
            studentRepository,
            gradeRepository,
            attendanceRepository,
            settingsRepository,
            currentUser);
        studentProfilePage.NavigateRequested += SelectSection;
        AddNavigationItem(currentUser, new NavigationItem(
            "Карточки студентов",
            studentProfilePage,
            NavigationIcons.Student,
            "Состав группы"));

        var myLessonsPage = new MyLessonsPageViewModel(lessonRepository, currentUser);
        myLessonsPage.NavigateRequested += SelectSection;
        AddNavigationItem(currentUser, new NavigationItem(
            "Мои занятия",
            myLessonsPage,
            NavigationIcons.Calendar,
            "Пары и темы"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Оценивание",
            new GradesPageViewModel(
                gradeRepository,
                studentRepository,
                groupRepository,
                subjectRepository,
                gradeTypeRepository,
                assignmentRepository,
                lessonRepository,
                settingsRepository,
                currentUser),
            NavigationIcons.Grade,
            "Оценки студентов"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Журнал занятия",
            new LessonJournalPageViewModel(
                lessonRepository,
                studentRepository,
                attendanceRepository,
                gradeRepository,
                gradeTypeRepository,
                settingsRepository,
                reportRepository,
                currentUser),
            NavigationIcons.Journal,
            "Пара целиком"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Отчеты",
            new TeacherReportsPageViewModel(
                gradeRepository,
                studentRepository,
                settingsRepository,
                currentUser),
            NavigationIcons.Report,
            "Успеваемость"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Пересдачи",
            new RetakesPageViewModel(gradeRepository, gradeRetakeRepository, settingsRepository, currentUser),
            NavigationIcons.Retake,
            "История исправлений"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Посещаемость",
            new AttendancePageViewModel(
                attendanceRepository,
                lessonRepository,
                studentRepository,
                groupRepository,
                subjectRepository,
                currentUser),
            NavigationIcons.Attendance,
            "Отметки занятий"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Темы и расписание",
            new LessonsPageViewModel(lessonRepository, assignmentRepository, currentUser),
            NavigationIcons.Calendar,
            "Темы и расписание"));
        var riskStudentsPage = new RiskStudentsPageViewModel(
            gradeRepository,
            attendanceRepository,
            notificationRepository,
            settingsRepository,
            currentUser);
        riskStudentsPage.StudentProfileRequested += studentId =>
        {
            studentProfilePage.SelectStudentById(studentId);
            SelectSection("Карточки студентов");
        };
        AddNavigationItem(currentUser, new NavigationItem(
            "Студенты риска",
            riskStudentsPage,
            NavigationIcons.Warning,
            "Контроль рисков"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Мои группы",
            new MyGroupsPageViewModel(
                groupRepository,
                studentRepository,
                gradeRepository,
                notificationRepository,
                settingsRepository,
                currentUser),
            NavigationIcons.Analytics,
            "Аналитика групп"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Уведомления",
            new NotificationsPageViewModel(notificationRepository, currentUser),
            NavigationIcons.Bell,
            "Сообщения куратору"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Итоговые",
            new FinalGradesPageViewModel(
                finalGradeRepository,
                studentRepository,
                assignmentRepository,
                settingsRepository,
                currentUser),
            NavigationIcons.Final,
            "Ведомость периода"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Справочники",
            new ReferenceDataPageViewModel(
                groupRepository,
                subjectRepository,
                gradeTypeRepository,
                lessonRepository,
                assignmentRepository),
            NavigationIcons.Folder,
            "Учебные данные"));
        AddNavigationItem(currentUser, new NavigationItem(
            "Настройки",
            new SettingsPageViewModel(settingsRepository, backupService),
            NavigationIcons.Settings,
            "Параметры системы"));

        if (NavigationItems.Count > 0)
        {
            SelectedNavigationItem = NavigationItems[0];
            CurrentPage = SelectedNavigationItem.Page;
        }
    }

    private void AddNavigationItem(AuthenticatedUser user, NavigationItem item)
    {
        if (CanOpenSection(user.RoleName, item.Title))
        {
            NavigationItems.Add(item);
        }
    }

    private void SelectSection(string sectionTitle)
    {
        sectionTitle = NormalizeSectionTitle(sectionTitle);

        foreach (var item in NavigationItems)
        {
            if (item.Title == sectionTitle)
            {
                SelectedNavigationItem = item;
                return;
            }
        }

        OperationStatus = $"Раздел \"{sectionTitle}\" недоступен для текущей роли.";
    }

    private static bool CanOpenSection(string roleName, string title)
    {
        return roleName switch
        {
            "Администратор" => title is "Пользователи" or "Студенты" or "Группы" or "Назначения" or "Справочники" or "Настройки" or "Отчеты",
            "Преподаватель" => title is "Мои занятия" or "Журнал занятия" or "Оценивание" or "Отчеты" or "Пересдачи" or "Посещаемость" or "Темы и расписание" or "Студенты риска" or "Итоговые",
            "Куратор группы" => title is "Мои группы" or "Студенты риска" or "Уведомления" or "Карточки студентов",
            _ => title is "Карточки студентов"
        };
    }

    private static string NormalizeSectionTitle(string sectionTitle)
    {
        return sectionTitle switch
        {
            "Оценки" => "Оценивание",
            "Занятия" => "Темы и расписание",
            "Должники" => "Студенты риска",
            "Студенты и группы" => "Студенты",
            "Статистика" => "Мои группы",
            _ => sectionTitle
        };
    }
}
