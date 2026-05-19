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

public partial class RetakesPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly GradeRetakeRepository gradeRetakeRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<RetakeEntryRow> allRetakeRows = new();

    [ObservableProperty]
    private ObservableCollection<GradeRetakeItem> retakes = new();

    [ObservableProperty]
    private GradeRetakeItem? selectedRetake;

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> grades = new();

    [ObservableProperty]
    private ObservableCollection<RetakeEntryRow> retakeRows = new();

    [ObservableProperty]
    private ObservableCollection<string> retakeFilters = new();

    [ObservableProperty]
    private RetakeEntryRow? selectedRetakeRow;

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
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedRetakeFilter = "Все";

    [ObservableProperty]
    private int retakeCount;

    [ObservableProperty]
    private int availableGradeCount;

    [ObservableProperty]
    private int pendingRetakeCount;

    [ObservableProperty]
    private int visibleRetakeRowCount;

    [ObservableProperty]
    private string selectedRetakeTitle = "Выберите пересдачу";

    [ObservableProperty]
    private string selectedRetakeDetails = "После выбора строки здесь появится история изменения оценки.";

    [ObservableProperty]
    private string selectedGradeTitle = "Выберите оценку 2, 3 или 4";

    [ObservableProperty]
    private string selectedGradeDetails = "После выбора оценки справа можно оформить пересдачу.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSaveSelectedRetake))]
    [NotifyPropertyChangedFor(nameof(CanDeleteSelectedRetake))]
    private string selectedNewValueText = string.Empty;

    [ObservableProperty]
    private string selectedRetakeDate = DateTime.Today.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private string selectedReason = string.Empty;

    [ObservableProperty]
    private string selectedRetakeStatus = "Выберите строку.";

    public bool CanSaveSelectedRetake => SelectedRetakeRow?.CanCreateRetake == true;

    public bool CanDeleteSelectedRetake => SelectedRetakeRow?.HasRetake == true;

    public static IReadOnlyList<string> FilterOptions { get; } =
    [
        "Все",
        "Можно оформить",
        "Уже оформлены",
        "Двойки",
        "Тройки и четверки"
    ];

    public RetakesPageViewModel(
        GradeRepository gradeRepository,
        GradeRetakeRepository gradeRetakeRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Пересдачи")
    {
        this.gradeRepository = gradeRepository;
        this.gradeRetakeRepository = gradeRetakeRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;

        Load();
    }

    partial void OnSearchTextChanged(string value) => ApplyRetakeFilter();

    partial void OnSelectedRetakeFilterChanged(string value) => ApplyRetakeFilter();

    partial void OnSelectedGradeChanged(GradeJournalItem? value)
    {
        if (value is null)
        {
            OldValue = 0;
            ResultMessage = "Выберите оценку для пересдачи.";
            SelectedGradeTitle = "Выберите оценку 2, 3 или 4";
            SelectedGradeDetails = "После выбора оценки справа можно оформить пересдачу.";
            return;
        }

        OldValue = value.GradeValue;
        NewValue = value.GradeValue < 4 ? 4 : 5;
        SelectedGradeTitle = $"{value.StudentName}: {value.GradeValue}";
        SelectedGradeDetails =
            $"{value.GroupName}, {value.SubjectName}. Тип: {value.GradeType}. " +
            $"Дата: {value.GradeDate}.";
        ResultMessage = value.GradeValue is 2 or 3 or 4
            ? $"Выбрана оценка: {value.StudentName}, {value.SubjectName}."
            : "Пересдачу можно назначить только для оценки 2, 3 или 4.";
    }

    partial void OnSelectedRetakeRowChanged(RetakeEntryRow? value)
    {
        if (value is null)
        {
            ResultMessage = "Выберите строку для пересдачи.";
            SelectedNewValueText = string.Empty;
            SelectedRetakeDate = DateTime.Today.ToString("yyyy-MM-dd");
            SelectedReason = string.Empty;
            SelectedRetakeStatus = "Выберите строку.";
            OnPropertyChanged(nameof(CanSaveSelectedRetake));
            OnPropertyChanged(nameof(CanDeleteSelectedRetake));
            return;
        }

        SelectedNewValueText = value.HasRetake
            ? value.LastRetakeValue?.ToString("0.##") ?? string.Empty
            : value.NewValueText;
        SelectedRetakeDate = value.HasRetake
            ? value.LastRetakeDate ?? DateTime.Today.ToString("yyyy-MM-dd")
            : DateTime.Today.ToString("yyyy-MM-dd");
        SelectedReason = value.HasRetake ? value.LastRetakeReason ?? string.Empty : string.Empty;
        SelectedRetakeStatus = value.HasRetake
            ? $"Пересдача уже оформлена: {value.ResultText}. Повторно сохранять нельзя."
            : $"Можно оформить одну пересдачу для оценки {value.OldValue:0.##}.";
        ResultMessage = $"Выбрана оценка: {value.StudentName}, {value.SubjectName}.";
        OnPropertyChanged(nameof(CanSaveSelectedRetake));
        OnPropertyChanged(nameof(CanDeleteSelectedRetake));
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

            Retakes = new ObservableCollection<GradeRetakeItem>(LoadRetakesForCurrentUser());
            var selectedGradeId = SelectedRetakeRow?.GradeId ?? SelectedGrade?.GradeId;
            Grades = new ObservableCollection<GradeJournalItem>(LoadGradesForCurrentUser());
            RetakeFilters = new ObservableCollection<string>(FilterOptions);
            allRetakeRows = BuildRetakeRows(Grades);
            RetakeCount = Retakes.Count;
            AvailableGradeCount = allRetakeRows.Count;
            PendingRetakeCount = allRetakeRows.Count(row => row.CanCreateRetake);
            ApplyRetakeFilter();
            SelectedRetakeRow = RetakeRows.FirstOrDefault(row => row.GradeId == selectedGradeId) ?? RetakeRows.FirstOrDefault();
            SelectedGrade = Grades.FirstOrDefault(grade => grade.GradeId == selectedGradeId) ?? Grades.FirstOrDefault();
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
        if (SelectedRetakeRow is null)
        {
            ResultMessage = "Сначала выберите оценку 2, 3 или 4.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (SelectedRetakeRow.HasRetake)
        {
            ResultMessage = "По этой оценке пересдача уже оформлена. Повторная пересдача недоступна.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!double.TryParse(
                SelectedRetakeRow.NewValueText.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var newValue))
        {
            ResultMessage = "Новая оценка должна быть числом.";
            NotifyWarning(ResultMessage);
            return;
        }

        if (!IsGradeInScale(newValue, out var gradeError))
        {
            ResultMessage = gradeError;
            NotifyWarning(ResultMessage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(SelectedRetakeRow.RetakeDate) &&
            !DateTime.TryParse(SelectedRetakeRow.RetakeDate, out _))
        {
            ResultMessage = "Дата пересдачи должна быть в понятном формате, например 2026-02-10.";
            NotifyWarning(ResultMessage);
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            if (currentUser.RoleName == "Преподаватель" &&
                !gradeRepository.CanTeacherAccessGrade(SelectedRetakeRow.GradeId, currentUser.UserId))
            {
                ResultMessage = "Преподаватель может оформлять пересдачи только по своим предметам.";
                NotifyWarning(ResultMessage);
                return;
            }

            if (gradeRetakeRepository.HasRetakeForGrade(SelectedRetakeRow.GradeId))
            {
                ResultMessage = "По этой оценке пересдача уже оформлена. Повторная пересдача недоступна.";
                NotifyWarning(ResultMessage);
                Load();
                return;
            }

            var currentValue = gradeRepository.GetGradeValue(SelectedRetakeRow.GradeId) ?? SelectedRetakeRow.OldValue;
            if (currentValue is not (2 or 3 or 4))
            {
                ResultMessage = "Пересдачу можно назначить только для оценки 2, 3 или 4.";
                NotifyWarning(ResultMessage);
                Load();
                return;
            }

            var retake = new GradeRetake(
                0,
                SelectedRetakeRow.GradeId,
                currentValue,
                newValue,
                string.IsNullOrWhiteSpace(SelectedRetakeRow.RetakeDate) ? DateTime.Today.ToString("yyyy-MM-dd") : SelectedRetakeRow.RetakeDate.Trim(),
                string.IsNullOrWhiteSpace(SelectedRetakeRow.Reason) ? null : SelectedRetakeRow.Reason.Trim(),
                currentUser.UserId,
                string.Empty);

            gradeRetakeRepository.AddRetake(retake);
            ResultMessage = "Пересдача сохранена, оценка обновлена.";
            NotifySuccess(ResultMessage);
            Load();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить пересдачу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedRetake()
    {
        if (SelectedRetakeRow?.RetakeId is not int retakeId)
        {
            ResultMessage = "У выбранной оценки еще нет пересдачи для удаления.";
            NotifyInfo(ResultMessage);
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить пересдачу",
            $"Удалить последнюю пересдачу студента {SelectedRetakeRow.StudentName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            gradeRetakeRepository.DeleteRetake(retakeId);
            SelectedRetake = null;
            Load();
            ResultMessage = "Пересдача удалена.";
            NotifySuccess(ResultMessage);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить пересдачу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
            NotifyError(ResultMessage);
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

    private List<GradeRetakeItem> LoadRetakesForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => gradeRetakeRepository.GetRetakesForTeacher(currentUser.UserId),
            "Куратор группы" => gradeRetakeRepository.GetRetakesForCurator(currentUser.UserId),
            _ => gradeRetakeRepository.GetRetakes()
        };
    }

    private List<GradeJournalItem> LoadGradesForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => gradeRepository.GetJournalForTeacher(currentUser.UserId),
            "Куратор группы" => gradeRepository.GetJournalForCurator(currentUser.UserId),
            _ => gradeRepository.GetJournal()
        };
    }

    private List<RetakeEntryRow> BuildRetakeRows(IEnumerable<GradeJournalItem> grades)
    {
        var latestRetakes = gradeRetakeRepository.GetLatestRetakes()
            .ToDictionary(retake => retake.OriginalGradeId);

        return grades
            .Where(grade => grade.GradeValue is 2 or 3 or 4 || latestRetakes.ContainsKey(grade.GradeId))
            .Select(grade =>
            {
                latestRetakes.TryGetValue(grade.GradeId, out var retake);
                return new RetakeEntryRow(
                    grade,
                    retake?.RetakeId,
                    retake?.OldValue,
                    retake?.NewValue,
                    retake?.RetakeDate,
                    retake?.Reason);
            })
            .ToList();
    }

    private void ApplyRetakeFilter()
    {
        IEnumerable<RetakeEntryRow> filtered = allRetakeRows;

        filtered = SelectedRetakeFilter switch
        {
            "Можно оформить" => filtered.Where(row => row.CanCreateRetake),
            "Уже оформлены" => filtered.Where(row => row.HasRetake),
            "Двойки" => filtered.Where(row => Math.Abs(row.OldValue - 2) < 0.001),
            "Тройки и четверки" => filtered.Where(row => row.OldValue is 3 or 4),
            _ => filtered
        };

        var query = SearchText.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(row =>
                row.StudentName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.GroupName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.SubjectName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                row.GradeType.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        var visible = filtered.ToList();
        RetakeRows = new ObservableCollection<RetakeEntryRow>(visible);
        VisibleRetakeRowCount = visible.Count;

        if (SelectedRetakeRow is not null && !visible.Any(row => row.GradeId == SelectedRetakeRow.GradeId))
        {
            SelectedRetakeRow = visible.FirstOrDefault();
        }
    }
}
