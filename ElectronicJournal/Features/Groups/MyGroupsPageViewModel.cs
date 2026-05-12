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

public sealed partial class MyGroupsPageViewModel : PageViewModelBase
{
    private readonly GroupRepository groupRepository;
    private readonly StudentRepository studentRepository;
    private readonly GradeRepository gradeRepository;
    private readonly NotificationRepository notificationRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<DebtorItem> allDebts = new();

    [ObservableProperty]
    private ObservableCollection<GroupStatisticsItem> groups = new();

    [ObservableProperty]
    private ObservableCollection<StudentListItem> students = new();

    [ObservableProperty]
    private ObservableCollection<DebtorItem> groupDebts = new();

    [ObservableProperty]
    private ObservableCollection<CuratorNotificationItem> notifications = new();

    [ObservableProperty]
    private ObservableCollection<GroupChartItem> chartItems = new();

    [ObservableProperty]
    private GroupStatisticsItem? selectedGroup;

    [ObservableProperty]
    private string groupTitle = "Выберите группу";

    [ObservableProperty]
    private string groupDetails = "Здесь появятся студенты, риски и уведомления.";

    [ObservableProperty]
    private int totalStudents;

    [ObservableProperty]
    private int totalDebtors;

    [ObservableProperty]
    private string averageText = "Нет данных";

    [ObservableProperty]
    private string resultMessage = "Группы загружены.";

    public MyGroupsPageViewModel(
        GroupRepository groupRepository,
        StudentRepository studentRepository,
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Мои группы")
    {
        this.groupRepository = groupRepository;
        this.studentRepository = studentRepository;
        this.gradeRepository = gradeRepository;
        this.notificationRepository = notificationRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;
        Load();
    }

    partial void OnSelectedGroupChanged(GroupStatisticsItem? value) => UpdateGroupDetails();

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            var minPositiveGrade = settingsRepository.GetMinPositiveGrade();
            var loadedGroups = currentUser.RoleName == "Куратор группы"
                ? groupRepository.GetGroupStatisticsForCurator(minPositiveGrade, currentUser.UserId)
                : groupRepository.GetGroupStatistics(minPositiveGrade);
            allDebts = currentUser.RoleName == "Куратор группы"
                ? gradeRepository.GetDebtorsForCurator(minPositiveGrade, currentUser.UserId)
                : gradeRepository.GetDebtors(minPositiveGrade);
            Notifications = new ObservableCollection<CuratorNotificationItem>(
                currentUser.RoleName == "Куратор группы"
                    ? notificationRepository.GetNotificationsByCurator(currentUser.UserId)
                    : notificationRepository.GetNotifications());
            Groups = new ObservableCollection<GroupStatisticsItem>(loadedGroups);
            ChartItems = new ObservableCollection<GroupChartItem>(BuildChartItems(loadedGroups));
            TotalStudents = loadedGroups.Sum(group => group.StudentCount);
            TotalDebtors = loadedGroups.Sum(group => group.DebtorCount);
            AverageText = loadedGroups.Where(group => group.AverageGrade is not null).Select(group => group.AverageGrade!.Value).DefaultIfEmpty().Average().ToString("F2");
            SelectedGroup = loadedGroups.FirstOrDefault();
            ResultMessage = $"Групп в работе: {loadedGroups.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить группы: {ex.Message}";
        }
    }

    private static List<GroupChartItem> BuildChartItems(List<GroupStatisticsItem> groups)
    {
        var maxDebtors = Math.Max(1, groups.Select(group => group.DebtorCount).DefaultIfEmpty().Max());
        return groups.Select(group =>
        {
            var average = group.AverageGrade ?? 0;
            return new GroupChartItem(
                group.GroupName,
                group.AverageGrade?.ToString("F2") ?? "-",
                Math.Clamp(average / 5 * 160, 4, 160),
                group.DebtorCount,
                Math.Clamp((double)group.DebtorCount / maxDebtors * 160, group.DebtorCount == 0 ? 0 : 4, 160));
        }).ToList();
    }

    private void UpdateGroupDetails()
    {
        if (SelectedGroup is null)
        {
            GroupTitle = "Выберите группу";
            GroupDetails = "Здесь появятся студенты, риски и уведомления.";
            Students.Clear();
            GroupDebts.Clear();
            return;
        }

        GroupTitle = SelectedGroup.GroupName;
        GroupDetails = $"Студентов: {SelectedGroup.StudentCount}. Средний балл: {SelectedGroup.AverageGrade?.ToString("F2") ?? "нет данных"}. Должников: {SelectedGroup.DebtorCount}.";
        Students = new ObservableCollection<StudentListItem>(studentRepository.GetStudentsByGroup(SelectedGroup.GroupId));
        GroupDebts = new ObservableCollection<DebtorItem>(allDebts.Where(debt => debt.GroupId == SelectedGroup.GroupId));
    }

    [RelayCommand]
    private void ExportGroupReport()
    {
        if (SelectedGroup is null)
        {
            ResultMessage = "Выберите группу для отчета.";
            return;
        }

        try
        {
            var rows = new List<IReadOnlyList<string?>>();
            rows.Add(new[]
            {
                "Сводка",
                SelectedGroup.GroupName,
                $"Студентов: {SelectedGroup.StudentCount}",
                $"Средний балл: {SelectedGroup.AverageGrade?.ToString("F2") ?? "нет данных"}",
                $"Должников: {SelectedGroup.DebtorCount}",
                string.Empty
            });

            rows.AddRange(Students.Select(student => new[]
            {
                "Студент",
                SelectedGroup.GroupName,
                student.FullName,
                student.Status,
                student.StudentCardNumber,
                string.Empty
            }));

            rows.AddRange(GroupDebts.Select(debt => new[]
            {
                "Риск",
                debt.GroupName,
                debt.StudentName,
                debt.SubjectName,
                debt.GradeValue.ToString("F1"),
                debt.Comment ?? string.Empty
            }));

            var reportPath = CsvExportService.CreateReport(
                $"group_{SelectedGroup.GroupName}",
                new[] { "Раздел", "Группа", "Объект", "Описание", "Значение", "Комментарий" },
                rows);

            ReportExportService.ShowInExplorer(reportPath);
            ResultMessage = $"Сводный отчет сохранен: {reportPath}";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить отчет: {ex.Message}";
        }
    }
}
