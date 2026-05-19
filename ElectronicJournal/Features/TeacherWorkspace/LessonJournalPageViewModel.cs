using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
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
    private readonly ReportRepository reportRepository;
    private readonly AuthenticatedUser currentUser;

    [ObservableProperty]
    private ObservableCollection<LessonJournalLessonItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<StudentListItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LessonPrintRow> lessonRows = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> gradeTypes = new();

    [ObservableProperty]
    private LessonJournalLessonItem? selectedLesson;

    [ObservableProperty]
    private StudentListItem? selectedStudent;

    [ObservableProperty]
    private LookupItem? selectedGradeType;

    [ObservableProperty]
    private string selectedStatus = "Присутствовал";

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

    [ObservableProperty]
    private int lessonStudentCount;

    [ObservableProperty]
    private int markedAttendanceCount;

    [ObservableProperty]
    private int savedGradeCount;

    [ObservableProperty]
    private string lessonSnapshotSummary = "Выберите занятие, чтобы увидеть заполненность журнала.";

    public LessonJournalPageViewModel(
        LessonRepository lessonRepository,
        StudentRepository studentRepository,
        AttendanceRepository attendanceRepository,
        GradeRepository gradeRepository,
        GradeTypeRepository gradeTypeRepository,
        SettingsRepository settingsRepository,
        ReportRepository reportRepository,
        AuthenticatedUser currentUser)
        : base("Журнал занятия")
    {
        this.lessonRepository = lessonRepository;
        this.studentRepository = studentRepository;
        this.attendanceRepository = attendanceRepository;
        this.gradeRepository = gradeRepository;
        this.gradeTypeRepository = gradeTypeRepository;
        this.settingsRepository = settingsRepository;
        this.reportRepository = reportRepository;
        this.currentUser = currentUser;

        Load();
    }

    public ObservableCollection<string> AttendanceStatuses { get; } = new()
    {
        "Присутствовал",
        "Отсутствовал",
        "Опоздал",
        "Уважительная причина"
    };

    public event Action<string>? NavigateRequested;

    public override void OnNavigatedTo()
    {
        Load();
    }

    partial void OnSelectedLessonChanged(LessonJournalLessonItem? value)
    {
        if (value is null)
        {
            Students.Clear();
            LessonTitle = "Выберите занятие";
            LessonDetails = "После выбора появится список студентов группы.";
            LessonRows = new ObservableCollection<LessonPrintRow>();
            UpdateLessonCounters();
            return;
        }

        LessonTitle = $"{value.LessonDate}: {value.Topic}";
        LessonDetails = $"{value.GroupName} · {value.SubjectName} · {value.ClassroomName ?? "аудитория не указана"}";
        Students = new ObservableCollection<StudentListItem>(studentRepository.GetStudentsByGroup(value.GroupId));
        SelectedStudent = Students.FirstOrDefault();
        RefreshLessonSnapshot();
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
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            if (!CanUseSelectedLessonScope(out var scopeError))
            {
                ResultMessage = scopeError;
                NotifyWarning(ResultMessage);
                return;
            }

            attendanceRepository.UpsertAttendance(
                SelectedLesson.LessonId,
                SelectedStudent.StudentId,
                SelectedStatus,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());
            RefreshLessonSnapshot();
            ResultMessage = $"Посещаемость сохранена: {SelectedStudent.FullName}.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить посещаемость: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void SaveGrade()
    {
        if (SelectedLesson is null || SelectedStudent is null || SelectedGradeType is null)
        {
            ResultMessage = "Выберите занятие, студента и тип оценки.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!IsGradeInScale(GradeValue, out var gradeError))
        {
            ResultMessage = gradeError;
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            if (!CanUseSelectedLessonScope(out var scopeError))
            {
                ResultMessage = scopeError;
                NotifyWarning(ResultMessage);
                return;
            }

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
            RefreshLessonSnapshot();
            ResultMessage = $"Оценка сохранена: {SelectedStudent.FullName} - {GradeValue}.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void SaveBoth()
    {
        SaveAttendance();
        SaveGrade();
    }

    [RelayCommand]
    private void OpenGrades()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Выберите занятие.";
            NotifyWarning(ResultMessage);
            return;
        }

        NavigateRequested?.Invoke("Оценивание");
    }

    [RelayCommand]
    private void OpenAttendance()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Выберите занятие.";
            NotifyWarning(ResultMessage);
            return;
        }

        NavigateRequested?.Invoke("Посещаемость");
    }

    [RelayCommand]
    private void PrintJournal()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Выберите занятие для печати.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            if (!CanUseSelectedLessonOnly(out var scopeError))
            {
                ResultMessage = scopeError;
                NotifyWarning(ResultMessage);
                return;
            }

            var rows = reportRepository.GetLessonPrintRows(SelectedLesson.LessonId);
            var path = ReportExportService.CreatePrintableHtml(
                $"lesson_journal_{SelectedLesson.GroupName}_{SelectedLesson.LessonDate}",
                "Журнал занятия",
                new[]
                {
                    $"Дата: {SelectedLesson.LessonDate}",
                    $"Группа: {SelectedLesson.GroupName}",
                    $"Предмет: {SelectedLesson.SubjectName}",
                    $"Преподаватель: {SelectedLesson.TeacherName}",
                    $"Тема: {SelectedLesson.Topic}"
                },
                new[] { "Студент", "Посещаемость", "Оценки", "Комментарий" },
                rows.Select(row => new[] { row.StudentName, row.Status, row.Grades, row.Comment ?? string.Empty }));

            ReportExportService.OpenPrintDialog(path);
            ResultMessage = $"Печатная форма открыта: {path}";
            NotifySuccess("Печатная форма открыта.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать печатную форму: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void ExportJournalExcel()
    {
        if (SelectedLesson is null)
        {
            ResultMessage = "Выберите занятие для экспорта.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            if (!CanUseSelectedLessonOnly(out var scopeError))
            {
                ResultMessage = scopeError;
                NotifyWarning(ResultMessage);
                return;
            }

            var rows = reportRepository.GetLessonPrintRows(SelectedLesson.LessonId);
            var path = ReportExportService.CreateExcelXml(
                $"lesson_journal_{SelectedLesson.GroupName}_{SelectedLesson.LessonDate}",
                "Журнал занятия",
                new[] { "Студент", "Посещаемость", "Оценки", "Комментарий" },
                rows.Select(row => new[] { row.StudentName, row.Status, row.Grades, row.Comment ?? string.Empty }));

            ReportExportService.ShowInExplorer(path);
            ResultMessage = $"Excel-файл создан: {path}";
            NotifySuccess("Excel-файл создан и открыт в проводнике.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось экспортировать журнал: {ex.Message}";
            NotifyError(ResultMessage);
        }
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

    private bool CanUseSelectedLessonScope(out string error)
    {
        if (SelectedLesson is null || SelectedStudent is null)
        {
            error = "Выберите занятие и студента.";
            return false;
        }

        if (!Lessons.Any(lesson => lesson.LessonId == SelectedLesson.LessonId))
        {
            error = "Выбранное занятие недоступно текущему пользователю.";
            return false;
        }

        if (!Students.Any(student => student.StudentId == SelectedStudent.StudentId))
        {
            error = "Выбранный студент не входит в список выбранной группы.";
            return false;
        }

        if (currentUser.RoleName == "Преподаватель" &&
            !attendanceRepository.CanTeacherAccessLesson(SelectedLesson.LessonId, currentUser.UserId))
        {
            error = "Преподаватель может работать только со своими занятиями.";
            return false;
        }

        if (!attendanceRepository.CanStudentAttendLesson(SelectedLesson.LessonId, SelectedStudent.StudentId))
        {
            error = "Студент не относится к группе выбранного занятия.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool CanUseSelectedLessonOnly(out string error)
    {
        if (SelectedLesson is null)
        {
            error = "Выберите занятие.";
            return false;
        }

        if (!Lessons.Any(lesson => lesson.LessonId == SelectedLesson.LessonId))
        {
            error = "Выбранное занятие недоступно текущему пользователю.";
            return false;
        }

        if (currentUser.RoleName == "Преподаватель" &&
            !attendanceRepository.CanTeacherAccessLesson(SelectedLesson.LessonId, currentUser.UserId))
        {
            error = "Преподаватель может печатать только свои занятия.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void RefreshLessonSnapshot()
    {
        if (SelectedLesson is null)
        {
            LessonRows = new ObservableCollection<LessonPrintRow>();
            UpdateLessonCounters();
            return;
        }

        LessonRows = new ObservableCollection<LessonPrintRow>(
            reportRepository.GetLessonPrintRows(SelectedLesson.LessonId));
        UpdateLessonCounters();
    }

    private void UpdateLessonCounters()
    {
        LessonStudentCount = Students.Count;
        MarkedAttendanceCount = LessonRows.Count(row => row.Status != "Не отмечен");
        SavedGradeCount = LessonRows.Count(row => !string.IsNullOrWhiteSpace(row.Grades));
        LessonSnapshotSummary = SelectedLesson is null
            ? "Выберите занятие, чтобы увидеть заполненность журнала."
            : $"Отмечено посещений: {MarkedAttendanceCount} из {LessonStudentCount}. Оценки есть у студентов: {SavedGradeCount}.";
    }
}
