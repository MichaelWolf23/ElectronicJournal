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

public partial class AssignmentsPageViewModel : PageViewModelBase
{
    private readonly AssignmentRepository assignmentRepository;
    private readonly UserRepository userRepository;
    private readonly GroupRepository groupRepository;
    private readonly SubjectRepository subjectRepository;
    private List<TeacherAssignmentItem> allTeacherAssignments = new();
    private List<GroupCuratorItem> allCuratorAssignments = new();

    [ObservableProperty]
    private ObservableCollection<TeacherAssignmentItem> teacherAssignments = new();

    [ObservableProperty]
    private ObservableCollection<GroupCuratorItem> curatorAssignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> teachers = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> curators = new();

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private ObservableCollection<Subject> subjects = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> periods = new();

    [ObservableProperty]
    private TeacherAssignmentItem? selectedTeacherAssignment;

    [ObservableProperty]
    private GroupCuratorItem? selectedCuratorAssignment;

    [ObservableProperty]
    private int selectedTeacherId;

    [ObservableProperty]
    private int selectedTeacherGroupId;

    [ObservableProperty]
    private int selectedSubjectId;

    [ObservableProperty]
    private int selectedPeriodId;

    [ObservableProperty]
    private int selectedCuratorId;

    [ObservableProperty]
    private int selectedCuratorGroupId;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int teacherAssignmentCount;

    [ObservableProperty]
    private int curatorAssignmentCount;

    [ObservableProperty]
    private string resultMessage = "Настройте, кто ведет занятия и кто курирует группы.";

    public AssignmentsPageViewModel(
        AssignmentRepository assignmentRepository,
        UserRepository userRepository,
        GroupRepository groupRepository,
        SubjectRepository subjectRepository)
        : base("Назначения")
    {
        this.assignmentRepository = assignmentRepository;
        this.userRepository = userRepository;
        this.groupRepository = groupRepository;
        this.subjectRepository = subjectRepository;

        Load();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Teachers = new ObservableCollection<LookupItem>(userRepository.GetActiveUserLookupsByRole("Преподаватель"));
            Curators = new ObservableCollection<LookupItem>(userRepository.GetActiveUserLookupsByRole("Куратор группы"));
            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            Subjects = new ObservableCollection<Subject>(subjectRepository.GetAll());
            Periods = new ObservableCollection<LookupItem>(assignmentRepository.GetPeriodLookups());
            allTeacherAssignments = assignmentRepository.GetTeacherAssignments();
            allCuratorAssignments = assignmentRepository.GetGroupCurators();

            SelectedTeacherId = Teachers.FirstOrDefault()?.Id ?? 0;
            SelectedTeacherGroupId = Groups.FirstOrDefault()?.GroupId ?? 0;
            SelectedSubjectId = Subjects.FirstOrDefault()?.SubjectId ?? 0;
            SelectedPeriodId = Periods.FirstOrDefault()?.Id ?? 0;
            SelectedCuratorId = Curators.FirstOrDefault()?.Id ?? 0;
            SelectedCuratorGroupId = Groups.FirstOrDefault()?.GroupId ?? 0;

            ApplyFilter();
            ResultMessage = "Назначения загружены.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить назначения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddTeacherAssignment()
    {
        if (SelectedTeacherId == 0 || SelectedTeacherGroupId == 0 || SelectedSubjectId == 0 || SelectedPeriodId == 0)
        {
            ResultMessage = "Выберите преподавателя, группу, предмет и период.";
            return;
        }

        try
        {
            if (assignmentRepository.TeacherAssignmentExists(
                SelectedTeacherId,
                SelectedTeacherGroupId,
                SelectedSubjectId,
                SelectedPeriodId))
            {
                ResultMessage = "Такое назначение преподавателя уже есть.";
                return;
            }

            assignmentRepository.AddTeacherAssignment(
                SelectedTeacherId,
                SelectedTeacherGroupId,
                SelectedSubjectId,
                SelectedPeriodId);
            ResultMessage = "Преподаватель назначен.";
            ReloadAssignments();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось назначить преподавателя: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private async Task DeleteTeacherAssignment()
    {
        if (SelectedTeacherAssignment is null)
        {
            ResultMessage = "Выберите назначение преподавателя.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить назначение",
            $"Удалить назначение: {SelectedTeacherAssignment.TeacherName}, {SelectedTeacherAssignment.GroupName}, {SelectedTeacherAssignment.SubjectName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            assignmentRepository.DeleteTeacherAssignment(SelectedTeacherAssignment.AssignmentId);
            SelectedTeacherAssignment = null;
            ResultMessage = "Назначение преподавателя удалено.";
            ReloadAssignments();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить назначение: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void AddCuratorAssignment()
    {
        if (SelectedCuratorId == 0 || SelectedCuratorGroupId == 0)
        {
            ResultMessage = "Выберите куратора и группу.";
            return;
        }

        try
        {
            if (assignmentRepository.CuratorAssignmentExists(SelectedCuratorGroupId, SelectedCuratorId))
            {
                ResultMessage = "Этот куратор уже назначен на выбранную группу.";
                return;
            }

            assignmentRepository.AddGroupCurator(SelectedCuratorGroupId, SelectedCuratorId);
            ResultMessage = "Куратор назначен.";
            ReloadAssignments();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось назначить куратора: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private async Task DeleteCuratorAssignment()
    {
        if (SelectedCuratorAssignment is null)
        {
            ResultMessage = "Выберите назначение куратора.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить куратора",
            $"Убрать куратора {SelectedCuratorAssignment.CuratorName} из группы {SelectedCuratorAssignment.GroupName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            assignmentRepository.DeleteGroupCurator(SelectedCuratorAssignment.GroupCuratorId);
            SelectedCuratorAssignment = null;
            ResultMessage = "Назначение куратора удалено.";
            ReloadAssignments();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить куратора: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    private void ReloadAssignments()
    {
        allTeacherAssignments = assignmentRepository.GetTeacherAssignments();
        allCuratorAssignments = assignmentRepository.GetGroupCurators();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();
        IEnumerable<TeacherAssignmentItem> filteredTeachers = allTeacherAssignments;
        IEnumerable<GroupCuratorItem> filteredCurators = allCuratorAssignments;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filteredTeachers = filteredTeachers.Where(item =>
                Contains(item.TeacherName, query) ||
                Contains(item.GroupName, query) ||
                Contains(item.SubjectName, query) ||
                Contains(item.PeriodName, query));
            filteredCurators = filteredCurators.Where(item =>
                Contains(item.CuratorName, query) ||
                Contains(item.GroupName, query));
        }

        var visibleTeachers = filteredTeachers.ToList();
        var visibleCurators = filteredCurators.ToList();
        TeacherAssignments = new ObservableCollection<TeacherAssignmentItem>(visibleTeachers);
        CuratorAssignments = new ObservableCollection<GroupCuratorItem>(visibleCurators);
        TeacherAssignmentCount = visibleTeachers.Count;
        CuratorAssignmentCount = visibleCurators.Count;
    }

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.OrdinalIgnoreCase);
}
