using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class GroupsPageViewModel : PageViewModelBase
{
    private readonly GroupRepository groupRepository;

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private Group? selectedGroup;

    [ObservableProperty]
    private string groupName = string.Empty;

    [ObservableProperty]
    private string courseNumber = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string formTitle = "Новая группа";

    [ObservableProperty]
    private string resultMessage = "Выберите группу или создайте новую.";

    [ObservableProperty]
    private int groupCount;

    [ObservableProperty]
    private int firstCourseCount;

    [ObservableProperty]
    private int seniorCourseCount;

    public GroupsPageViewModel(GroupRepository groupRepository)
        : base("Группы")
    {
        this.groupRepository = groupRepository;
        Load();
    }

    partial void OnSelectedGroupChanged(Group? value)
    {
        if (value is null)
        {
            return;
        }

        FormTitle = "Редактирование группы";
        GroupName = value.GroupName;
        CourseNumber = value.CourseNumber?.ToString() ?? string.Empty;
        Description = value.Description ?? string.Empty;
        ResultMessage = $"Выбрана группа: {value.GroupName}.";
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            UpdateCounters();
            SelectedGroup ??= Groups.FirstOrDefault();
            ResultMessage = $"Загружено групп: {GroupCount}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить группы: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(GroupName))
        {
            ResultMessage = "Введите название группы.";
            return;
        }

        int? parsedCourseNumber = null;
        if (!string.IsNullOrWhiteSpace(CourseNumber))
        {
            if (!int.TryParse(CourseNumber, out var course) || course < 1 || course > 6)
            {
                ResultMessage = "Курс должен быть числом от 1 до 6.";
                return;
            }

            parsedCourseNumber = course;
        }

        try
        {
            var group = new Group(
                SelectedGroup?.GroupId ?? 0,
                GroupName.Trim(),
                parsedCourseNumber,
                NullIfWhiteSpace(Description));

            if (SelectedGroup is null)
            {
                groupRepository.AddGroup(group);
                ResultMessage = "Группа создана.";
            }
            else
            {
                groupRepository.UpdateGroup(group);
                ResultMessage = "Группа обновлена.";
            }

            var selectedName = group.GroupName;
            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            UpdateCounters();
            SelectedGroup = Groups.FirstOrDefault(item => item.GroupName == selectedName);
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить группу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedGroup = null;
        FormTitle = "Новая группа";
        GroupName = string.Empty;
        CourseNumber = string.Empty;
        Description = string.Empty;
        ResultMessage = "Заполните данные новой группы.";
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task DeleteSelectedGroup()
    {
        if (SelectedGroup is null)
        {
            ResultMessage = "Сначала выберите группу.";
            return;
        }

        var blockingReferences = groupRepository.CountBlockingGroupReferences(SelectedGroup.GroupId);
        if (blockingReferences > 0)
        {
            ResultMessage = "Группу нельзя удалить: в ней есть студенты или назначенные предметы.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить группу",
            $"Удалить группу {SelectedGroup.GroupName}? Назначения кураторов и уведомления по этой группе тоже будут удалены.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            groupRepository.DeleteGroup(SelectedGroup.GroupId);
            ClearForm();
            Load();
            ResultMessage = "Группа удалена.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить группу: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    private void UpdateCounters()
    {
        GroupCount = Groups.Count;
        FirstCourseCount = Groups.Count(group => group.CourseNumber == 1);
        SeniorCourseCount = Groups.Count(group => group.CourseNumber >= 3);
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
