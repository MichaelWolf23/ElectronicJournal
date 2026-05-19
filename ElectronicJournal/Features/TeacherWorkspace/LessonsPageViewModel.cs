using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class LessonsPageViewModel : PageViewModelBase
{
    private readonly LessonRepository lessonRepository;
    private readonly AssignmentRepository assignmentRepository;
    private readonly AuthenticatedUser currentUser;
    private List<LessonListItem> allLessons = new();
    private List<ScheduleItem> allSchedule = new();

    [ObservableProperty]
    private ObservableCollection<LessonListItem> lessons = new();

    [ObservableProperty]
    private LessonListItem? selectedLesson;

    [ObservableProperty]
    private ObservableCollection<ScheduleItem> schedule = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> classrooms = new();

    [ObservableProperty]
    private int selectedAssignmentId;

    [ObservableProperty]
    private int? selectedClassroomId;

    [ObservableProperty]
    private string lessonDate = DateTime.Today.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private string topic = string.Empty;

    [ObservableProperty]
    private string note = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Заполните данные занятия.";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedScheduleDayFilter = ScheduleDayFilters[0];

    [ObservableProperty]
    private int lessonCount;

    [ObservableProperty]
    private int scheduleCount;

    [ObservableProperty]
    private int assignmentCount;

    [ObservableProperty]
    private string lessonsSummary = "Занятия загружаются.";

    [ObservableProperty]
    private string formTitle = "Новое занятие";

    [ObservableProperty]
    private string selectedLessonTitle = "Выберите занятие";

    [ObservableProperty]
    private string selectedLessonDetails = "После выбора строки здесь появятся дата, группа, предмет и аудитория.";

    [ObservableProperty]
    private string selectedLessonNote = "Примечание не выбрано.";

    public static IReadOnlyList<string> ScheduleDayFilters { get; } =
    [
        "Вся неделя",
        "Понедельник",
        "Вторник",
        "Среда",
        "Четверг",
        "Пятница",
        "Суббота",
        "Воскресенье"
    ];

    public LessonsPageViewModel(
        LessonRepository lessonRepository,
        AssignmentRepository assignmentRepository,
        AuthenticatedUser currentUser)
        : base("Занятия")
    {
        this.lessonRepository = lessonRepository;
        this.assignmentRepository = assignmentRepository;
        this.currentUser = currentUser;

        Load();
    }

    partial void OnSelectedLessonChanged(LessonListItem? value)
    {
        if (value is null)
        {
            SelectedLessonTitle = "Выберите занятие";
            SelectedLessonDetails = "После выбора строки здесь появятся дата, группа, предмет и аудитория.";
            SelectedLessonNote = "Примечание не выбрано.";
            return;
        }

        SelectedLessonTitle = value.Topic;
        SelectedLessonDetails =
            $"{value.LessonDate}, {value.GroupName}, {value.SubjectName}. " +
            $"Преподаватель: {value.TeacherName}. Аудитория: {value.ClassroomName ?? "не указана"}.";
        SelectedLessonNote = string.IsNullOrWhiteSpace(value.Note)
            ? "Примечание не указано."
            : value.Note;
        FormTitle = "Редактирование занятия";
        SelectedAssignmentId = value.AssignmentId;
        SelectedClassroomId = value.ClassroomId;
        LessonDate = value.LessonDate;
        Topic = value.Topic;
        Note = value.Note ?? string.Empty;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedScheduleDayFilterChanged(string value) => ApplyFilters();

    public override void OnNavigatedTo()
    {
        Load();
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            allLessons = LoadLessonsForCurrentUser();
            allSchedule = LoadScheduleForCurrentUser();
            Assignments = new ObservableCollection<LookupItem>(LoadAssignmentsForCurrentUser());
            Classrooms = new ObservableCollection<LookupItem>(lessonRepository.GetClassroomLookups());

            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedClassroomId = Classrooms.FirstOrDefault()?.Id;
            ApplyFilters();
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
    private void AddLesson()
    {
        if (!ValidateLessonForm(out var normalizedDate))
        {
            return;
        }

        try
        {
            if (lessonRepository.LessonExists(SelectedAssignmentId, normalizedDate, Topic.Trim()))
            {
                ResultMessage = "Такое занятие уже есть.";
                NotifyInfo(ResultMessage);
                return;
            }

            var lesson = new Lesson(
                0,
                SelectedAssignmentId,
                null,
                normalizedDate,
                Topic.Trim(),
                SelectedClassroomId,
                string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());

            lessonRepository.AddLesson(lesson);
            ResultMessage = "Занятие добавлено.";
            NotifySuccess(ResultMessage);
            ClearLessonForm();
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось добавить занятие: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void SaveSelectedLesson()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Выберите занятие, которое нужно изменить.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!ValidateLessonForm(out var normalizedDate))
        {
            return;
        }

        try
        {
            lessonRepository.UpdateLesson(new Lesson(
                SelectedLesson.LessonId,
                SelectedAssignmentId,
                null,
                normalizedDate,
                Topic.Trim(),
                SelectedClassroomId,
                string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()));
            ResultMessage = "Занятие обновлено.";
            NotifySuccess(ResultMessage);
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось обновить занятие: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void ClearLessonForm()
    {
        SelectedLesson = null;
        FormTitle = "Новое занятие";
        SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
        SelectedClassroomId = Classrooms.FirstOrDefault()?.Id;
        LessonDate = DateTime.Today.ToString("yyyy-MM-dd");
        Topic = string.Empty;
        Note = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteSelectedLesson()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Сначала выберите занятие.";
            NotifyWarning(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить занятие",
            $"Удалить занятие \"{SelectedLesson.Topic}\" от {SelectedLesson.LessonDate}? Оценки останутся, но будут отвязаны от занятия.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            lessonRepository.DeleteLesson(SelectedLesson.LessonId);
            ClearLessonForm();
            ResultMessage = "Занятие удалено.";
            NotifySuccess(ResultMessage);
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить занятие: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    private List<LessonListItem> LoadLessonsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? lessonRepository.GetLessonsForTeacher(currentUser.UserId)
            : lessonRepository.GetLessons();
    }

    private List<ScheduleItem> LoadScheduleForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? lessonRepository.GetScheduleForTeacher(currentUser.UserId)
            : lessonRepository.GetSchedule();
    }

    private List<LookupItem> LoadAssignmentsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? assignmentRepository.GetAssignmentLookupsForTeacher(currentUser.UserId)
            : assignmentRepository.GetAssignmentLookups();
    }

    private void UpdateSummary()
    {
        LessonCount = Lessons.Count;
        ScheduleCount = Schedule.Count;
        AssignmentCount = Assignments.Count;
        LessonsSummary = LessonCount == 0
            ? "Занятия пока не созданы."
            : $"Загружено занятий: {LessonCount}. Записей расписания: {ScheduleCount}. Доступных назначений: {AssignmentCount}.";
    }

    private void ApplyFilters()
    {
        IEnumerable<LessonListItem> filteredLessons = allLessons;
        IEnumerable<ScheduleItem> filteredSchedule = allSchedule;
        var query = SearchText.Trim();

        if (!string.IsNullOrWhiteSpace(query))
        {
            filteredLessons = filteredLessons.Where(lesson =>
                Contains(lesson.Topic, query) ||
                Contains(lesson.GroupName, query) ||
                Contains(lesson.SubjectName, query) ||
                Contains(lesson.TeacherName, query) ||
                Contains(lesson.ClassroomName, query));
            filteredSchedule = filteredSchedule.Where(item =>
                Contains(item.GroupName, query) ||
                Contains(item.SubjectName, query) ||
                Contains(item.TeacherName, query) ||
                Contains(item.ClassroomName, query) ||
                Contains(item.DayName, query));
        }

        if (SelectedScheduleDayFilter != "Вся неделя")
        {
            filteredSchedule = filteredSchedule.Where(item => item.DayName == SelectedScheduleDayFilter);
        }

        var selectedId = SelectedLesson?.LessonId;
        var visibleLessons = filteredLessons.ToList();
        Lessons = new ObservableCollection<LessonListItem>(visibleLessons);
        Schedule = new ObservableCollection<ScheduleItem>(filteredSchedule.ToList());
        SelectedLesson = visibleLessons.FirstOrDefault(lesson => lesson.LessonId == selectedId)
            ?? visibleLessons.FirstOrDefault();
        UpdateSummary();
    }

    private bool ValidateLessonForm(out string normalizedDate)
    {
        normalizedDate = string.IsNullOrWhiteSpace(LessonDate)
            ? DateTime.Today.ToString("yyyy-MM-dd")
            : LessonDate.Trim();

        if (SelectedAssignmentId == 0)
        {
            ResultMessage = "Выберите группу и предмет.";
            NotifyWarning(ResultMessage);
            return false;
        }

        if (string.IsNullOrWhiteSpace(Topic))
        {
            ResultMessage = "Введите тему занятия.";
            NotifyWarning(ResultMessage);
            return false;
        }

        if (!DateTime.TryParse(normalizedDate, out _))
        {
            ResultMessage = "Дата занятия должна быть в формате 2026-02-10.";
            NotifyWarning(ResultMessage);
            return false;
        }

        if (!Assignments.Any(assignment => assignment.Id == SelectedAssignmentId))
        {
            ResultMessage = "Выбранное назначение недоступно текущему пользователю.";
            NotifyWarning(ResultMessage);
            return false;
        }

        return true;
    }

    private static bool Contains(string? value, string query) =>
        value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;
}
