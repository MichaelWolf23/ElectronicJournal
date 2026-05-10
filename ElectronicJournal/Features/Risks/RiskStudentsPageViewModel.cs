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
    private StudentRiskItem? selectedRisk;

    [ObservableProperty]
    private string selectedGroupFilter = "Все группы";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int riskCount;

    [ObservableProperty]
    private int uniqueStudentCount;

    [ObservableProperty]
    private int affectedGroupCount;

    [ObservableProperty]
    private string riskTitle = "Выберите студента риска";

    [ObservableProperty]
    private string riskDetails = "Здесь будет причина риска, предмет и преподаватель.";

    [ObservableProperty]
    private string notificationPreview = "Выберите запись, чтобы подготовить сообщение куратору.";

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

    partial void OnSelectedRiskChanged(StudentRiskItem? value) => UpdateSelectedRisk();

    partial void OnSelectedGroupFilterChanged(string value) => ApplyFilters();

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
            SelectedGroupFilter = "Все группы";
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
        ApplyFilters();
    }

    [RelayCommand]
    private void CreateNotification()
    {
        if (SelectedRisk is null)
        {
            ResultMessage = "Выберите студента риска.";
            return;
        }

        var curatorUserId = notificationRepository.GetCuratorUserIdForGroup(SelectedRisk.GroupId);
        if (curatorUserId is null)
        {
            ResultMessage = "Для группы не найден куратор.";
            return;
        }

        try
        {
            notificationRepository.CreateNotification(new CuratorNotification(
                0,
                curatorUserId.Value,
                SelectedRisk.StudentId,
                SelectedRisk.GroupId,
                SelectedRisk.AssignmentId,
                $"{SelectedRisk.RiskType}: {SelectedRisk.StudentName}",
                NotificationPreview,
                "Новое",
                string.Empty,
                null));
            ResultMessage = "Уведомление куратору создано.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать уведомление: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void OpenStudentProfile()
    {
        if (SelectedRisk is null)
        {
            ResultMessage = "Выберите студента риска.";
            return;
        }

        StudentProfileRequested?.Invoke(SelectedRisk.StudentId);
    }

    private void ApplyFilters()
    {
        IEnumerable<StudentRiskItem> filtered = allRisks;

        if (SelectedGroupFilter != "Все группы")
        {
            filtered = filtered.Where(risk => risk.GroupName == SelectedGroupFilter);
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
        SelectedRisk = visible.FirstOrDefault();
    }

    private void UpdateSelectedRisk()
    {
        if (SelectedRisk is null)
        {
            RiskTitle = "Выберите студента риска";
            RiskDetails = "Здесь будет причина риска, предмет и преподаватель.";
            NotificationPreview = "Выберите запись, чтобы подготовить сообщение куратору.";
            return;
        }

        RiskTitle = $"{SelectedRisk.StudentName} · {SelectedRisk.GroupName}";
        RiskDetails = $"{SelectedRisk.RiskType}: {SelectedRisk.ValueText} от {SelectedRisk.DateText}. Предмет: {SelectedRisk.SubjectName}.";
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
