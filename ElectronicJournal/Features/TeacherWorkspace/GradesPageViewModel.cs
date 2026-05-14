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
    private readonly AuthenticatedUser currentUser;
    private List<GradeJournalItem> allGrades = new();

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> grades = new();

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private ObservableCollection<Subject> subjects = new();

    [ObservableProperty]
    private ObservableCollection<StudentLookupItem> students = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> assignments = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> gradeTypes = new();

    [ObservableProperty]
    private ObservableCollection<LookupItem> lessons = new();

    [ObservableProperty]
    private ObservableCollection<LessonJournalLessonItem> lessonChoices = new();

    [ObservableProperty]
    private ObservableCollection<GradeEntryRow> gradeEntryRows = new();

    [ObservableProperty]
    private LessonJournalLessonItem? selectedLessonForGrading;

    [ObservableProperty]
    private GradeJournalItem? selectedGrade;

    [ObservableProperty]
    private Group? selectedGroupFilter;

    [ObservableProperty]
    private Subject? selectedSubjectFilter;

    [ObservableProperty]
    private StudentLookupItem? selectedStudentFilter;

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

    [ObservableProperty]
    private int visibleGradeCount;

    [ObservableProperty]
    private string averageGradeText = "Нет данных";

    [ObservableProperty]
    private int lowGradeCount;

    [ObservableProperty]
    private string journalSummary = "Журнал оценок загружается.";

    [ObservableProperty]
    private string lessonGradeSummary = "Выберите занятие и тип оценки.";

    [ObservableProperty]
    private string lessonGradeResult = string.Empty;

    [ObservableProperty]
    private int lessonStudentCount;

    [ObservableProperty]
    private int filledLessonGradeCount;

    public GradesPageViewModel(
        GradeRepository gradeRepository,
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        SubjectRepository subjectRepository,
        GradeTypeRepository gradeTypeRepository,
        AssignmentRepository assignmentRepository,
        LessonRepository lessonRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
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
        this.currentUser = currentUser;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSelectedSubjectFilterChanged(Subject? value) => ApplyFilters();

    partial void OnSelectedStudentFilterChanged(StudentLookupItem? value) => ApplyFilters();

    partial void OnSelectedLessonForGradingChanged(LessonJournalLessonItem? value)
    {
        if (value is null)
        {
            SelectedLessonId = null;
            SelectedAssignmentId = 0;
            GradeEntryRows = new ObservableCollection<GradeEntryRow>();
            UpdateLessonGradeSummary();
            return;
        }

        SelectedLessonId = value.LessonId;
        SelectedAssignmentId = value.AssignmentId;
        GradeDate = value.LessonDate;
        SelectedGroupFilter = Groups.FirstOrDefault(group => group.GroupId == value.GroupId);
        SelectedSubjectFilter = Subjects.FirstOrDefault(subject => subject.SubjectName == value.SubjectName);
        LoadLessonGradeRows();
        ApplyFilters();
    }

    partial void OnSelectedGradeTypeIdChanged(int value)
    {
        LoadLessonGradeRows();
    }

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

            Groups = new ObservableCollection<Group>(LoadGroupsForCurrentUser());
            Subjects = new ObservableCollection<Subject>(LoadSubjectsForCurrentUser());
            Students = new ObservableCollection<StudentLookupItem>(LoadStudentLookupsForCurrentUser());
            Assignments = new ObservableCollection<LookupItem>(LoadAssignmentLookupsForCurrentUser());
            GradeTypes = new ObservableCollection<LookupItem>(gradeTypeRepository.GetGradeTypeLookups());
            LessonChoices = new ObservableCollection<LessonJournalLessonItem>(LoadJournalLessonsForCurrentUser());
            Lessons = new ObservableCollection<LookupItem>(LoadLessonLookupsForCurrentUser());
            allGrades = LoadJournalForCurrentUser();

            SelectedStudentId = Students.FirstOrDefault()?.Id ?? 0;
            SelectedAssignmentId = Assignments.FirstOrDefault()?.Id ?? 0;
            SelectedGradeTypeId = GradeTypes.FirstOrDefault()?.Id ?? 0;
            SelectedLessonId = Lessons.FirstOrDefault()?.Id;
            SelectedLessonForGrading = LessonChoices.FirstOrDefault();

            ApplyFilters();
            LoadLessonGradeRows();
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
    private void SaveLessonGrades()
    {
        if (SelectedLessonForGrading is null)
        {
            LessonGradeResult = "Выберите занятие.";
            return;
        }

        if (SelectedGradeTypeId == 0)
        {
            LessonGradeResult = "Выберите тип оценки.";
            return;
        }

        var rowsToSave = GradeEntryRows
            .Where(row => !string.IsNullOrWhiteSpace(row.GradeValueText))
            .ToList();
        if (rowsToSave.Count == 0)
        {
            LessonGradeResult = "Заполните хотя бы одну оценку.";
            return;
        }

        if (!CanUseSelectedGradeScope(out var scopeError))
        {
            LessonGradeResult = scopeError;
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var saved = 0;
            foreach (var row in rowsToSave)
            {
                if (!double.TryParse(row.GradeValueText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    LessonGradeResult = $"Проверьте оценку у студента {row.StudentName}.";
                    return;
                }

                if (!IsGradeInScale(value, out var gradeError))
                {
                    LessonGradeResult = $"{row.StudentName}: {gradeError}";
                    return;
                }

                gradeRepository.UpsertLessonGrade(
                    row.GradeId,
                    row.StudentId,
                    SelectedLessonForGrading.AssignmentId,
                    SelectedLessonForGrading.LessonId,
                    SelectedGradeTypeId,
                    value,
                    SelectedLessonForGrading.LessonDate,
                    string.IsNullOrWhiteSpace(row.Comment) ? null : row.Comment.Trim(),
                    currentUser.UserId);
                saved++;
            }

            allGrades = LoadJournalForCurrentUser();
            LoadLessonGradeRows();
            ApplyFilters();
            LessonGradeResult = $"Сохранено оценок: {saved}.";
        }
        catch (Exception ex)
        {
            LessonGradeResult = $"Не удалось сохранить оценки: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
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

        if (!CanUseSelectedGradeScope(out var scopeError))
        {
            ErrorMessage = scopeError;
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
            var normalizedDate = NormalizeDate(GradeDate);

            if (gradeRepository.GradeExists(
                SelectedStudentId,
                SelectedAssignmentId,
                SelectedGradeTypeId,
                normalizedDate,
                SelectedLessonId is 0 ? null : SelectedLessonId))
            {
                ErrorMessage = "Такая оценка уже есть: тот же студент, предмет, тип, дата и занятие.";
                return;
            }

            var grade = new Grade(
                0,
                SelectedStudentId,
                SelectedAssignmentId,
                SelectedLessonId is 0 ? null : SelectedLessonId,
                SelectedGradeTypeId,
                GradeValue,
                normalizedDate,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim(),
                currentUser.UserId,
                string.Empty,
                null);

            gradeRepository.AddGrade(grade);
            Comment = string.Empty;
            allGrades = LoadJournalForCurrentUser();
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
            if (!CanEditSelectedGrade(SelectedGrade.GradeId))
            {
                EditResult = "Нельзя изменить оценку, которая не относится к вашим предметам.";
                return;
            }

            gradeRepository.UpdateGrade(
                SelectedGrade.GradeId,
                GradeValue,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim());

            allGrades = LoadJournalForCurrentUser();
            ApplyFilters();
            EditResult = "Оценка обновлена.";
        }
        catch (Exception ex)
        {
            EditResult = $"Не удалось обновить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedGrade()
    {
        if (SelectedGrade is null)
        {
            EditResult = "Сначала выберите оценку в таблице.";
            return;
        }

        if (!CanEditSelectedGrade(SelectedGrade.GradeId))
        {
            EditResult = "Нельзя удалить оценку, которая не относится к вашим предметам.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить оценку",
            $"Удалить оценку {SelectedGrade.GradeValue} у студента {SelectedGrade.StudentName}?");
        if (!confirmed)
        {
            return;
        }

        try
        {
            gradeRepository.DeleteGrade(SelectedGrade.GradeId);
            SelectedGrade = null;
            allGrades = LoadJournalForCurrentUser();
            ApplyFilters();
            EditResult = "Оценка удалена.";
        }
        catch (Exception ex)
        {
            EditResult = $"Не удалось удалить оценку: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
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
            filtered = filtered.Where(grade =>
                grade.StudentName == SelectedStudentFilter.Name &&
                grade.GroupName == SelectedStudentFilter.GroupName);
        }

        var visibleGrades = filtered.ToList();
        Grades = new ObservableCollection<GradeJournalItem>(visibleGrades);
        UpdateSummary(visibleGrades);
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

    private List<StudentLookupItem> LoadStudentLookupsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => studentRepository.GetStudentLookupItemsForTeacher(currentUser.UserId),
            "Куратор группы" => studentRepository.GetStudentLookupItemsForCurator(currentUser.UserId),
            _ => studentRepository.GetStudentLookupItems()
        };
    }

    private List<LookupItem> LoadAssignmentLookupsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? assignmentRepository.GetAssignmentLookupsForTeacher(currentUser.UserId)
            : assignmentRepository.GetAssignmentLookups();
    }

    private List<LookupItem> LoadLessonLookupsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? lessonRepository.GetLessonLookupsForTeacher(currentUser.UserId)
            : lessonRepository.GetLessonLookups();
    }

    private List<LessonJournalLessonItem> LoadJournalLessonsForCurrentUser()
    {
        return currentUser.RoleName == "Преподаватель"
            ? lessonRepository.GetJournalLessonsForTeacher(currentUser.UserId)
            : lessonRepository.GetJournalLessons();
    }

    private List<GradeJournalItem> LoadJournalForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => gradeRepository.GetJournalForTeacher(currentUser.UserId),
            "Куратор группы" => gradeRepository.GetJournalForCurator(currentUser.UserId),
            _ => gradeRepository.GetJournal()
        };
    }

    private bool CanUseSelectedGradeScope(out string error)
    {
        if (SelectedLessonForGrading is not null)
        {
            if (!LessonChoices.Any(lesson => lesson.LessonId == SelectedLessonForGrading.LessonId))
            {
                error = "Выбранное занятие недоступно текущему пользователю.";
                return false;
            }

            if (currentUser.RoleName == "Преподаватель" &&
                !gradeRepository.CanTeacherUseAssignment(SelectedLessonForGrading.AssignmentId, currentUser.UserId))
            {
                error = "Преподаватель может ставить оценки только по своим предметам.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (!Students.Any(student => student.Id == SelectedStudentId))
        {
            error = "Выбранный студент недоступен текущему пользователю.";
            return false;
        }

        if (!Assignments.Any(assignment => assignment.Id == SelectedAssignmentId))
        {
            error = "Выбранное назначение недоступно текущему пользователю.";
            return false;
        }

        if (currentUser.RoleName == "Преподаватель" &&
            !gradeRepository.CanTeacherUseAssignment(SelectedAssignmentId, currentUser.UserId))
        {
            error = "Преподаватель может ставить оценки только по своим предметам.";
            return false;
        }

        if (!gradeRepository.CanStudentUseAssignment(SelectedStudentId, SelectedAssignmentId))
        {
            error = "Студент не относится к группе выбранного предмета.";
            return false;
        }

        if (SelectedLessonId is int lessonId &&
            lessonId != 0 &&
            !gradeRepository.LessonBelongsToAssignment(lessonId, SelectedAssignmentId))
        {
            error = "Выбранное занятие не относится к выбранному предмету.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool CanEditSelectedGrade(int gradeId)
    {
        return currentUser.RoleName != "Преподаватель" ||
            gradeRepository.CanTeacherAccessGrade(gradeId, currentUser.UserId);
    }

    private void UpdateSummary(IReadOnlyCollection<GradeJournalItem> visibleGrades)
    {
        VisibleGradeCount = visibleGrades.Count;
        LowGradeCount = visibleGrades.Count(grade => grade.GradeValue < settingsRepository.GetMinPositiveGrade());
        AverageGradeText = visibleGrades.Count == 0
            ? "Нет данных"
            : visibleGrades.Average(grade => grade.GradeValue).ToString("F2");
        JournalSummary = visibleGrades.Count == 0
            ? "По выбранным фильтрам оценок нет."
            : $"Показано оценок: {VisibleGradeCount}. Средний балл по списку: {AverageGradeText}.";
    }

    private void LoadLessonGradeRows()
    {
        if (SelectedLessonForGrading is null || SelectedGradeTypeId == 0)
        {
            GradeEntryRows = new ObservableCollection<GradeEntryRow>();
            UpdateLessonGradeSummary();
            return;
        }

        try
        {
            GradeEntryRows = new ObservableCollection<GradeEntryRow>(
                gradeRepository.GetGradeEntryRowsForLesson(SelectedLessonForGrading.LessonId, SelectedGradeTypeId));
            LessonGradeResult = string.Empty;
            UpdateLessonGradeSummary();
        }
        catch (Exception ex)
        {
            LessonGradeResult = $"Не удалось загрузить студентов занятия: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
    }

    private void UpdateLessonGradeSummary()
    {
        LessonStudentCount = GradeEntryRows.Count;
        FilledLessonGradeCount = GradeEntryRows.Count(row => !string.IsNullOrWhiteSpace(row.GradeValueText));
        LessonGradeSummary = SelectedLessonForGrading is null
            ? "Выберите занятие и тип оценки."
            : $"{SelectedLessonForGrading.LessonDate} · {SelectedLessonForGrading.GroupName} · {SelectedLessonForGrading.SubjectName} · {SelectedLessonForGrading.Topic}";
    }
}
