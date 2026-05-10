using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class RetakesPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly GradeRetakeRepository gradeRetakeRepository;
    private readonly SettingsRepository settingsRepository;

    [ObservableProperty]
    private ObservableCollection<GradeRetakeItem> retakes = new();

    [ObservableProperty]
    private GradeRetakeItem? selectedRetake;

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> grades = new();

    [ObservableProperty]
    private GradeJournalItem? selectedGrade;

    [ObservableProperty]
    private double oldValue;

    [ObservableProperty]
    private double newValue = 4;

    [ObservableProperty]
    private string retakeDate = DateTime.Today.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private string reason = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Выберите оценку для пересдачи.";

    [ObservableProperty]
    private int retakeCount;

    [ObservableProperty]
    private int availableGradeCount;

    [ObservableProperty]
    private string selectedRetakeTitle = "Выберите пересдачу";

    [ObservableProperty]
    private string selectedRetakeDetails = "После выбора строки здесь появится история изменения оценки.";

    public RetakesPageViewModel(
        GradeRepository gradeRepository,
        GradeRetakeRepository gradeRetakeRepository,
        SettingsRepository settingsRepository)
        : base("Пересдачи")
    {
        this.gradeRepository = gradeRepository;
        this.gradeRetakeRepository = gradeRetakeRepository;
        this.settingsRepository = settingsRepository;

        Load();
    }

    partial void OnSelectedGradeChanged(GradeJournalItem? value)
    {
        if (value is null)
        {
            OldValue = 0;
            ResultMessage = "Выберите оценку для пересдачи.";
            return;
        }

        OldValue = value.GradeValue;
        NewValue = value.GradeValue;
        ResultMessage = $"Выбрана оценка: {value.StudentName}, {value.SubjectName}.";
    }

    partial void OnSelectedRetakeChanged(GradeRetakeItem? value)
    {
        if (value is null)
        {
            SelectedRetakeTitle = "Выберите пересдачу";
            SelectedRetakeDetails = "После выбора строки здесь появится история изменения оценки.";
            return;
        }

        SelectedRetakeTitle = $"{value.StudentName}: {value.OldValue} -> {value.NewValue}";
        SelectedRetakeDetails =
            $"{value.GroupName}, {value.SubjectName}. Дата: {value.RetakeDate}. " +
            $"Причина: {value.Reason ?? "не указана"}. Изменил: {value.ChangedByName}.";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Retakes = new ObservableCollection<GradeRetakeItem>(gradeRetakeRepository.GetRetakes());
            Grades = new ObservableCollection<GradeJournalItem>(gradeRepository.GetJournal());
            RetakeCount = Retakes.Count;
            AvailableGradeCount = Grades.Count;
            SelectedGrade ??= Grades.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить пересдачи: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddRetake()
    {
        if (SelectedGrade is null)
        {
            ResultMessage = "Сначала выберите оценку.";
            return;
        }

        if (!IsGradeInScale(NewValue, out var gradeError))
        {
            ResultMessage = gradeError;
            return;
        }

        if (!string.IsNullOrWhiteSpace(RetakeDate) && !DateTime.TryParse(RetakeDate, out _))
        {
            ResultMessage = "Дата пересдачи должна быть в понятном формате, например 2026-02-10.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var currentValue = gradeRepository.GetGradeValue(SelectedGrade.GradeId) ?? SelectedGrade.GradeValue;
            var retake = new GradeRetake(
                0,
                SelectedGrade.GradeId,
                currentValue,
                NewValue,
                string.IsNullOrWhiteSpace(RetakeDate) ? DateTime.Today.ToString("yyyy-MM-dd") : RetakeDate.Trim(),
                string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim(),
                1,
                string.Empty);

            gradeRetakeRepository.AddRetake(retake);
            ResultMessage = "Пересдача сохранена, оценка обновлена.";
            Reason = string.Empty;
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить пересдачу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool IsGradeInScale(double value, out string error)
    {
        var minGrade = settingsRepository.GetMinGradeScale();
        var maxGrade = settingsRepository.GetMaxGradeScale();
        if (value < minGrade || value > maxGrade)
        {
            error = $"Оценка должна быть в пределах от {minGrade} до {maxGrade}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
