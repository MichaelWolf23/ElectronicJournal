using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public sealed partial class MyLessonsPageViewModel : PageViewModelBase
{
    private readonly LessonRepository lessonRepository;
    private readonly AuthenticatedUser currentUser;
    private List<LessonListItem> allLessons = new();
    private List<ScheduleItem> allSchedule = new();

    [ObservableProperty]
    private ObservableCollection<LessonListItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<LessonScheduleCard> scheduleCards = new();

    [ObservableProperty]
    private LessonListItem? selectedLesson;

    [ObservableProperty]
    private int lessonCount;

    [ObservableProperty]
    private int todayLessonCount;

    [ObservableProperty]
    private int upcomingLessonCount;

    [ObservableProperty]
    private int scheduleCount;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedLessonPeriodFilter = LessonPeriodFilters[0];

    [ObservableProperty]
    private string selectedLessonTitle = "Выберите занятие";

    [ObservableProperty]
    private string selectedLessonDetails = "После выбора здесь появятся группа, предмет, аудитория и примечание.";

    [ObservableProperty]
    private string selectedLessonNote = string.Empty;

    [ObservableProperty]
    private string selectedLessonActionText = "Выберите занятие, затем откройте нужное действие.";

    [ObservableProperty]
    private string scheduleSummary = "Расписание загружается.";

    [ObservableProperty]
    private string resultMessage = "Занятия загружены.";

    public static IReadOnlyList<string> LessonPeriodFilters { get; } =
    [
        "Все занятия",
        "Сегодня",
        "Ближайшие",
        "Прошедшие"
    ];

    public MyLessonsPageViewModel(LessonRepository lessonRepository, AuthenticatedUser currentUser)
        : base("Мои занятия")
    {
        this.lessonRepository = lessonRepository;
        this.currentUser = currentUser;
        Load();
    }

    public event Action<string>? NavigateRequested;

    public override void OnNavigatedTo()
    {
        Load();
    }

    partial void OnSelectedLessonChanged(LessonListItem? value)
    {
        if (value is null)
        {
            SelectedLessonTitle = "Выберите занятие";
            SelectedLessonDetails = "После выбора здесь появятся группа, предмет, аудитория и примечание.";
            SelectedLessonNote = string.Empty;
            SelectedLessonActionText = "Выберите занятие, затем откройте нужное действие.";
            return;
        }

        SelectedLessonTitle = $"{value.LessonDate}: {value.Topic}";
        SelectedLessonDetails = $"{value.GroupName} · {value.SubjectName} · {value.ClassroomName ?? "аудитория не указана"}";
        SelectedLessonNote = string.IsNullOrWhiteSpace(value.Note)
            ? "Примечаний нет."
            : value.Note;
        SelectedLessonActionText = "Откройте журнал, оценки или посещаемость для выбранного занятия.";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedLessonPeriodFilterChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            allLessons = currentUser.RoleName == "Преподаватель"
                ? lessonRepository.GetLessonsForTeacher(currentUser.UserId)
                : lessonRepository.GetLessons();
            allSchedule = currentUser.RoleName == "Преподаватель"
                ? lessonRepository.GetScheduleForTeacher(currentUser.UserId)
                : lessonRepository.GetSchedule();

            TodayLessonCount = allLessons.Count(IsToday);
            UpcomingLessonCount = allLessons.Count(IsUpcoming);
            ScheduleCount = allSchedule.Count;
            ScheduleCards = new ObservableCollection<LessonScheduleCard>(allSchedule.Select(ToScheduleCard));
            ScheduleSummary = ScheduleCount == 0
                ? "Расписание пока не заполнено."
                : $"Записей расписания: {ScheduleCount}.";
            ApplyFilters();
            ResultMessage = allLessons.Count == 0
                ? "Занятий пока нет. Добавьте тему через раздел тем и расписания."
                : $"Показано занятий: {Lessons.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить занятия: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenJournal() => OpenSelectedLessonSection("Журнал занятия");

    [RelayCommand]
    private void OpenGrades() => OpenSelectedLessonSection("Оценивание");

    [RelayCommand]
    private void OpenAttendance() => OpenSelectedLessonSection("Посещаемость");

    [RelayCommand]
    private void OpenTopics() => NavigateRequested?.Invoke("Темы и расписание");

    private void OpenSelectedLessonSection(string section)
    {
        if (SelectedLesson is null)
        {
            NotifyWarning("Сначала выберите занятие.");
            return;
        }

        NavigateRequested?.Invoke(section);
    }

    private void ApplyFilters()
    {
        IEnumerable<LessonListItem> filtered = allLessons;
        var query = SearchText.Trim();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(lesson =>
                Contains(lesson.Topic, query) ||
                Contains(lesson.GroupName, query) ||
                Contains(lesson.SubjectName, query) ||
                Contains(lesson.ClassroomName, query) ||
                Contains(lesson.Note, query));
        }

        filtered = SelectedLessonPeriodFilter switch
        {
            "Сегодня" => filtered.Where(IsToday),
            "Ближайшие" => filtered.Where(IsUpcoming),
            "Прошедшие" => filtered.Where(IsPast),
            _ => filtered
        };

        var selectedId = SelectedLesson?.LessonId;
        var visible = filtered.ToList();
        Lessons = new ObservableCollection<LessonListItem>(visible);
        LessonCount = visible.Count;
        SelectedLesson = visible.FirstOrDefault(lesson => lesson.LessonId == selectedId) ?? visible.FirstOrDefault();
        ResultMessage = visible.Count == 0
            ? "Нет занятий по выбранным условиям."
            : $"Показано занятий: {visible.Count}.";
    }

    private static LessonScheduleCard ToScheduleCard(ScheduleItem item)
    {
        return new LessonScheduleCard(
            GetDayName(item.DayOfWeek),
            $"{item.StartTime}-{item.EndTime}",
            item.GroupName,
            item.SubjectName,
            item.ClassroomName ?? "аудитория не указана");
    }

    private static bool IsToday(LessonListItem lesson)
    {
        return DateTime.TryParse(lesson.LessonDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date.Date == DateTime.Today;
    }

    private static bool IsUpcoming(LessonListItem lesson)
    {
        return DateTime.TryParse(lesson.LessonDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date.Date >= DateTime.Today;
    }

    private static bool IsPast(LessonListItem lesson)
    {
        return DateTime.TryParse(lesson.LessonDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            && date.Date < DateTime.Today;
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "Понедельник",
            2 => "Вторник",
            3 => "Среда",
            4 => "Четверг",
            5 => "Пятница",
            6 => "Суббота",
            7 => "Воскресенье",
            _ => $"День {dayOfWeek}"
        };
    }
}
