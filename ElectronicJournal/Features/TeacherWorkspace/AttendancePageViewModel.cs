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

public partial class AttendancePageViewModel : PageViewModelBase
{
    private readonly AttendanceRepository attendanceRepository;
    private readonly LessonRepository lessonRepository;
    private readonly StudentRepository studentRepository;
    private readonly GroupRepository groupRepository;
    private readonly SubjectRepository subjectRepository;
    private readonly AuthenticatedUser currentUser;
    private List<AttendanceJournalItem> allAttendance = new();

    [ObservableProperty]
    private ObservableCollection<AttendanceJournalItem> attendanceItems = new();

    [ObservableProperty]
    private AttendanceJournalItem? selectedAttendanceItem;

    [ObservableProperty]
    private ObservableCollection<LookupItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> students = new();

    [ObservableProperty]
    private ObservableCollection<AttendanceMarkItem> lessonMarks = new();

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private ObservableCollection<Subject> subjects = new();

    [ObservableProperty]
    private Group? selectedGroupFilter;

    [ObservableProperty]
    private Subject? selectedSubjectFilter;

    [ObservableProperty]
    private string dateFilter = string.Empty;

    [ObservableProperty]
    private int selectedLessonId;

    [ObservableProperty]
    private int selectedStudentId;

    [ObservableProperty]
    private string selectedStatus = AttendanceStatuses[0];

    [ObservableProperty]
    private string bulkStatus = AttendanceStatuses[0];

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Выберите занятие и студента.";

    [ObservableProperty]
    private int visibleAttendanceCount;

    [ObservableProperty]
    private int presentCount;

    [ObservableProperty]
    private int absentCount;

    [ObservableProperty]
    private int lateCount;

    [ObservableProperty]
    private string attendanceSummary = "Журнал посещаемости загружается.";

    [ObservableProperty]
    private string selectedAttendanceTitle = "Выберите запись";

    [ObservableProperty]
    private string selectedAttendanceDetails = "После выбора строки здесь появятся детали посещаемости.";

    [ObservableProperty]
    private string selectedAttendanceComment = "Комментарий не выбран.";

    [ObservableProperty]
    private int lessonStudentCount;

    [ObservableProperty]
    private int lessonMarkedCount;

    public static IReadOnlyList<string> AttendanceStatuses { get; } =
    [
        "Присутствовал",
        "Отсутствовал",
        "Опоздал",
        "Уважительная причина"
    ];

