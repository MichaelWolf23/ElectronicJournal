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

public partial class AttendancePageViewModel : PageViewModelBase
{
    private readonly AttendanceRepository attendanceRepository;
    private readonly LessonRepository lessonRepository;
    private readonly StudentRepository studentRepository;
    private readonly GroupRepository groupRepository;
    private readonly SubjectRepository subjectRepository;
    private List<AttendanceJournalItem> allAttendance = new();

    [ObservableProperty]
    private ObservableCollection<AttendanceJournalItem> attendanceItems = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> students = new();

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
    private string comment = string.Empty;

    [ObservableProperty]
    private string resultMessage = "Выберите занятие и студента.";

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
        SubjectRepository subjectRepository)
        : base("Посещаемость")
    {
        this.attendanceRepository = attendanceRepository;
        this.lessonRepository = lessonRepository;
        this.studentRepository = studentRepository;
        this.groupRepository = groupRepository;
        this.subjectRepository = subjectRepository;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(Subject? value) => ApplyFilters();

    partial void OnDateFilterChanged(string value) => ApplyFilters();

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            allAttendance = attendanceRepository.GetAttendanceJournal();
            Lessons = new ObservableCollection<LookupItem>(lessonRepository.GetLessonLookups());
            Students = new ObservableCollection<LookupItem>(studentRepository.GetStudentLookups());
            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            Subjects = new ObservableCollection<Subject>(subjectRepository.GetAll());

            SelectedLessonId = Lessons.FirstOrDefault()?.Id ?? 0;
            SelectedStudentId = Students.FirstOrDefault()?.Id ?? 0;
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
    private void SaveAttendance()
    {
        if (SelectedLessonId == 0 || SelectedStudentId == 0)
        {
            ResultMessage = "Выберите занятие и студента.";
            return;
        }

        try
        {
            attendanceRepository.UpsertAttendance(
                SelectedLessonId,
                SelectedStudentId,
                SelectedStatus,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());

            ResultMessage = "Посещаемость сохранена.";
            Comment = string.Empty;
            allAttendance = attendanceRepository.GetAttendanceJournal();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            ResultMessage = $"Не удалось сохранить посещаемость: {ex.Message}";
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

        AttendanceItems = new ObservableCollection<AttendanceJournalItem>(filtered);
    }
}
