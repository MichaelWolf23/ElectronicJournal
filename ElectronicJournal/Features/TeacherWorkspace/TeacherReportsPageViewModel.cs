using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;

namespace ElectronicJournal.ViewModels;

public sealed partial class TeacherReportsPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly StudentRepository studentRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<GradeJournalItem> allGrades = new();
    private List<StudentListItem> allStudents = new();

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> grades = new();

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> attentionGrades = new();

    [ObservableProperty]
    private ObservableCollection<string> groupFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> subjectFilters = new();

    [ObservableProperty]
    private string selectedGroupFilter = "Все группы";

    [ObservableProperty]
    private string selectedSubjectFilter = "Все предметы";

    [ObservableProperty]
    private int gradeCount;

    [ObservableProperty]
    private int studentCount;

    [ObservableProperty]
    private int debtorCount;

    [ObservableProperty]
    private int lowGradeCount;

    [ObservableProperty]
    private string averageText = "Нет данных";

    [ObservableProperty]
    private string reportScopeText = "Все группы · все предметы";

    [ObservableProperty]
    private string attentionSummary = "Проблемных оценок нет.";

    [ObservableProperty]
    private string resultMessage = "Сформируйте отчет по успеваемости.";

    public TeacherReportsPageViewModel(
        GradeRepository gradeRepository,
        StudentRepository studentRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Отчеты")
    {
        this.gradeRepository = gradeRepository;
        this.studentRepository = studentRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;
        Load();
    }

    partial void OnSelectedGroupFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            allGrades = currentUser.RoleName == "Преподаватель"
                ? gradeRepository.GetJournalForTeacher(currentUser.UserId)
                : gradeRepository.GetJournal();
            allStudents = currentUser.RoleName == "Преподаватель"
                ? studentRepository.GetStudentsForTeacher(currentUser.UserId)
                : studentRepository.GetStudents();

            GroupFilters = new ObservableCollection<string>(
                new[] { "Все группы" }.Concat(allGrades.Select(item => item.GroupName).Distinct().OrderBy(name => name)));
            SubjectFilters = new ObservableCollection<string>(
                new[] { "Все предметы" }.Concat(allGrades.Select(item => item.SubjectName).Distinct().OrderBy(name => name)));
            SelectedGroupFilter = "Все группы";
            SelectedSubjectFilter = "Все предметы";
            ApplyFilters();
            ResultMessage = $"Загружено оценок: {allGrades.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить отчет: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SelectedGroupFilter = "Все группы";
        SelectedSubjectFilter = "Все предметы";
        ApplyFilters();
    }

    [RelayCommand]
    private void PrintReport()
    {
        if (Grades.Count == 0)
        {
            ResultMessage = "Нет данных для печати.";
            NotifyInfo(ResultMessage);
            return;
        }

        try
        {
            var path = ReportExportService.CreatePrintableHtml(
                "teacher_performance_report",
                "Отчет по успеваемости",
                new[]
                {
                    $"Пользователь: {currentUser.FullName}",
                    $"Группа: {SelectedGroupFilter}",
                    $"Предмет: {SelectedSubjectFilter}",
                    $"Оценок: {GradeCount}. Студентов: {StudentCount}. Средний балл: {AverageText}. Должников: {DebtorCount}."
                },
                new[] { "Студент", "Группа", "Предмет", "Тип", "Вес", "Оценка", "Дата", "Комментарий" },
                Grades.Select(ToReportRow));

            ReportExportService.OpenPrintDialog(path);
            ResultMessage = $"Печатный отчет открыт: {path}";
            NotifySuccess("Печатный отчет открыт.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать отчет: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void ExportExcel()
    {
        if (Grades.Count == 0)
        {
            ResultMessage = "Нет данных для экспорта.";
            NotifyInfo(ResultMessage);
            return;
        }

        try
        {
            var path = ReportExportService.CreateExcelXml(
                "teacher_performance_report",
                "Отчет по успеваемости",
                new[] { "Студент", "Группа", "Предмет", "Тип", "Вес", "Оценка", "Дата", "Комментарий" },
                Grades.Select(ToReportRow));

            ReportExportService.ShowInExplorer(path);
            ResultMessage = $"Excel-отчет создан: {path}";
            NotifySuccess("Excel-отчет создан и открыт в проводнике.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось экспортировать отчет: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<GradeJournalItem> filtered = allGrades;
        if (SelectedGroupFilter != "Все группы")
        {
            filtered = filtered.Where(item => item.GroupName == SelectedGroupFilter);
        }

        if (SelectedSubjectFilter != "Все предметы")
        {
            filtered = filtered.Where(item => item.SubjectName == SelectedSubjectFilter);
        }

        var visible = filtered.ToList();
        var minPositiveGrade = settingsRepository.GetMinPositiveGrade();
        var attention = visible
            .Where(item => item.GradeValue < minPositiveGrade)
            .OrderBy(item => item.GroupName)
            .ThenBy(item => item.StudentName)
            .ThenBy(item => item.SubjectName)
            .ThenBy(item => item.GradeDate)
            .ToList();

        Grades = new ObservableCollection<GradeJournalItem>(visible);
        AttentionGrades = new ObservableCollection<GradeJournalItem>(attention);
        GradeCount = visible.Count;
        StudentCount = CountStudentsInReportScope();
        LowGradeCount = attention.Count;
        DebtorCount = attention.Select(item => item.StudentName).Distinct().Count();
        AverageText = visible.Count == 0
            ? "Нет данных"
            : visible.Average(item => item.GradeValue).ToString("F2");
        ReportScopeText = $"{SelectedGroupFilter} · {SelectedSubjectFilter}";
        AttentionSummary = attention.Count == 0
            ? "По текущим фильтрам нет оценок ниже положительной."
            : $"Низких оценок: {LowGradeCount}. Студентов с риском: {DebtorCount}.";
    }

    private int CountStudentsInReportScope()
    {
        IEnumerable<StudentListItem> scopedStudents = allStudents;

        if (SelectedGroupFilter != "Все группы")
        {
            scopedStudents = scopedStudents.Where(student => student.GroupName == SelectedGroupFilter);
        }

        if (SelectedSubjectFilter != "Все предметы")
        {
            var groupsWithSelectedSubject = allGrades
                .Where(grade => grade.SubjectName == SelectedSubjectFilter)
                .Select(grade => grade.GroupName)
                .Distinct()
                .ToHashSet();
            scopedStudents = scopedStudents.Where(student => groupsWithSelectedSubject.Contains(student.GroupName));
        }

        return scopedStudents.Select(student => student.StudentId).Distinct().Count();
    }

    private static IReadOnlyList<string?> ToReportRow(GradeJournalItem item)
    {
        return new[]
        {
            item.StudentName,
            item.GroupName,
            item.SubjectName,
            item.GradeType,
            item.GradeWeight.ToString("F1"),
            item.GradeValue.ToString("F1"),
            item.GradeDate,
            item.Comment ?? string.Empty
        };
    }
}
