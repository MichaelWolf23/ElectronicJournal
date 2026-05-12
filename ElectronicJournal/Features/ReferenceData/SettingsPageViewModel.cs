using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;

namespace ElectronicJournal.ViewModels;

public partial class SettingsPageViewModel : PageViewModelBase
{
    private readonly SettingsRepository settingsRepository;
    private readonly BackupService backupService;

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

    [ObservableProperty]
    private string backupResultMessage = "Резервная копия еще не создавалась.";

    public SettingsPageViewModel(SettingsRepository settingsRepository, BackupService backupService)
        : base("Настройки")
    {
        this.settingsRepository = settingsRepository;
        this.backupService = backupService;
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

        if (!IsSettingValueValid(SelectedSetting.SettingKey, SettingValue.Trim(), out var validationError))
        {
            ResultMessage = validationError;
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

    [RelayCommand]
    private void CreateBackup()
    {
        try
        {
            IsBusy = true;
            var backupPath = backupService.CreateBackup();
            BackupResultMessage = $"Резервная копия создана: {backupPath}";
            ResultMessage = "База данных сохранена в отдельный файл.";
        }
        catch (Exception ex)
        {
            BackupResultMessage = $"Не удалось создать резервную копию: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ArchiveSemester()
    {
        try
        {
            IsBusy = true;
            var result = backupService.ArchiveCurrentPeriod();
            BackupResultMessage = result.ArchivedRows > 0
                ? $"Период \"{result.PeriodName}\" архивирован. Копия базы: {result.BackupPath}"
                : $"Резервная копия создана, но период \"{result.PeriodName}\" уже был архивирован или не найден.";
            ResultMessage = "Архивирование выполнено после создания резервной копии.";
        }
        catch (Exception ex)
        {
            BackupResultMessage = $"Не удалось архивировать период: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool IsSettingValueValid(string key, string value, out string error)
    {
        error = string.Empty;

        if (key is "Минимальная положительная оценка" or "Минимальная оценка шкалы" or "Максимальная оценка шкалы")
        {
            if (!double.TryParse(value, out var grade) || grade < 0 || grade > 100)
            {
                error = "Оценочный параметр должен быть числом.";
                return false;
            }
        }

        if (key == "Автоматические уведомления кураторам" &&
            value is not ("Включены" or "Отключены" or "Да" or "Нет" or "true" or "false" or "1" or "0"))
        {
            error = "Для уведомлений используйте значение: Включены или Отключены.";
            return false;
        }

        return true;
    }
}
