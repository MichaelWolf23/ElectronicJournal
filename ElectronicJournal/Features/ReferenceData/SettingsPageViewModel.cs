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

    [ObservableProperty]
    private string currentPeriodInput = string.Empty;

    [ObservableProperty]
    private string minPositiveGradeInput = string.Empty;

    [ObservableProperty]
    private string minGradeScaleInput = string.Empty;

    [ObservableProperty]
    private string maxGradeScaleInput = string.Empty;

    [ObservableProperty]
    private bool curatorNotificationsEnabled = true;

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

    public override void OnNavigatedTo()
    {
        Load();
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
            CurrentPeriodInput = CurrentPeriod == "Не задан" ? string.Empty : CurrentPeriod;
            MinPositiveGradeInput = Settings.FirstOrDefault(setting => setting.SettingKey == "Минимальная положительная оценка")?.SettingValue ?? "3";
            MinGradeScaleInput = minGrade == "?" ? "2" : minGrade;
            MaxGradeScaleInput = maxGrade == "?" ? "5" : maxGrade;
            CuratorNotificationsEnabled = settingsRepository.AreCuratorNotificationsEnabled();
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
    private void SaveJournalSettings()
    {
        if (string.IsNullOrWhiteSpace(CurrentPeriodInput))
        {
            ResultMessage = "Укажите текущий учебный период.";
            NotifyWarning(ResultMessage);
            return;
        }

        SaveKnownSetting("Текущий учебный период", CurrentPeriodInput.Trim(), "Период журнала сохранен.");
    }

    [RelayCommand]
    private void SaveGradeSettings()
    {
        if (!double.TryParse(MinGradeScaleInput, out var minGrade) ||
            !double.TryParse(MaxGradeScaleInput, out var maxGrade) ||
            !double.TryParse(MinPositiveGradeInput, out var minPositiveGrade))
        {
            ResultMessage = "Оценочные параметры должны быть числами.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (minGrade < 0 || maxGrade > 100 || minGrade >= maxGrade)
        {
            ResultMessage = "Минимальная оценка должна быть меньше максимальной.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (minPositiveGrade < minGrade || minPositiveGrade > maxGrade)
        {
            ResultMessage = "Положительная оценка должна быть внутри шкалы оценивания.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            settingsRepository.UpdateSetting("Минимальная оценка шкалы", MinGradeScaleInput.Trim());
            settingsRepository.UpdateSetting("Максимальная оценка шкалы", MaxGradeScaleInput.Trim());
            settingsRepository.UpdateSetting("Минимальная положительная оценка", MinPositiveGradeInput.Trim());
            ResultMessage = "Настройки оценивания сохранены.";
            NotifySuccess(ResultMessage);
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить настройки оценивания: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void SaveNotificationSettings()
    {
        SaveKnownSetting(
            "Автоматические уведомления кураторам",
            CuratorNotificationsEnabled ? "Включены" : "Отключены",
            CuratorNotificationsEnabled
                ? "Уведомления кураторам включены."
                : "Уведомления кураторам отключены.");
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedSetting is null)
        {
            ResultMessage = "Сначала выберите настройку.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (string.IsNullOrWhiteSpace(SettingValue))
        {
            ResultMessage = "Значение настройки не может быть пустым.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!IsSettingValueValid(SelectedSetting.SettingKey, SettingValue.Trim(), out var validationError))
        {
            ResultMessage = validationError;
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            settingsRepository.UpdateSetting(SelectedSetting.SettingKey, SettingValue.Trim());
            ResultMessage = $"Настройка \"{SelectedSetting.SettingKey}\" сохранена.";
            NotifySuccess(ResultMessage);
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
            NotifyError(ResultMessage);
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
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            BackupResultMessage = $"Не удалось создать резервную копию: {ex.Message}";
            NotifyError(BackupResultMessage);
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
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            BackupResultMessage = $"Не удалось архивировать период: {ex.Message}";
            NotifyError(BackupResultMessage);
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

    private void SaveKnownSetting(string key, string value, string successMessage)
    {
        try
        {
            settingsRepository.UpdateSetting(key, value);
            ResultMessage = successMessage;
            NotifySuccess(ResultMessage);
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить настройку: {ex.Message}";
            NotifyError(ResultMessage);
        }
    }
}