    public AttendancePageViewModel(
        AttendanceRepository attendanceRepository,
        LessonRepository lessonRepository,
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        SubjectRepository subjectRepository,
        AuthenticatedUser currentUser)
        : base("Посещаемость")
    {
        this.attendanceRepository = attendanceRepository;
        this.lessonRepository = lessonRepository;
        this.studentRepository = studentRepository;
        this.groupRepository = groupRepository;
        this.subjectRepository = subjectRepository;
        this.currentUser = currentUser;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(Subject? value) => ApplyFilters();

    partial void OnDateFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedLessonIdChanged(int value) => RefreshLessonAttendance();

    partial void OnSelectedAttendanceItemChanged(AttendanceJournalItem? value)
    {
        if (value is null)
        {
            SelectedAttendanceTitle = "Выберите запись";
            SelectedAttendanceDetails = "После выбора строки здесь появятся детали посещаемости.";
            SelectedAttendanceComment = "Комментарий не выбран.";
            return;
        }

        SelectedAttendanceTitle = $"{value.StudentName} - {value.Status}";
        SelectedAttendanceDetails =
            $"{value.LessonDate}, {value.GroupName}, {value.SubjectName}. Тема: {value.Topic}.";
        SelectedAttendanceComment = string.IsNullOrWhiteSpace(value.Comment)
            ? "Комментарий не указан."
            : value.Comment;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            allAttendance = LoadAttendanceForCurrentUser();
            Lessons = new ObservableCollection<LookupItem>(LoadLessonLookupsForCurrentUser());
            Groups = new ObservableCollection<Group>(LoadGroupsForCurrentUser());
            Subjects = new ObservableCollection<Subject>(LoadSubjectsForCurrentUser());

            SelectedLessonId = Lessons.FirstOrDefault()?.Id ?? 0;
            RefreshLessonAttendance();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить посещаемость: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAttendance()
    {
        if (SelectedAttendanceItem is null)
        {
            ResultMessage = "Сначала выберите запись посещаемости.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить посещаемость",
            $"Удалить отметку посещаемости: {SelectedAttendanceItem.StudentName}, {SelectedAttendanceItem.LessonDate}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            attendanceRepository.DeleteAttendance(SelectedAttendanceItem.AttendanceId);
            SelectedAttendanceItem = null;
            allAttendance = LoadAttendanceForCurrentUser();
            RefreshLessonAttendance();
            ApplyFilters();
            ResultMessage = "Отметка посещаемости удалена.";
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось удалить отметку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void SaveAttendance()
    {
        if (SelectedLessonId == 0 || SelectedStudentId == 0)
        {
            ResultMessage = "Выберите занятие и студента.";
            return;
        }

        try
        {
            if (!CanUseSelectedAttendanceScope(out var scopeError))
            {
                ResultMessage = scopeError;
                return;
            }

            attendanceRepository.UpsertAttendance(
                SelectedLessonId,
                SelectedStudentId,
                SelectedStatus,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());

            ResultMessage = "Посещаемость сохранена.";
            Comment = string.Empty;
            allAttendance = LoadAttendanceForCurrentUser();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить посещаемость: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void MarkAllPresent()
    {
        if (LessonMarks.Count == 0)
        {
            ResultMessage = "В выбранном занятии нет студентов.";
            return;
        }

        foreach (var mark in LessonMarks)
        {
            mark.Status = "Присутствовал";
            mark.Comment = string.Empty;
        }

        SaveLessonAttendance();
    }

    [RelayCommand]
    private void ApplyStatusToAll()
    {
        if (LessonMarks.Count == 0)
        {
            ResultMessage = "В выбранном занятии нет студентов.";
            return;
        }

        foreach (var mark in LessonMarks)
        {
            mark.Status = BulkStatus;
        }

        ResultMessage = $"В таблице всем студентам выбран статус \"{BulkStatus}\". Нажмите \"Сохранить журнал\".";
    }

    [RelayCommand]
    private void SaveLessonAttendance()
    {
        if (SelectedLessonId == 0)
        {
            ResultMessage = "Выберите занятие.";
            return;
        }

        if (LessonMarks.Count == 0)
        {
            ResultMessage = "В выбранном занятии нет студентов.";
            return;
        }

        try
        {
            if (currentUser.RoleName == "Преподаватель" &&
                !attendanceRepository.CanTeacherAccessLesson(SelectedLessonId, currentUser.UserId))
            {
                ResultMessage = "Преподаватель может отмечать посещаемость только на своих занятиях.";
                return;
            }

            foreach (var mark in LessonMarks)
            {
                if (!AttendanceStatuses.Contains(mark.Status))
                {
                    ResultMessage = $"У студента {mark.StudentName} выбран некорректный статус.";
                    return;
                }

                if (!attendanceRepository.CanStudentAttendLesson(SelectedLessonId, mark.StudentId))
                {
                    ResultMessage = $"Студент {mark.StudentName} не относится к группе выбранного занятия.";
                    return;
                }

                attendanceRepository.UpsertAttendance(
                    SelectedLessonId,
                    mark.StudentId,
                    mark.Status,
                    string.IsNullOrWhiteSpace(mark.Comment) ? null : mark.Comment.Trim());
            }

            ResultMessage = $"Журнал занятия сохранен. В таблице обновлено студентов: {LessonMarks.Count}.";
            allAttendance = LoadAttendanceForCurrentUser();
            RefreshLessonAttendance();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить журнал занятия: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedGroupFilter = null;
        SelectedSubjectFilter = null;
        DateFilter = string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<AttendanceJournalItem> filtered = allAttendance;

        if (SelectedGroupFilter is not null)
        {
            filtered = filtered.Where(item => item.GroupName == SelectedGroupFilter.GroupName);
        }

        if (SelectedSubjectFilter is not null)
        {
            filtered = filtered.Where(item => item.SubjectName == SelectedSubjectFilter.SubjectName);
        }

        if (!string.IsNullOrWhiteSpace(DateFilter))
        {
            filtered = filtered.Where(item => item.LessonDate.Contains(DateFilter, StringComparison.OrdinalIgnoreCase));
        }

        var visibleItems = filtered.ToList();
        AttendanceItems = new ObservableCollection<AttendanceJournalItem>(visibleItems);
        UpdateSummary(visibleItems);
    }

    private List<AttendanceJournalItem> LoadAttendanceForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? attendanceRepository.GetAttendanceJournalForTeacher(currentUser.UserId)
            : attendanceRepository.GetAttendanceJournal();
    }

    private List<LookupItem> LoadLessonLookupsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? lessonRepository.GetLessonLookupsForTeacher(currentUser.UserId)
            : lessonRepository.GetLessonLookups();
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

    private void RefreshLessonAttendance()
    {
        var currentStudentId = SelectedStudentId;
        var lessonStudents = SelectedLessonId > 0
            ? studentRepository.GetStudentLookupsForLesson(SelectedLessonId)
            : LoadStudentLookupsForCurrentUser();

        Students = new ObservableCollection<LookupItem>(lessonStudents);
        SelectedStudentId = Students.Any(student => student.Id == currentStudentId)
            ? currentStudentId
            : Students.FirstOrDefault()?.Id ?? 0;

        LessonMarks = SelectedLessonId > 0
            ? new ObservableCollection<AttendanceMarkItem>(attendanceRepository.GetLessonAttendanceMarks(SelectedLessonId))
            : new ObservableCollection<AttendanceMarkItem>();
        LessonStudentCount = LessonMarks.Count;
        LessonMarkedCount = LessonMarks.Count(mark => mark.AttendanceId is not null);
    }

    private List<Group> LoadGroupsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => groupRepository.GetGroupsForTeacher(currentUser.UserId),
            "Куратор группы" => groupRepository.GetGroupsForCurator(currentUser.UserId),
            _ => groupRepository.GetAll()
        };
    }

    private List<Subject> LoadSubjectsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => subjectRepository.GetSubjectsForTeacher(currentUser.UserId),
            "Куратор группы" => subjectRepository.GetSubjectsForCurator(currentUser.UserId),
            _ => subjectRepository.GetAll()
        };
    }

    private bool CanUseSelectedAttendanceScope(out string error)
    {
        if (!Lessons.Any(lesson => lesson.Id == SelectedLessonId))
        {
            error = "Выбранное занятие недоступно текущему пользователю.";
            return false;
        }

        if (!Students.Any(student => student.Id == SelectedStudentId))
        {
            error = "Выбранный студент недоступен текущему пользователю.";
            return false;
        }

        if (currentUser.RoleName == "Преподаватель" &&
            !attendanceRepository.CanTeacherAccessLesson(SelectedLessonId, currentUser.UserId))
        {
            error = "Преподаватель может отмечать посещаемость только на своих занятиях.";
            return false;
        }

        if (!attendanceRepository.CanStudentAttendLesson(SelectedLessonId, SelectedStudentId))
        {
            error = "Студент не относится к группе выбранного занятия.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private void UpdateSummary(IReadOnlyCollection<AttendanceJournalItem> visibleItems)
    {
        VisibleAttendanceCount = visibleItems.Count;
        PresentCount = visibleItems.Count(item => item.Status == "Присутствовал");
        AbsentCount = visibleItems.Count(item => item.Status == "Отсутствовал");
        LateCount = visibleItems.Count(item => item.Status == "Опоздал");
        AttendanceSummary = visibleItems.Count == 0
            ? "По выбранным фильтрам записей посещаемости нет."
            : $"Показано записей: {VisibleAttendanceCount}. Присутствовали: {PresentCount}. Отсутствовали: {AbsentCount}.";
    }
}
