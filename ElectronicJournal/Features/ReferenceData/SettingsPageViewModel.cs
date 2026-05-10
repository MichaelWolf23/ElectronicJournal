using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class SettingsPageViewModel : PageViewModelBase
{
    private readonly SettingsRepository settingsRepository;

    [ObservableProperty]
    private ObservableCollection<SystemSetting> settings = new();

    [ObservableProperty]
    private SystemSetting? selectedSetting;

    [ObservableProperty]
    private string settingValue = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Выберите настройку для изменения.";

    [ObservableProperty]
    private int settingCount;

    [ObservableProperty]
    private string currentPeriod = "Не задан";

    [ObservableProperty]
    private string gradeScaleText = "Не задана";

    public SettingsPageViewModel(SettingsRepository settingsRepository)
        : base("Настройки")
    {
        this.settingsRepository = settingsRepository;
        Load();
    }

    partial void OnSelectedSettingChanged(SystemSetting? value)
    {
        SettingValue = value?.SettingValue ?? string.Empty;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Settings = new ObservableCollection<SystemSetting>(settingsRepository.GetSettings());
            SettingCount = Settings.Count;
            CurrentPeriod = Settings.FirstOrDefault(setting => setting.SettingKey == "Текущий учебный период")?.SettingValue ?? "Не задан";
            var minGrade = Settings.FirstOrDefault(setting => setting.SettingKey == "Минимальная оценка шкалы")?.SettingValue ?? "?";
            var maxGrade = Settings.FirstOrDefault(setting => setting.SettingKey == "Максимальная оценка шкалы")?.SettingValue ?? "?";
            GradeScaleText = $"{minGrade}-{maxGrade}";
            ResultMessage = $"Загружено настроек: {Settings.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить настройки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedSetting is null)
        {
            ResultMessage = "Сначала выберите настройку.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SettingValue))
        {
            ResultMessage = "Значение настройки не может быть пустым.";
            return;
        }

        try
        {
            settingsRepository.UpdateSetting(SelectedSetting.SettingKey, SettingValue.Trim());
            ResultMessage = $"Настройка \"{SelectedSetting.SettingKey}\" сохранена.";
            var selectedKey = SelectedSetting.SettingKey;
            Load();

            foreach (var setting in Settings)
            {
                if (setting.SettingKey == selectedKey)
                {
                    SelectedSetting = setting;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить настройку: {ex.Message}";
        }
    }
}
