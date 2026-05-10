using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class StatisticsPageViewModel : PageViewModelBase
{
    private readonly GroupRepository groupRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;

    [ObservableProperty]
    private ObservableCollection<GroupStatisticsItem> groupStatistics = new();

    [ObservableProperty]
    private GroupStatisticsItem? selectedGroupStatistics;

    [ObservableProperty]
    private int totalStudents;

    [ObservableProperty]
    private int totalDebtors;

    [ObservableProperty]
    private double? averageGrade;

    [ObservableProperty]
    private double minPositiveGrade = 3;

    [ObservableProperty]
    private string resultMessage = "Статистика по группам.";

    [ObservableProperty]
    private int groupCount;

    [ObservableProperty]
    private int riskGroupCount;

    [ObservableProperty]
    private string bestGroupText = "Нет данных";

    [ObservableProperty]
    private string selectedGroupTitle = "Выберите группу";

    [ObservableProperty]
    private string selectedGroupDetails = "После выбора строки здесь появится краткий анализ группы.";

    [ObservableProperty]
    private string selectedGroupRisk = "Риск не рассчитан.";

    public StatisticsPageViewModel(
        GroupRepository groupRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Статистика")
    {
        this.groupRepository = groupRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;

        Load();
    }

    partial void OnSelectedGroupStatisticsChanged(GroupStatisticsItem? value)
    {
        if (value is null)
        {
            SelectedGroupTitle = "Выберите группу";
            SelectedGroupDetails = "После выбора строки здесь появится краткий анализ группы.";
            SelectedGroupRisk = "Риск не рассчитан.";
            return;
        }

        SelectedGroupTitle = value.GroupName;
        SelectedGroupDetails =
            $"Студентов: {value.StudentCount}. Средний балл: {(value.AverageGrade is null ? "нет данных" : value.AverageGrade.Value.ToString("F2"))}. " +
            $"Должников: {value.DebtorCount}.";
        SelectedGroupRisk = value.DebtorCount == 0
            ? "Группа выглядит стабильно: должников нет."
            : value.DebtorCount <= 2
                ? "Есть отдельные проблемы, стоит проверить конкретных студентов."
                : "Группа требует внимания: много студентов с задолженностями.";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            MinPositiveGrade = settingsRepository.GetMinPositiveGrade();

            var statistics = currentUser.RoleName == "Куратор группы"
                ? groupRepository.GetGroupStatisticsForCurator(MinPositiveGrade, currentUser.UserId)
                : groupRepository.GetGroupStatistics(MinPositiveGrade);
            GroupStatistics = new ObservableCollection<GroupStatisticsItem>(statistics);
            TotalStudents = statistics.Sum(item => item.StudentCount);
            TotalDebtors = statistics.Sum(item => item.DebtorCount);
            GroupCount = statistics.Count;
            RiskGroupCount = statistics.Count(item => item.DebtorCount > 0);
            BestGroupText = statistics
                .Where(item => item.AverageGrade is not null)
                .OrderByDescending(item => item.AverageGrade)
                .Select(item => $"{item.GroupName} ({item.AverageGrade!.Value:F2})")
                .FirstOrDefault() ?? "Нет данных";
            AverageGrade = statistics
                .Where(item => item.AverageGrade is not null)
                .Select(item => item.AverageGrade!.Value)
                .DefaultIfEmpty()
                .Average();
            ResultMessage = $"Загружено групп: {statistics.Count}. Минимальная положительная оценка: {MinPositiveGrade}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить статистику: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
