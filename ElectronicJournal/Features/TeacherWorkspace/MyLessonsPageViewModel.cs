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
    private int scheduleCount;

    [ObservableProperty]
    private string selectedLessonTitle = "Выберите занятие";

    [ObservableProperty]
    private string selectedLessonDetails = "После выбора здесь появятся группа, предмет, аудитория и примечание.";

    [ObservableProperty]
    private string selectedLessonNote = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Занятия загружены.";

    public MyLessonsPageViewModel(LessonRepository lessonRepository, AuthenticatedUser currentUser)
        : base("Мои занятия")
    {
        this.lessonRepository = lessonRepository;
        this.currentUser = currentUser;
        Load();
    }

    public event Action<string>? NavigateRequested;

    partial void OnSelectedLessonChanged(LessonListItem? value)
    {
        if (value is null)
        {
            SelectedLessonTitle = "Выберите занятие";
            SelectedLessonDetails = "После выбора здесь появятся группа, предмет, аудитория и примечание.";
            SelectedLessonNote = string.Empty;
            return;
        }

        SelectedLessonTitle = $"{value.LessonDate}: {value.Topic}";
        SelectedLessonDetails = $"{value.GroupName} · {value.SubjectName} · {value.ClassroomName ?? "аудитория не указана"}";
        SelectedLessonNote = string.IsNullOrWhiteSpace(value.Note)
            ? "Примечаний нет."
            : value.Note;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var loadedLessons = currentUser.RoleName == "Преподаватель"
                ? lessonRepository.GetLessonsForTeacher(currentUser.UserId)
                : lessonRepository.GetLessons();
            var loadedSchedule = currentUser.RoleName == "Преподаватель"
                ? lessonRepository.GetScheduleForTeacher(currentUser.UserId)
                : lessonRepository.GetSchedule();

            Lessons = new ObservableCollection<LessonListItem>(loadedLessons);
            ScheduleCards = new ObservableCollection<LessonScheduleCard>(loadedSchedule.Select(ToScheduleCard));
            LessonCount = loadedLessons.Count;
            TodayLessonCount = loadedLessons.Count(IsToday);
            ScheduleCount = loadedSchedule.Count;
            SelectedLesson = loadedLessons.FirstOrDefault();
            ResultMessage = loadedLessons.Count == 0
                ? "Занятий пока нет. Добавьте тему через раздел тем и расписания."
                : $"Показано занятий: {loadedLessons.Count}.";
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
    private void OpenGrades() => NavigateRequested?.Invoke("Журнал занятия");

    [RelayCommand]
    private void OpenAttendance() => NavigateRequested?.Invoke("Посещаемость");

    [RelayCommand]
    private void OpenTopics() => NavigateRequested?.Invoke("Темы и расписание");

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
