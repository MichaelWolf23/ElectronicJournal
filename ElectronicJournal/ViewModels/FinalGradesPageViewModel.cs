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

public partial class FinalGradesPageViewModel : PageViewModelBase
{
    private readonly FinalGradeRepository finalGradeRepository;
    private readonly StudentRepository studentRepository;
    private readonly AssignmentRepository assignmentRepository;
    private readonly SettingsRepository settingsRepository;

    [ObservableProperty]
    private ObservableCollection<FinalGradeItem> finalGrades = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> periods = new();

    [ObservableProperty]
    private int selectedStudentId;

    [ObservableProperty]
    private int selectedAssignmentId;

    [ObservableProperty]
    private int selectedPeriodId;

    [ObservableProperty]
    private double? calculatedAverage;

    [ObservableProperty]
    private string finalValue = string.Empty;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Выберите студента, предмет и период.";

    public FinalGradesPageViewModel(
        FinalGradeRepository finalGradeRepository,
        StudentRepository studentRepository,
        AssignmentRepository assignmentRepository,
        SettingsRepository settingsRepository)
        : base("Итоговые оценки")
    {
        this.finalGradeRepository = finalGradeRepository;
        this.studentRepository = studentRepository;
        this.assignmentRepository = assignmentRepository;
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
            FinalGrades = new ObservableCollection<FinalGradeItem>(finalGradeRepository.GetFinalGrades());
            Students = new ObservableCollection<LookupItem>(studentRepository.GetStudentLookups());
            Assignments = new ObservableCollection<LookupItem>(assignmentRepository.GetAssignmentLookups());
            Periods = new ObservableCollection<LookupItem>(finalGradeRepository.GetPeriodLookups());

            SelectedStudentId = Students.FirstOrDefault()?.Id ?? 0;
            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedPeriodId = Periods.FirstOrDefault()?.Id ?? 0;
            ResultMessage = $"Загружено итоговых оценок: {FinalGrades.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить итоговые оценки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Calculate()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0)
        {
            ResultMessage = "Выберите студента и предмет.";
            return;
        }

        CalculatedAverage = finalGradeRepository.CalculateAverage(SelectedStudentId, SelectedAssignmentId);
        if (CalculatedAverage is null)
        {
            FinalValue = string.Empty;
            ResultMessage = "Для выбранного студента и предмета пока нет оценок.";
            return;
        }

        FinalValue = Math.Round(CalculatedAverage.Value, 0, MidpointRounding.AwayFromZero).ToString();
        ResultMessage = $"Средний балл рассчитан: {CalculatedAverage:F2}. Итоговая оценка предложена автоматически.";
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0 || SelectedPeriodId == 0)
        {
            ResultMessage = "Выберите студента, предмет и период.";
            return;
        }

        if (!double.TryParse(FinalValue, out var value))
        {
            ResultMessage = "Итоговая оценка должна быть числом.";
            return;
        }

        if (!IsGradeInScale(value, out var gradeError))
        {
            ResultMessage = gradeError;
            return;
        }

        try
        {
            finalGradeRepository.SaveFinalGrade(new FinalGrade(
                0,
                SelectedStudentId,
                SelectedAssignmentId,
                SelectedPeriodId,
                value,
                CalculatedAverage,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim(),
                null,
                null));

            ResultMessage = "Итоговая оценка сохранена.";
            Comment = string.Empty;
            FinalGrades = new ObservableCollection<FinalGradeItem>(finalGradeRepository.GetFinalGrades());
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить итоговую оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    private bool IsGradeInScale(double value, out string error)
    {
        var minGrade = settingsRepository.GetMinGradeScale();
        var maxGrade = settingsRepository.GetMaxGradeScale();
        if (value < minGrade || value > maxGrade)
        {
            error = $"Итоговая оценка должна быть в пределах от {minGrade} до {maxGrade}.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
