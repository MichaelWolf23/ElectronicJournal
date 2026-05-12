using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ResultMessage = value.GradeValue is 3 or 4
            ? $"Выбрана оценка: {value.StudentName}, {value.SubjectName}."
            : "Пересдачу можно назначить только для оценки 3 или 4.";
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
            Grades = new ObservableCollection<GradeJournalItem>(LoadGradesForCurrentUser()
                .Where(grade => grade.GradeValue is 3 or 4));
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
            ResultMessage = "Сначала выберите оценку 3 или 4.";
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

            if (currentUser.RoleName == "Преподаватель" &&
                !gradeRepository.CanTeacherAccessGrade(SelectedGrade.GradeId, currentUser.UserId))
            {
                ResultMessage = "Преподаватель может оформлять пересдачи только по своим предметам.";
                return;
            }

            var currentValue = gradeRepository.GetGradeValue(SelectedGrade.GradeId) ?? SelectedGrade.GradeValue;
            if (currentValue is not (3 or 4))
            {
                ResultMessage = "Пересдачу можно назначить только для оценки 3 или 4.";
                Load();
                return;
            }

            var retake = new GradeRetake(
                0,
                SelectedGrade.GradeId,
                currentValue,
                NewValue,
                string.IsNullOrWhiteSpace(RetakeDate) ? DateTime.Today.ToString("yyyy-MM-dd") : RetakeDate.Trim(),
                string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim(),
                currentUser.UserId,
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

    [RelayCommand]
    private async Task DeleteSelectedRetake()
    {
        if (SelectedRetake is null)
        {
            ResultMessage = "Сначала выберите пересдачу.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить пересдачу",
            $"Удалить запись о пересдаче: {SelectedRetake.StudentName}, {SelectedRetake.OldValue} -> {SelectedRetake.NewValue}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            gradeRetakeRepository.DeleteRetake(SelectedRetake.RetakeId);
            SelectedRetake = null;
            Load();
            ResultMessage = "Пересдача удалена.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить пересдачу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
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
}
