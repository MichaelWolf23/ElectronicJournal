using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Repositories;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.ViewModels;

public partial class GradesPageViewModel : PageViewModelBase
{
    private readonly GradeRepository gradeRepository;
    private readonly StudentRepository studentRepository;
    private readonly GroupRepository groupRepository;
    private readonly SubjectRepository subjectRepository;
    private readonly GradeTypeRepository gradeTypeRepository;
    private readonly AssignmentRepository assignmentRepository;
    private readonly LessonRepository lessonRepository;
    private readonly SettingsRepository settingsRepository;
    private List<GradeJournalItem> allGrades = new();

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> grades = new();

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private ObservableCollection<Subject> subjects = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> gradeTypes = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> lessons = new();

    [ObservableProperty]
    private GradeJournalItem? selectedGrade;

    [ObservableProperty]
    private Group? selectedGroupFilter;

    [ObservableProperty]
    private Subject? selectedSubjectFilter;

    [ObservableProperty]
    private LookupItem? selectedStudentFilter;

    [ObservableProperty]
    private int selectedStudentId;

    [ObservableProperty]
    private int selectedAssignmentId;

    [ObservableProperty]
    private int selectedGradeTypeId;

    [ObservableProperty]
    private int? selectedLessonId;

    [ObservableProperty]
    private double gradeValue = 5;

    [ObservableProperty]
    private string gradeDate = DateTime.Today.ToString("yyyy-MM-dd");

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string averageResult = "Выберите студента и назначение для расчета.";

    [ObservableProperty]
    private string editResult = "Выберите оценку в таблице для редактирования.";

    public GradesPageViewModel(
        GradeRepository gradeRepository,
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        SubjectRepository subjectRepository,
        GradeTypeRepository gradeTypeRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        SettingsRepository settingsRepository)
        : base("Оценки")
    {
        this.gradeRepository = gradeRepository;
        this.studentRepository = studentRepository;
        this.groupRepository = groupRepository;
        this.subjectRepository = subjectRepository;
        this.gradeTypeRepository = gradeTypeRepository;
        this.assignmentRepository = assignmentRepository;
        this.lessonRepository = lessonRepository;
        this.settingsRepository = settingsRepository;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(Subject? value) => ApplyFilters();

    partial void OnSelectedStudentFilterChanged(LookupItem? value) => ApplyFilters();

    partial void OnSelectedGradeChanged(GradeJournalItem? value)
    {
        if (value is null)
        {
            EditResult = "Выберите оценку в таблице для редактирования.";
            return;
        }

        GradeValue = value.GradeValue;
        GradeDate = value.GradeDate;
        Comment = value.Comment ?? string.Empty;
        EditResult = $"Выбрана оценка: {value.StudentName}, {value.SubjectName}.";
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
            Students = new ObservableCollection<LookupItem>(studentRepository.GetStudentLookups());
            Assignments = new ObservableCollection<LookupItem>(assignmentRepository.GetAssignmentLookups());
            GradeTypes = new ObservableCollection<LookupItem>(gradeTypeRepository.GetGradeTypeLookups());
            Lessons = new ObservableCollection<LookupItem>(lessonRepository.GetLessonLookups());
            allGrades = gradeRepository.GetJournal();

            SelectedStudentId = Students.FirstOrDefault()?.Id ?? 0;
            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedGradeTypeId = GradeTypes.FirstOrDefault()?.Id ?? 0;
            SelectedLessonId = Lessons.FirstOrDefault()?.Id;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить журнал оценок: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddGrade()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0 || SelectedGradeTypeId == 0)
        {
            ErrorMessage = "Выберите студента, назначение и тип оценки.";
            return;
        }

        if (!IsGradeInScale(GradeValue, out var gradeError))
        {
            ErrorMessage = gradeError;
            return;
        }

        if (!IsDateValid(GradeDate, out var dateError))
        {
            ErrorMessage = dateError;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var grade = new Grade(
                0,
                SelectedStudentId,
                SelectedAssignmentId,
                SelectedLessonId,
                SelectedGradeTypeId,
                GradeValue,
                NormalizeDate(GradeDate),
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim(),
                1,
                string.Empty,
                null);

            gradeRepository.AddGrade(grade);
            Comment = string.Empty;
            allGrades = gradeRepository.GetJournal();
            ApplyFilters();
            CalculateAverage();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось добавить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void UpdateSelectedGrade()
    {
        if (SelectedGrade is null)
        {
            EditResult = "Сначала выберите оценку в таблице.";
            return;
        }

        if (!IsGradeInScale(GradeValue, out var gradeError))
        {
            EditResult = gradeError;
            return;
        }

        try
        {
            gradeRepository.UpdateGrade(
                SelectedGrade.GradeId,
                GradeValue,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());

            allGrades = gradeRepository.GetJournal();
            ApplyFilters();
            EditResult = "Оценка обновлена.";
        }
        catch (Exception ex)
        {
            EditResult = $"Не удалось обновить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private void CalculateAverage()
    {
        if (SelectedStudentId == 0 || SelectedAssignmentId == 0)
        {
            AverageResult = "Выберите студента и назначение для расчета.";
            return;
        }

        try
        {
            var average = gradeRepository.CalculateWeightedAverage(SelectedStudentId, SelectedAssignmentId);
            AverageResult = average is null
                ? "У выбранного студента пока нет оценок по этому назначению."
                : $"Средний балл с учетом веса: {average.Value:F2}";
        }
        catch (Exception ex)
        {
            AverageResult = $"Ошибка расчета: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SelectedGroupFilter = null;
        SelectedSubjectFilter = null;
        SelectedStudentFilter = null;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<GradeJournalItem> filtered = allGrades;

        if (SelectedGroupFilter is not null)
        {
            filtered = filtered.Where(grade => grade.GroupName == SelectedGroupFilter.GroupName);
        }

        if (SelectedSubjectFilter is not null)
        {
            filtered = filtered.Where(grade => grade.SubjectName == SelectedSubjectFilter.SubjectName);
        }

        if (SelectedStudentFilter is not null)
        {
            filtered = filtered.Where(grade => grade.StudentName == SelectedStudentFilter.Name);
        }

        Grades = new ObservableCollection<GradeJournalItem>(filtered);
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

    private static bool IsDateValid(string value, out string error)
    {
        if (string.IsNullOrWhiteSpace(value) || DateTime.TryParse(value, out _))
        {
            error = string.Empty;
            return true;
        }

        error = "Дата оценки должна быть в понятном формате, например 2026-02-10.";
        return false;
    }

    private static string NormalizeDate(string value) =>
        string.IsNullOrWhiteSpace(value) ? DateTime.Today.ToString("yyyy-MM-dd") : value.Trim();
}
