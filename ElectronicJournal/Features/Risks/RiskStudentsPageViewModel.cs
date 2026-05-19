using System;
using System.Collections.Generic;
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

public sealed partial class RiskStudentsPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly AttendanceRepository attendanceRepository;
    private readonly NotificationRepository notificationRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<StudentRiskItem> allRisks = new();

    [ObservableProperty]
    private ObservableCollection<StudentRiskItem> risks = new();

    [ObservableProperty]
    private ObservableCollection<string> groupFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> riskTypeFilters = new();

    [ObservableProperty]
    private StudentRiskItem? selectedRisk;

    [ObservableProperty]
    private string selectedGroupFilter = "Все группы";

    [ObservableProperty]
    private string selectedRiskTypeFilter = "Все риски";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int riskCount;

    [ObservableProperty]
    private int uniqueStudentCount;

    [ObservableProperty]
    private int affectedGroupCount;

    [ObservableProperty]
    private int gradeRiskCount;

    [ObservableProperty]
    private int attendanceRiskCount;

    [ObservableProperty]
    private string riskTitle = "Выберите студента риска";

    [ObservableProperty]
    private string riskDetails = "Здесь будет причина риска, предмет и преподаватель.";

    [ObservableProperty]
    private string notificationPreview = "Выберите запись, чтобы подготовить сообщение куратору.";

    [ObservableProperty]
    private string selectedRiskActionTitle = "Что произошло";

    [ObservableProperty]
    private string resultMessage = "Готово к проверке студентов риска.";

    public RiskStudentsPageViewModel(
        GradeRepository gradeRepository,
        AttendanceRepository attendanceRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Студенты риска")
    {
        this.gradeRepository = gradeRepository;
        this.attendanceRepository = attendanceRepository;
        this.notificationRepository = notificationRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;
        Load();
    }

    public bool CanCreateNotification => currentUser.RoleName is "Преподаватель" or "Администратор";

    public event Action<int>? StudentProfileRequested;

    public override void OnNavigatedTo()
    {
        Load();
    }

    partial void OnSelectedRiskChanged(StudentRiskItem? value) => UpdateSelectedRisk();

    partial void OnSelectedGroupFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedRiskTypeFilterChanged(string value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            var minPositiveGrade = settingsRepository.GetMinPositiveGrade();
            var gradeRisks = currentUser.RoleName switch
            {
                "Преподаватель" => gradeRepository.GetDebtorsForTeacher(minPositiveGrade, currentUser.UserId),
                "Куратор группы" => gradeRepository.GetDebtorsForCurator(minPositiveGrade, currentUser.UserId),
                _ => gradeRepository.GetDebtors(minPositiveGrade)
            };
            var attendanceRisks = currentUser.RoleName switch
            {
                "Преподаватель" => attendanceRepository.GetAbsenceRisksForTeacher(currentUser.UserId),
                "Куратор группы" => attendanceRepository.GetAbsenceRisksForCurator(currentUser.UserId),
                _ => attendanceRepository.GetAbsenceRisks()
            };

            allRisks = gradeRisks.Select(ToGradeRisk).Concat(attendanceRisks).ToList();

            GroupFilters = new ObservableCollection<string>(
                new[] { "Все группы" }.Concat(allRisks.Select(risk => risk.GroupName).Distinct().OrderBy(name => name)));
            RiskTypeFilters = new ObservableCollection<string>(
                new[] { "Все риски" }.Concat(allRisks.Select(risk => risk.RiskType).Distinct().OrderBy(name => name)));
            SelectedGroupFilter = "Все группы";
            SelectedRiskTypeFilter = "Все риски";
            ApplyFilters();
            ResultMessage = allRisks.Count == 0
                ? "Студентов риска не найдено."
                : $"Найдено проблемных записей: {allRisks.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить студентов риска: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        SelectedGroupFilter = "Все группы";
        SelectedRiskTypeFilter = "Все риски";
        ApplyFilters();
    }

    [RelayCommand]
    private void CreateNotification()
    {
        if (SelectedRisk is null)
        {
            ResultMessage = "Выберите студента риска.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!settingsRepository.AreCuratorNotificationsEnabled())
        {
            ResultMessage = "Уведомления кураторам отключены в настройках журнала.";
            NotifyInfo(ResultMessage);
            return;
        }

        var curator = notificationRepository.GetCuratorForGroup(SelectedRisk.GroupId);
        if (curator is null)
        {
            ResultMessage = "В системе нет активного куратора.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            notificationRepository.CreateNotification(new CuratorNotification(
                0,
                curator.UserId,
                SelectedRisk.StudentId,
                SelectedRisk.GroupId,
                SelectedRisk.AssignmentId,
                $"{SelectedRisk.RiskType}: {SelectedRisk.StudentName}",
                NotificationPreview,
                "Новое",
                string.Empty,
                null));
            ResultMessage = curator.IsAssignedToGroup
                ? $"Уведомление создано для куратора: {curator.FullName}."
                : $"У группы нет назначенного куратора. Уведомление отправлено активному куратору: {curator.FullName}.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать уведомление: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void OpenStudentProfile()
    {
        if (SelectedRisk is null)
        {
            ResultMessage = "Выберите студента риска.";
            NotifyWarning(ResultMessage);
            return;
        }

        StudentProfileRequested?.Invoke(SelectedRisk.StudentId);
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (Risks.Count == 0)
        {
            ResultMessage = "Нет данных для отчета.";
            NotifyInfo(ResultMessage);
            return;
        }

        try
        {
            var reportPath = CsvExportService.CreateReport(
                "students_risk",
                new[] { "Студент", "Группа", "Тип риска", "Предмет", "Преподаватель", "Значение", "Дата", "Комментарий" },
                Risks.Select(risk => new[]
                {
                    risk.StudentName,
                    risk.GroupName,
                    risk.RiskType,
                    risk.SubjectName,
                    risk.TeacherName,
                    risk.ValueText,
                    risk.DateText,
                    risk.Comment ?? string.Empty
                }));

            ReportExportService.ShowInExplorer(reportPath);
            ResultMessage = $"Отчет сохранен: {reportPath}";
            NotifySuccess("Отчет сохранен и открыт в проводнике.");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить отчет: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<StudentRiskItem> filtered = allRisks;

        if (SelectedGroupFilter != "Все группы")
        {
            filtered = filtered.Where(risk => risk.GroupName == SelectedGroupFilter);
        }

        if (SelectedRiskTypeFilter != "Все риски")
        {
            filtered = filtered.Where(risk => risk.RiskType == SelectedRiskTypeFilter);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(risk =>
                risk.StudentName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || risk.RiskType.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || risk.SubjectName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || risk.GroupName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        var visible = filtered.ToList();
        Risks = new ObservableCollection<StudentRiskItem>(visible);
        RiskCount = visible.Count;
        UniqueStudentCount = visible.Select(risk => risk.StudentId).Distinct().Count();
        AffectedGroupCount = visible.Select(risk => risk.GroupId).Distinct().Count();
        GradeRiskCount = visible.Count(risk => risk.RiskType == "Низкая оценка");
        AttendanceRiskCount = visible.Count(risk => risk.RiskType != "Низкая оценка");
        SelectedRisk = visible.FirstOrDefault();
    }

    private void UpdateSelectedRisk()
    {
        if (SelectedRisk is null)
        {
            RiskTitle = "Выберите студента риска";
            RiskDetails = "Здесь будет причина риска, предмет и преподаватель.";
            NotificationPreview = "Выберите запись, чтобы подготовить сообщение куратору.";
            SelectedRiskActionTitle = currentUser.RoleName == "Куратор группы"
                ? "Что произошло"
                : "Сообщение куратору";
            return;
        }

        RiskTitle = $"{SelectedRisk.StudentName} · {SelectedRisk.GroupName}";
        RiskDetails = $"{SelectedRisk.RiskType}: {SelectedRisk.ValueText} от {SelectedRisk.DateText}. Предмет: {SelectedRisk.SubjectName}.";
        SelectedRiskActionTitle = currentUser.RoleName == "Куратор группы"
            ? "Что произошло"
            : "Сообщение куратору";
        NotificationPreview =
            $"Студент {SelectedRisk.StudentName} из группы {SelectedRisk.GroupName}: {SelectedRisk.RiskType.ToLower()} ({SelectedRisk.ValueText}) по предмету {SelectedRisk.SubjectName}. " +
            "Нужно обратить внимание и при необходимости связаться со студентом.";
    }

    private static StudentRiskItem ToGradeRisk(DebtorItem debtor)
    {
        return new StudentRiskItem(
            debtor.StudentId,
            debtor.GroupId,
            debtor.AssignmentId,
            debtor.StudentName,
            debtor.GroupName,
            "Низкая оценка",
            debtor.SubjectName,
            debtor.TeacherName,
            debtor.GradeValue.ToString("F1"),
            debtor.GradeDate,
            debtor.Comment);
    }
}
