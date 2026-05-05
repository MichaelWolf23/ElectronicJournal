using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class RetakesPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly GradeRetakeRepository gradeRetakeRepository;

    [ObservableProperty]
    private ObservableCollection<GradeRetakeItem> retakes = new();

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

    public RetakesPageViewModel(GradeRepository gradeRepository, GradeRetakeRepository gradeRetakeRepository)
        : base("Пересдачи")
    {
        this.gradeRepository = gradeRepository;
        this.gradeRetakeRepository = gradeRetakeRepository;

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

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Retakes = new ObservableCollection<GradeRetakeItem>(gradeRetakeRepository.GetRetakes());
            Grades = new ObservableCollection<GradeJournalItem>(gradeRepository.GetJournal());
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

        if (NewValue < 0)
        {
            ResultMessage = "Новая оценка не может быть меньше нуля.";
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
                string.IsNullOrWhiteSpace(RetakeDate) ? DateTime.Today.ToString("yyyy-MM-dd") : RetakeDate,
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
            ResultMessage = $"Не удалось сохранить пересдачу: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
