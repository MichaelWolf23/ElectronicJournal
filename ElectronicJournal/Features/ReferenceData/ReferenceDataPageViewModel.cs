using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public partial class ReferenceDataPageViewModel : PageViewModelBase
{
    private readonly GroupRepository groupRepository;
    private readonly SubjectRepository subjectRepository;
    private readonly GradeTypeRepository gradeTypeRepository;
    private readonly LessonRepository lessonRepository;
    private readonly AssignmentRepository assignmentRepository;

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private ObservableCollection<Subject> subjects = new();

    [ObservableProperty]
    private ObservableCollection<GradeType> gradeTypes = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> classrooms = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<ScheduleItem> schedule = new();

    [ObservableProperty]
    private string resultMessage = "Справочники готовы к просмотру.";

    [ObservableProperty]
    private int groupsCount;

    [ObservableProperty]
    private int subjectsCount;

    [ObservableProperty]
    private int gradeTypesCount;

    [ObservableProperty]
    private int classroomsCount;

    [ObservableProperty]
    private int assignmentsCount;

    [ObservableProperty]
    private int scheduleCount;

    public ReferenceDataPageViewModel(
        GroupRepository groupRepository,
        SubjectRepository subjectRepository,
        GradeTypeRepository gradeTypeRepository,
        LessonRepository lessonRepository,
        AssignmentRepository assignmentRepository)
        : base("Справочники")
    {
        this.groupRepository = groupRepository;
        this.subjectRepository = subjectRepository;
        this.gradeTypeRepository = gradeTypeRepository;
        this.lessonRepository = lessonRepository;
        this.assignmentRepository = assignmentRepository;

        Load();
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            Subjects = new ObservableCollection<Subject>(subjectRepository.GetAll());
            GradeTypes = new ObservableCollection<GradeType>(
                gradeTypeRepository.GetAll().OrderByDescending(type => type.Weight).ThenBy(type => type.TypeName));
            Classrooms = new ObservableCollection<LookupItem>(lessonRepository.GetClassroomLookups());
            Assignments = new ObservableCollection<LookupItem>(assignmentRepository.GetAssignmentLookups());
            Schedule = new ObservableCollection<ScheduleItem>(lessonRepository.GetSchedule());

            GroupsCount = Groups.Count;
            SubjectsCount = Subjects.Count;
            GradeTypesCount = GradeTypes.Count;
            ClassroomsCount = Classrooms.Count;
            AssignmentsCount = Assignments.Count;
            ScheduleCount = Schedule.Count;

            ResultMessage = $"Загружено справочников: группы, предметы, типы оценок, аудитории, назначения и расписание.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить справочники: {ex.Message}";
            ResultMessage = "Справочники недоступны.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
