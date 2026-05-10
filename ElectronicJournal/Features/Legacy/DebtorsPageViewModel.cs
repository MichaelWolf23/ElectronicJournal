using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class DebtorsPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly NotificationRepository notificationRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<DebtorItem> allDebtors = new();

    [ObservableProperty]
    private ObservableCollection<DebtorItem> debtors = new();

    [ObservableProperty]
    private ObservableCollection<string> groupFilters = new();

    [ObservableProperty]
    private ObservableCollection<string> subjectFilters = new();

    [ObservableProperty]
    private DebtorItem? selectedDebtor;

    [ObservableProperty]
    private string? selectedGroupFilter;

    [ObservableProperty]
    private string? selectedSubjectFilter;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private double minPositiveGrade = 3;

    [ObservableProperty]
    private string resultMessage = "Выберите должника, чтобы создать уведомление куратору.";

    [ObservableProperty]
    private int visibleDebtCount;

    [ObservableProperty]
    private int uniqueStudentCount;

    [ObservableProperty]
    private int affectedGroupCount;

    [ObservableProperty]
    private string worstGradeText = "Нет данных";

    [ObservableProperty]
    private string selectedProblemTitle = "Выберите запись";

    [ObservableProperty]
    private string selectedProblemDetails = "После выбора строки здесь появятся детали задолженности.";

    [ObservableProperty]
    private string selectedProblemComment = "Комментарий не выбран";

    [ObservableProperty]
    private string notificationPreview = "Текст уведомления сформируется автоматически после выбора должника.";

    [ObservableProperty]
    private bool canCreateNotification;

    [ObservableProperty]
    private bool isNotificationHelpVisible;

    public DebtorsPageViewModel(
        GradeRepository gradeRepository,
        NotificationRepository notificationRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Должники")
    {
        this.gradeRepository = gradeRepository;
        this.notificationRepository = notificationRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;
        CanCreateNotification = currentUser.RoleName is "Преподаватель" or "Администратор";
        IsNotificationHelpVisible = !CanCreateNotification;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(string? value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(string? value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedDebtorChanged(DebtorItem? value)
    {
        if (value is null)
        {
            SelectedProblemTitle = "Выберите запись";
            SelectedProblemDetails = "После выбора строки здесь появятся детали задолженности.";
            SelectedProblemComment = "Комментарий не выбран";
            NotificationPreview = "Текст уведомления сформируется автоматически после выбора должника.";
            return;
        }

        SelectedProblemTitle = $"{value.StudentName} - {value.SubjectName}";
        SelectedProblemDetails =
            $"Группа {value.GroupName}, оценка {value.GradeValue} от {value.GradeDate}. " +
            $"Преподаватель: {value.TeacherName}.";
        SelectedProblemComment = string.IsNullOrWhiteSpace(value.Comment)
            ? "Комментарий к оценке не указан."
            : value.Comment;
        NotificationPreview = BuildNotificationMessage(value);
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            MinPositiveGrade = settingsRepository.GetMinPositiveGrade();
            allDebtors = LoadDebtorsForCurrentUser();
            GroupFilters = new ObservableCollection<string>(
                allDebtors.Select(debtor => debtor.GroupName).Distinct().OrderBy(name => name));
            SubjectFilters = new ObservableCollection<string>(
                allDebtors.Select(debtor => debtor.SubjectName).Distinct().OrderBy(name => name));
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить должников: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CreateNotification()
    {
        if (!CanCreateNotification)
        {
            ResultMessage = "Куратор просматривает задолженности, а уведомления создают преподаватель или администратор.";
            return;
        }

        if (SelectedDebtor is null)
        {
            ResultMessage = "Сначала выберите строку с должником.";
            return;
        }

        var curatorUserId = notificationRepository.GetCuratorUserIdForGroup(SelectedDebtor.GroupId);
        if (curatorUserId is null)
        {
            ResultMessage = $"Для группы {SelectedDebtor.GroupName} не найден куратор.";
            return;
        }

        try
        {
            var title = $"Задолженность: {SelectedDebtor.StudentName}";
            var message = BuildNotificationMessage(SelectedDebtor);

            notificationRepository.CreateNotification(new CuratorNotification(
                0,
                curatorUserId.Value,
                SelectedDebtor.StudentId,
                SelectedDebtor.GroupId,
                SelectedDebtor.AssignmentId,
                title,
                message,
                "Новое",
                string.Empty,
                null));

            ResultMessage = $"Уведомление куратору группы {SelectedDebtor.GroupName} создано.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось создать уведомление: {ex.Message}";
        }
    }

    private List<DebtorItem> LoadDebtorsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => gradeRepository.GetDebtorsForTeacher(MinPositiveGrade, currentUser.UserId),
            "Куратор группы" => gradeRepository.GetDebtorsForCurator(MinPositiveGrade, currentUser.UserId),
            _ => gradeRepository.GetDebtors(MinPositiveGrade)
        };
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedGroupFilter = null;
        SelectedSubjectFilter = null;
        SearchText = string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<DebtorItem> filtered = allDebtors;

        if (!string.IsNullOrWhiteSpace(SelectedGroupFilter))
        {
            filtered = filtered.Where(debtor => debtor.GroupName == SelectedGroupFilter);
        }

        if (!string.IsNullOrWhiteSpace(SelectedSubjectFilter))
        {
            filtered = filtered.Where(debtor => debtor.SubjectName == SelectedSubjectFilter);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(debtor =>
                debtor.StudentName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                debtor.GroupName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                debtor.SubjectName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
        }

        var visibleDebtors = filtered.ToList();
        Debtors = new ObservableCollection<DebtorItem>(visibleDebtors);
        UpdateSummary(visibleDebtors);
    }

    private void UpdateSummary(IReadOnlyCollection<DebtorItem> visibleDebtors)
    {
        VisibleDebtCount = visibleDebtors.Count;
        UniqueStudentCount = visibleDebtors.Select(debtor => debtor.StudentId).Distinct().Count();
        AffectedGroupCount = visibleDebtors.Select(debtor => debtor.GroupId).Distinct().Count();
        WorstGradeText = visibleDebtors.Count == 0
            ? "Нет данных"
            : visibleDebtors.Min(debtor => debtor.GradeValue).ToString("F1");
        ResultMessage = visibleDebtors.Count == 0
            ? "По выбранным фильтрам задолженностей нет."
            : $"Показано задолженностей ниже {MinPositiveGrade}: {VisibleDebtCount}. Студентов: {UniqueStudentCount}.";
    }

    private static string BuildNotificationMessage(DebtorItem debtor)
    {
        var comment = string.IsNullOrWhiteSpace(debtor.Comment)
            ? "Комментарий к оценке не указан."
            : $"Комментарий: {debtor.Comment}";

        return
            $"{debtor.StudentName}, группа {debtor.GroupName}: оценка {debtor.GradeValue} " +
            $"по предмету \"{debtor.SubjectName}\" от {debtor.GradeDate}. " +
            $"Преподаватель: {debtor.TeacherName}. {comment}";
    }
}
