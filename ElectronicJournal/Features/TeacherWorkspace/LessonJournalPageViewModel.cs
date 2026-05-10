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

public sealed partial class LessonJournalPageViewModel : PageViewModelBase
{
    private readonly LessonRepository lessonRepository;
    private readonly StudentRepository studentRepository;
    private readonly AttendanceRepository attendanceRepository;
    private readonly GradeRepository gradeRepository;
    private readonly GradeTypeRepository gradeTypeRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;

    [ObservableProperty]
    private ObservableCollection<LessonJournalLessonItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<StudentListItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> gradeTypes = new();

    [ObservableProperty]
    private LessonJournalLessonItem? selectedLesson;

    [ObservableProperty]
    private StudentListItem? selectedStudent;

    [ObservableProperty]
    private LookupItem? selectedGradeType;

    [ObservableProperty]
    private string selectedStatus = "присутствовал";

    [ObservableProperty]
    private double gradeValue = 5;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string lessonTitle = "Выберите занятие";

    [ObservableProperty]
    private string lessonDetails = "После выбора появится список студентов группы.";

    [ObservableProperty]
    private string studentDetails = "Выберите студента для отметки.";

    [ObservableProperty]
    private string resultMessage = "Готово к работе.";

    public LessonJournalPageViewModel(
        LessonRepository lessonRepository,
        StudentRepository studentRepository,
        AttendanceRepository attendanceRepository,
        GradeRepository gradeRepository,
        GradeTypeRepository gradeTypeRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Журнал занятия")
    {
        this.lessonRepository = lessonRepository;
        this.studentRepository = studentRepository;
        this.attendanceRepository = attendanceRepository;
        this.gradeRepository = gradeRepository;
        this.gradeTypeRepository = gradeTypeRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;

        Load();
    }

    public ObservableCollection<string> AttendanceStatuses { get; } = new()
    {
        "присутствовал",
        "отсутствовал",
        "опоздал",
        "уважительная причина"
    };

    partial void OnSelectedLessonChanged(LessonJournalLessonItem? value)
    {
        if (value is null)
        {
            Students.Clear();
            LessonTitle = "Выберите занятие";
            LessonDetails = "После выбора появится список студентов группы.";
            return;
        }

        LessonTitle = $"{value.LessonDate}: {value.Topic}";
        LessonDetails = $"{value.GroupName} · {value.SubjectName} · {value.ClassroomName ?? "аудитория не указана"}";
        Students = new ObservableCollection<StudentListItem>(studentRepository.GetStudentsByGroup(value.GroupId));
        SelectedStudent = Students.FirstOrDefault();
    }

    partial void OnSelectedStudentChanged(StudentListItem? value)
    {
        StudentDetails = value is null
            ? "Выберите студента для отметки."
            : $"{value.FullName} · {value.GroupName} · {value.Status}";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            var loadedLessons = currentUser.RoleName == "Преподаватель"
                ? lessonRepository.GetJournalLessonsForTeacher(currentUser.UserId)
                : lessonRepository.GetJournalLessons();
            Lessons = new ObservableCollection<LessonJournalLessonItem>(loadedLessons);
            GradeTypes = new ObservableCollection<LookupItem>(gradeTypeRepository.GetGradeTypeLookups());
            SelectedGradeType = GradeTypes.FirstOrDefault();
            SelectedLesson = loadedLessons.FirstOrDefault();
            ResultMessage = loadedLessons.Count == 0
                ? "Занятий пока нет."
                : $"Доступно занятий: {loadedLessons.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить журнал занятия: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SaveAttendance()
    {
        if (SelectedLesson is null || SelectedStudent is null)
        {
            ResultMessage = "Выберите занятие и студента.";
            return;
        }

        try
        {
            attendanceRepository.UpsertAttendance(
                SelectedLesson.LessonId,
                SelectedStudent.StudentId,
                SelectedStatus,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());
            ResultMessage = $"Посещаемость сохранена: {SelectedStudent.FullName}.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить посещаемость: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void SaveGrade()
    {
        if (SelectedLesson is null || SelectedStudent is null || SelectedGradeType is null)
        {
            ResultMessage = "Выберите занятие, студента и тип оценки.";
            return;
        }

        if (!IsGradeInScale(GradeValue, out var gradeError))
        {
            ResultMessage = gradeError;
            return;
        }

        try
        {
            gradeRepository.AddGrade(new Grade(
                0,
                SelectedStudent.StudentId,
                SelectedLesson.AssignmentId,
                SelectedLesson.LessonId,
                SelectedGradeType.Id,
                GradeValue,
                DateTime.Today.ToString("yyyy-MM-dd"),
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim(),
                currentUser.UserId,
                string.Empty,
                null));
            ResultMessage = $"Оценка сохранена: {SelectedStudent.FullName} - {GradeValue}.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void SaveBoth()
    {
        SaveAttendance();
        SaveGrade();
    }

    private bool IsGradeInScale(double value, out string error)
    {
        var minGrade = settingsRepository.GetMinGradeScale();
        var maxGrade = settingsRepository.GetMaxGradeScale();
        if (value < minGrade || value > maxGrade)
        {
            error = $"Оценка должна быть от {minGrade} до {maxGrade}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
