using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class FinalGradesPageViewModel : PageViewModelBase
{
    private readonly FinalGradeRepository finalGradeRepository;
    private readonly StudentRepository studentRepository;
    private readonly AssignmentRepository assignmentRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;

    [ObservableProperty]
    private ObservableCollection<FinalGradeItem> finalGrades = new();

    [ObservableProperty]
    private FinalGradeItem? selectedFinalGrade;

    [ObservableProperty]
    private ObservableCollection<LookupItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> periods = new();

    [ObservableProperty]
    private ObservableCollection<FinalGradeSheetRow> sheetRows = new();

    [ObservableProperty]
    private FinalGradeSheetRow? selectedSheetRow;

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

    [ObservableProperty]
    private int finalGradeCount;

    [ObservableProperty]
    private string finalAverageText = "Нет данных";

    [ObservableProperty]
    private string selectedFinalTitle = "Выберите итоговую оценку";

    [ObservableProperty]
    private string selectedFinalDetails = "После выбора строки здесь появится итог по студенту.";

    [ObservableProperty]
    private int sheetStudentCount;

    [ObservableProperty]
    private int savedSheetCount;

    [ObservableProperty]
    private string sheetAverageText = "Нет данных";

    partial void OnSelectedAssignmentIdChanged(int value) => LoadSheet();

    partial void OnSelectedPeriodIdChanged(int value) => LoadSheet();

    partial void OnSelectedSheetRowChanged(FinalGradeSheetRow? value)
    {
        SaveSelectedSheetRowCommand.NotifyCanExecuteChanged();
    }

    public FinalGradesPageViewModel(
        FinalGradeRepository finalGradeRepository,
        StudentRepository studentRepository,
        AssignmentRepository assignmentRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Итоговые оценки")
    {
        this.finalGradeRepository = finalGradeRepository;
        this.studentRepository = studentRepository;
        this.assignmentRepository = assignmentRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;

        Load();
    }

    [RelayCommand]
    private async Task DeleteSelectedFinalGrade()
    {
        if (SelectedFinalGrade is null)
        {
            ResultMessage = "Сначала выберите итоговую оценку.";
            NotifyWarning(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить итоговую",
            $"Удалить итоговую оценку {SelectedFinalGrade.FinalValue} у студента {SelectedFinalGrade.StudentName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            finalGradeRepository.DeleteFinalGrade(SelectedFinalGrade.FinalGradeId);
            SelectedFinalGrade = null;
            FinalGrades = new ObservableCollection<FinalGradeItem>(LoadFinalGradesForCurrentUser());
            FinalGradeCount = FinalGrades.Count;
            FinalAverageText = FinalGrades.Count == 0
                ? "Нет данных"
                : FinalGrades.Average(grade => grade.FinalValue).ToString("F2");
            ResultMessage = "Итоговая оценка удалена.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить итоговую оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    partial void OnSelectedFinalGradeChanged(FinalGradeItem? value)
    {
        if (value is null)
        {
            SelectedFinalTitle = "Выберите итоговую оценку";
            SelectedFinalDetails = "После выбора строки здесь появится итог по студенту.";
            return;
        }

        SelectedFinalTitle = $"{value.StudentName} - {value.FinalValue}";
        SelectedFinalDetails =
            $"{value.GroupName}, {value.SubjectName}, {value.PeriodName}. " +
            $"Средний: {value.CalculatedAverage?.ToString("F2") ?? "нет данных"}. " +
            $"Комментарий: {value.Comment ?? "не указан"}.";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            FinalGrades = new ObservableCollection<FinalGradeItem>(LoadFinalGradesForCurrentUser());
            FinalGradeCount = FinalGrades.Count;
            FinalAverageText = FinalGrades.Count == 0
                ? "Нет данных"
                : FinalGrades.Average(grade => grade.FinalValue).ToString("F2");
            Students = new ObservableCollection<LookupItem>(LoadStudentLookupsForCurrentUser());
            Assignments = new ObservableCollection<LookupItem>(LoadAssignmentLookupsForCurrentUser());
            Periods = new ObservableCollection<LookupItem>(finalGradeRepository.GetPeriodLookups());

            SelectedStudentId = Students.FirstOrDefault()?.Id ?? 0;
            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedPeriodId = Periods.FirstOrDefault()?.Id ?? 0;
            LoadSheet();
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
    private void SaveSheet()
    {
        var rowsToSave = SheetRows
            .Where(row => (row.IsDirty || !row.HasSavedFinalGrade) && !string.IsNullOrWhiteSpace(row.FinalValueText))
            .ToList();
        if (rowsToSave.Count == 0)
        {
            ResultMessage = "Нет строк для сохранения.";
            NotifyInfo(ResultMessage);
            return;
        }

        SaveRows(rowsToSave, "Ведомость сохранена");
    }

    [RelayCommand]
    private void SaveSheetRow(FinalGradeSheetRow? row)
    {
        if (row is null)
        {
            ResultMessage = "Сначала выберите строку ведомости.";
            NotifyWarning(ResultMessage);
            return;
        }

        SaveRows(new List<FinalGradeSheetRow> { row }, "Строка сохранена");
    }

    [RelayCommand(CanExecute = nameof(CanSaveSelectedSheetRow))]
    private void SaveSelectedSheetRow()
    {
        if (SelectedSheetRow is null)
        {
            ResultMessage = "Сначала выберите строку ведомости.";
            NotifyWarning(ResultMessage);
            return;
        }

        SaveRows(new List<FinalGradeSheetRow> { SelectedSheetRow }, "Строка сохранена");
    }

    [RelayCommand]
    private async Task DeleteSheetRowFinalGrade(FinalGradeSheetRow? row)
    {
        if (row is null)
        {
            ResultMessage = "Сначала выберите строку ведомости.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!row.HasSavedFinalGrade)
        {
            ResultMessage = "У выбранного студента нет сохраненного итога.";
            NotifyInfo(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить итог",
            $"Удалить итоговую оценку у студента {row.StudentName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            finalGradeRepository.DeleteFinalGrade(row.StudentId, SelectedAssignmentId, SelectedPeriodId);
            ReloadFinalGradesAndSheet();
            ResultMessage = "Итоговая оценка удалена.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить итоговую оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private async Task DeleteAllSheetFinalGrades()
    {
        if (SelectedAssignmentId == 0 || SelectedPeriodId == 0)
        {
            ResultMessage = "Выберите предмет и период.";
            NotifyWarning(ResultMessage);
            return;
        }

        var savedCount = SheetRows.Count(row => row.HasSavedFinalGrade);
        if (savedCount == 0)
        {
            ResultMessage = "В ведомости нет сохраненных итогов.";
            NotifyInfo(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить все итоги",
            $"Удалить все сохраненные итоговые оценки в этой ведомости? Количество: {savedCount}.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            var deleted = finalGradeRepository.DeleteFinalGradesForSheet(SelectedAssignmentId, SelectedPeriodId);
            ReloadFinalGradesAndSheet();
            ResultMessage = $"Удалено итоговых оценок: {deleted}.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить итоговые оценки: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    private bool CanSaveSelectedSheetRow() => SelectedSheetRow is not null;

    private bool CanDeleteSelectedSheetFinalGrade() => SelectedSheetRow?.HasSavedFinalGrade == true;

    private void SaveRows(IReadOnlyCollection<FinalGradeSheetRow> rowsToSave, string successPrefix)
    {
        if (SelectedAssignmentId == 0 || SelectedPeriodId == 0)
        {
            ResultMessage = "Выберите предмет и период.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!Assignments.Any(assignment => assignment.Id == SelectedAssignmentId))
        {
            ResultMessage = "Выбранный предмет недоступен текущему пользователю.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (rowsToSave.Count == 0)
        {
            ResultMessage = "Нет строк для сохранения.";
            NotifyInfo(ResultMessage);
            return;
        }

        try
        {
            var saved = 0;
            foreach (var row in rowsToSave)
            {
                if (string.IsNullOrWhiteSpace(row.FinalValueText))
                {
                    ResultMessage = $"{row.StudentName}: укажите итоговую оценку.";
                    NotifyWarning(ResultMessage);
                    return;
                }

                if (!double.TryParse(row.FinalValueText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    ResultMessage = $"Проверьте итоговую оценку у студента {row.StudentName}.";
                    NotifyWarning(ResultMessage);
                    return;
                }

                if (!IsGradeInScale(value, out var gradeError))
                {
                    ResultMessage = $"{row.StudentName}: {gradeError}";
                    NotifyWarning(ResultMessage);
                    return;
                }

                finalGradeRepository.SaveFinalGrade(new FinalGrade(
                    0,
                    row.StudentId,
                    SelectedAssignmentId,
                    SelectedPeriodId,
                    value,
                    row.CalculatedAverage,
                    string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment.Trim(),
                    currentUser.UserId,
                    null));
                saved++;
            }

            ReloadFinalGradesAndSheet();
            ResultMessage = $"{successPrefix}. Итогов записано: {saved}.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить ведомость: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    [RelayCommand]
    private void Calculate()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0)
        {
            ResultMessage = "Выберите студента и предмет.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!CanUseSelectedScope(out var scopeError))
        {
            ResultMessage = scopeError;
            NotifyWarning(ResultMessage);
            return;
        }

        CalculatedAverage = finalGradeRepository.CalculateAverage(SelectedStudentId, SelectedAssignmentId);
        if (CalculatedAverage is null)
        {
            FinalValue = string.Empty;
            ResultMessage = "Для выбранного студента и предмета пока нет оценок.";
            NotifyInfo(ResultMessage);
            return;
        }

        FinalValue = Math.Round(CalculatedAverage.Value, 0, MidpointRounding.AwayFromZero).ToString();
        ResultMessage = $"Средний балл рассчитан: {CalculatedAverage:F2}. Итоговая оценка предложена автоматически.";
        NotifySuccess(ResultMessage);
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0 || SelectedPeriodId == 0)
        {
            ResultMessage = "Выберите студента, предмет и период.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!CanUseSelectedScope(out var scopeError))
        {
            ResultMessage = scopeError;
            NotifyWarning(ResultMessage);
            return;
        }

        if (!double.TryParse(FinalValue, out var value))
        {
            ResultMessage = "Итоговая оценка должна быть числом.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!IsGradeInScale(value, out var gradeError))
        {
            ResultMessage = gradeError;
            NotifyWarning(ResultMessage);
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
                currentUser.UserId,
                null));

            ResultMessage = "Итоговая оценка сохранена.";
            NotifySuccess(ResultMessage);
            Comment = string.Empty;
            FinalGrades = new ObservableCollection<FinalGradeItem>(LoadFinalGradesForCurrentUser());
            FinalGradeCount = FinalGrades.Count;
            FinalAverageText = FinalGrades.Count == 0
                ? "Нет данных"
                : FinalGrades.Average(grade => grade.FinalValue).ToString("F2");
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить итоговую оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
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

    private List<FinalGradeItem> LoadFinalGradesForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => finalGradeRepository.GetFinalGradesForTeacher(currentUser.UserId),
            "Куратор группы" => finalGradeRepository.GetFinalGradesForCurator(currentUser.UserId),
            _ => finalGradeRepository.GetFinalGrades()
        };
    }

    private List<LookupItem> LoadStudentLookupsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => studentRepository.GetStudentLookupsForTeacher(currentUser.UserId),
            "Куратор группы" => studentRepository.GetStudentLookupsForCurator(currentUser.UserId),
            _ => studentRepository.GetStudentLookups()
        };
    }

    private List<LookupItem> LoadAssignmentLookupsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? assignmentRepository.GetAssignmentLookupsForTeacher(currentUser.UserId)
            : assignmentRepository.GetAssignmentLookups();
    }

    private bool CanUseSelectedScope(out string error)
    {
        if (!Students.Any(student => student.Id == SelectedStudentId))
        {
            error = "Выбранный студент недоступен текущему пользователю.";
            return false;
        }

        if (!Assignments.Any(assignment => assignment.Id == SelectedAssignmentId))
        {
            error = "Выбранный предмет недоступен текущему пользователю.";
            return false;
        }

        if (!finalGradeRepository.CanStudentUseAssignment(SelectedStudentId, SelectedAssignmentId))
        {
            error = "Студент не относится к группе выбранного предмета.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void ReloadFinalGradesAndSheet()
    {
        FinalGrades = new ObservableCollection<FinalGradeItem>(LoadFinalGradesForCurrentUser());
        FinalGradeCount = FinalGrades.Count;
        FinalAverageText = FinalGrades.Count == 0
            ? "Нет данных"
            : FinalGrades.Average(grade => grade.FinalValue).ToString("F2");
        LoadSheet();
    }

    private void LoadSheet()
    {
        if (SelectedAssignmentId == 0 || SelectedPeriodId == 0)
        {
            SheetRows = new ObservableCollection<FinalGradeSheetRow>();
            UpdateSheetSummary();
            return;
        }

        if (!Assignments.Any(assignment => assignment.Id == SelectedAssignmentId))
        {
            SheetRows = new ObservableCollection<FinalGradeSheetRow>();
            UpdateSheetSummary();
            return;
        }

        try
        {
            SheetRows = new ObservableCollection<FinalGradeSheetRow>(
                finalGradeRepository.GetFinalGradeSheet(SelectedAssignmentId, SelectedPeriodId));
            SelectedSheetRow = SheetRows.FirstOrDefault();
            UpdateSheetSummary();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось загрузить ведомость: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
    }

    private void UpdateSheetSummary()
    {
        SheetStudentCount = SheetRows.Count;
        SavedSheetCount = SheetRows.Count(row => row.SavedFinalValue is not null);
        var averages = SheetRows
            .Where(row => row.CalculatedAverage is not null)
            .Select(row => row.CalculatedAverage!.Value)
            .ToList();
        SheetAverageText = averages.Count == 0 ? "Нет данных" : averages.Average().ToString("F2");
    }
}
