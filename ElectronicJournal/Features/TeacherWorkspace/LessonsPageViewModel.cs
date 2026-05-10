using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class LessonsPageViewModel : PageViewModelBase
{
    private readonly LessonRepository lessonRepository;
    private readonly AssignmentRepository assignmentRepository;
    private readonly AuthenticatedUser currentUser;

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
    private int lessonCount;

    [ObservableProperty]
    private int scheduleCount;

    [ObservableProperty]
    private int assignmentCount;

    [ObservableProperty]
    private string lessonsSummary = "Занятия загружаются.";

    [ObservableProperty]
    private string selectedLessonTitle = "Выберите занятие";

    [ObservableProperty]
    private string selectedLessonDetails = "После выбора строки здесь появятся дата, группа, предмет и аудитория.";

    [ObservableProperty]
    private string selectedLessonNote = "Примечание не выбрано.";

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
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Lessons = new ObservableCollection<LessonListItem>(LoadLessonsForCurrentUser());
            Schedule = new ObservableCollection<ScheduleItem>(LoadScheduleForCurrentUser());
            Assignments = new ObservableCollection<LookupItem>(LoadAssignmentsForCurrentUser());
            Classrooms = new ObservableCollection<LookupItem>(lessonRepository.GetClassroomLookups());

            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedClassroomId = Classrooms.FirstOrDefault()?.Id;
            UpdateSummary();
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
        if (SelectedAssignmentId == 0)
        {
            ResultMessage = "Выберите назначение преподавателя.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Topic))
        {
            ResultMessage = "Введите тему занятия.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(LessonDate) && !DateTime.TryParse(LessonDate, out _))
        {
            ResultMessage = "Дата занятия должна быть в понятном формате, например 2026-02-10.";
            return;
        }

        try
        {
            var lesson = new Lesson(
                0,
                SelectedAssignmentId,
                null,
                string.IsNullOrWhiteSpace(LessonDate) ? DateTime.Today.ToString("yyyy-MM-dd") : LessonDate.Trim(),
                Topic.Trim(),
                SelectedClassroomId,
                string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());

            lessonRepository.AddLesson(lesson);
            ResultMessage = "Занятие добавлено.";
            Topic = string.Empty;
            Note = string.Empty;
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось добавить занятие: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
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
}
