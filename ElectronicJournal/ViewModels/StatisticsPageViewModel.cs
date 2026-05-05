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

    [ObservableProperty]
    private ObservableCollection<GroupStatisticsItem> groupStatistics = new();

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

    public StatisticsPageViewModel(
        GroupRepository groupRepository,
        SettingsRepository settingsRepository)
        : base("Статистика")
    {
        this.groupRepository = groupRepository;
        this.settingsRepository = settingsRepository;

        Load();
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            MinPositiveGrade = settingsRepository.GetMinPositiveGrade();

            var statistics = groupRepository.GetGroupStatistics(MinPositiveGrade);
            GroupStatistics = new ObservableCollection<GroupStatisticsItem>(statistics);
            TotalStudents = statistics.Sum(item => item.StudentCount);
            TotalDebtors = statistics.Sum(item => item.DebtorCount);
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
