using System;
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

    [ObservableProperty]
    private ObservableCollection<LessonListItem> lessons = new();

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

    public LessonsPageViewModel(LessonRepository lessonRepository, AssignmentRepository assignmentRepository)
        : base("Занятия")
    {
        this.lessonRepository = lessonRepository;
        this.assignmentRepository = assignmentRepository;

        Load();
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Lessons = new ObservableCollection<LessonListItem>(lessonRepository.GetLessons());
            Schedule = new ObservableCollection<ScheduleItem>(lessonRepository.GetSchedule());
            Assignments = new ObservableCollection<LookupItem>(assignmentRepository.GetAssignmentLookups());
            Classrooms = new ObservableCollection<LookupItem>(lessonRepository.GetClassroomLookups());

            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedClassroomId = Classrooms.FirstOrDefault()?.Id;
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
}
