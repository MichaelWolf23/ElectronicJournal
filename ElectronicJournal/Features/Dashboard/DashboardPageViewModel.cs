using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public sealed partial class DashboardPageViewModel : PageViewModelBase
{
    private readonly AuthenticatedUser _currentUser;

    public DashboardPageViewModel(
        AuthenticatedUser currentUser,
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        UserRepository userRepository)
        : base("Главная")
    {
        _currentUser = currentUser;
        Greeting = $"Здравствуйте, {currentUser.FullName}";
        RoleText = $"Роль: {currentUser.RoleName}";
        Description = GetRoleDescription(currentUser.RoleName);

        LoadMetrics(
            studentRepository,
            groupRepository,
            assignmentRepository,
            lessonRepository,
            gradeRepository,
            notificationRepository,
            settingsRepository,
            userRepository);
        LoadActions();
        LoadRoleWorkspace(
            studentRepository,
            groupRepository,
            assignmentRepository,
            lessonRepository,
            gradeRepository,
            notificationRepository,
            settingsRepository,
            userRepository);
    }

    public string Greeting { get; }

    public string RoleText { get; }

    public string Description { get; }

    public string WorkdayTitle { get; private set; } = "Рабочий день";

    public string PrimaryScenarioTitle { get; private set; } = "Начать работу";

    public string PrimaryScenarioText { get; private set; } = "Выберите действие, которое нужно выполнить сейчас.";

    public string AttentionTitle { get; private set; } = "Требует внимания";

    public ObservableCollection<DashboardMetricItem> Metrics { get; } = new();

    public ObservableCollection<DashboardActionItem> Actions { get; } = new();

    public ObservableCollection<DashboardWorkItem> PriorityItems { get; } = new();

    public ObservableCollection<DashboardWorkItem> AttentionItems { get; } = new();

    public event Action<string>? NavigateRequested;

    private void LoadMetrics(
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        UserRepository userRepository)
    {
        var minPositiveGrade = settingsRepository.GetMinPositiveGrade();

        switch (_currentUser.RoleName)
        {
            case "Преподаватель":
                Metrics.Add(new DashboardMetricItem(
                    "Назначения",
                    assignmentRepository.GetAssignmentLookupsForTeacher(_currentUser.UserId).Count.ToString(),
                    "группы и предметы преподавателя"));
                Metrics.Add(new DashboardMetricItem(
                    "Темы и расписание",
                    lessonRepository.GetLessonsForTeacher(_currentUser.UserId).Count.ToString(),
                    "созданные темы занятий"));
                Metrics.Add(new DashboardMetricItem(
                    "Студенты риска",
                    gradeRepository.GetDebtorsForTeacher(minPositiveGrade, _currentUser.UserId).Count.ToString(),
                    "оценки ниже минимальной"));
                break;

            case "Куратор группы":
                Metrics.Add(new DashboardMetricItem(
                    "Группы",
                    groupRepository.GetGroupsForCurator(_currentUser.UserId).Count.ToString(),
                    "закреплены за куратором"));
                Metrics.Add(new DashboardMetricItem(
                    "Карточки студентов",
                    studentRepository.GetStudentsForCurator(_currentUser.UserId).Count.ToString(),
                    "в закрепленных группах"));
                Metrics.Add(new DashboardMetricItem(
                    "Уведомления",
                    notificationRepository.GetNotificationsByCurator(_currentUser.UserId).Count.ToString(),
                    "сообщения по успеваемости"));
                break;

            case "Администратор":
                Metrics.Add(new DashboardMetricItem(
                    "Пользователи",
                    userRepository.GetUsers().Count.ToString(),
                    "учетные записи системы"));
                Metrics.Add(new DashboardMetricItem(
                    "Карточки студентов",
                    studentRepository.GetStudents().Count.ToString(),
                    "записи электронного журнала"));
                Metrics.Add(new DashboardMetricItem(
                    "Настройки",
                    settingsRepository.GetSettings().Count.ToString(),
                    "параметры приложения"));
                break;

            default:
                Metrics.Add(new DashboardMetricItem(
                    "Разделы",
                    "1",
                    "доступное рабочее место"));
                break;
        }
    }

    private void LoadActions()
    {
        switch (_currentUser.RoleName)
        {
            case "Преподаватель":
                AddAction("Внести оценку", "Открыть журнал оценок и добавить результат студенту.", "Оценивание");
                AddAction("Отметить посещаемость", "Выбрать занятие и отметить присутствующих.", "Посещаемость");
                AddAction("Создать занятие", "Добавить тему занятия по своей группе и предмету.", "Темы и расписание");
                AddAction("Проверить должников", "Найти студентов с оценками ниже проходного уровня.", "Студенты риска");
                break;

            case "Куратор группы":
                AddAction("Посмотреть статистику", "Оценить средний балл и количество проблемных студентов.", "Мои группы");
                AddAction("Открыть уведомления", "Разобрать новые сообщения от преподавателей.", "Уведомления");
                AddAction("Проверить должников", "Посмотреть отстающих студентов своих групп.", "Студенты риска");
                AddAction("Список студентов", "Открыть студентов закрепленных групп.", "Карточки студентов");
                break;

            case "Администратор":
                AddAction("Управлять пользователями", "Создать учетную запись, изменить роль или отключить доступ.", "Пользователи");
                AddAction("Открыть студентов", "Добавить или исправить данные студента.", "Студенты и группы");
                AddAction("Изменить настройки", "Настроить период, шкалу оценок и системные параметры.", "Настройки");
                AddAction("Проверить студентов", "Проверить общую картину по студентам и группам.", "Студенты и группы");
                break;

            default:
                AddAction("Открыть студентов", "Перейти к доступному разделу.", "Карточки студентов");
                break;
        }
    }

    private void AddAction(string title, string description, string targetSection)
    {
        Actions.Add(new DashboardActionItem(
            title,
            description,
            targetSection,
            new RelayCommand(() => NavigateRequested?.Invoke(targetSection))));
    }

    private void LoadRoleWorkspace(
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        UserRepository userRepository)
    {
        var minPositiveGrade = settingsRepository.GetMinPositiveGrade();

        switch (_currentUser.RoleName)
        {
            case "Преподаватель":
                WorkdayTitle = "Рабочий стол преподавателя";
                PrimaryScenarioTitle = "Провести учебный день";
                PrimaryScenarioText = "Откройте занятие, отметьте посещаемость, внесите оценки и сразу проверьте проблемные результаты.";
                AttentionTitle = "Контроль группы";
                AddPriority("Журнал занятия", "Отметить посещаемость и оценки в одном месте.", "Пара", "accent", "Журнал занятия");
                AddPriority("Отметить посещаемость", "Зафиксировать присутствующих, отсутствующих и опоздавших.", "Пара", "success", "Посещаемость");
                AddPriority("Добавить тему", "Записать дату, тему и аудиторию занятия.", "Тема", "warning", "Темы и расписание");
                AddAttention(
                    "Студенты риска",
                    $"{gradeRepository.GetDebtorsForTeacher(minPositiveGrade, _currentUser.UserId).Count} записей ниже проходного балла.",
                    "Проверить",
                    "danger",
                    "Студенты риска");
                AddAttention("Итоговые", "Рассчитать ведомость за период.", "Семестр", "accent", "Итоговые");
                break;

            case "Куратор группы":
                WorkdayTitle = "Рабочий стол куратора";
                PrimaryScenarioTitle = "Контролировать группу";
                PrimaryScenarioText = "Разбирайте уведомления, смотрите статистику и быстро находите студентов, которым нужна помощь.";
                AttentionTitle = "Сигналы по студентам";
                AddPriority("Уведомления", "Разобрать сообщения преподавателей по успеваемости.", "Входящие", "accent", "Уведомления");
                AddPriority("Мои группы", "Понять средний балл, должников и группы риска.", "Аналитика", "success", "Мои группы");
                AddPriority("Карточки студентов", "Открыть состав закрепленных групп.", "Группы", "warning", "Карточки студентов");
                AddAttention(
                    "Сообщения",
                    $"{notificationRepository.GetNotificationsByCurator(_currentUser.UserId).Count} уведомлений в журнале куратора.",
                    "Открыть",
                    "accent",
                    "Уведомления");
                AddAttention("Студенты риска", "Студенты закрепленных групп с оценками ниже проходного уровня.", "Проверить", "danger", "Студенты риска");
                break;

            case "Администратор":
                WorkdayTitle = "Рабочий стол администратора";
                PrimaryScenarioTitle = "Поддерживать систему";
                PrimaryScenarioText = "Управляйте пользователями, студентами и настройками, чтобы журнал был готов к работе.";
                AttentionTitle = "Администрирование";
                AddPriority("Пользователь", "Добавить учетную запись и назначить роль.", "Доступ", "accent", "Пользователи");
                AddPriority("Студент", "Исправить группу, контакты или статус.", "Данные", "success", "Студенты и группы");
                AddPriority("Настройки", "Проверить период и шкалу оценок.", "Параметры", "warning", "Настройки");
                AddAttention("Пользователи", $"{userRepository.GetUsers().Count} учетных записей.", "Открыть", "accent", "Пользователи");
                AddAttention("Студенты", $"{studentRepository.GetStudents().Count} карточек студентов.", "Проверить", "success", "Студенты и группы");
                break;

            default:
                AddPriority("Открыть раздел", "Перейти к доступной работе с журналом.", "Старт", "accent", "Карточки студентов");
                break;
        }
    }

    private void AddPriority(string title, string description, string badge, string accent, string targetSection)
    {
        PriorityItems.Add(new DashboardWorkItem(
            title,
            description,
            badge,
            accent,
            targetSection,
            new RelayCommand(() => NavigateRequested?.Invoke(targetSection))));
    }

    private void AddAttention(string title, string description, string badge, string accent, string targetSection)
    {
        AttentionItems.Add(new DashboardWorkItem(
            title,
            description,
            badge,
            accent,
            targetSection,
            new RelayCommand(() => NavigateRequested?.Invoke(targetSection))));
    }

    private static string GetRoleDescription(string roleName)
    {
        return roleName switch
        {
            "Преподаватель" => "Здесь собраны быстрые действия для ведения занятий: оценки, посещаемость, темы и работа с должниками.",
            "Куратор группы" => "Здесь видно состояние закрепленных групп: статистика, должники и уведомления по успеваемости.",
            "Администратор" => "Здесь находятся основные действия по управлению пользователями, студентами и настройками системы.",
            _ => "Здесь собраны доступные разделы электронного журнала."
        };
    }
}
