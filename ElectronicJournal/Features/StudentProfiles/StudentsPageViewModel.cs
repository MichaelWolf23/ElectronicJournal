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

public partial class StudentsPageViewModel : PageViewModelBase
{
    private readonly StudentRepository studentRepository;
    private readonly GroupRepository groupRepository;
    private readonly AuthenticatedUser currentUser;
    private List<StudentListItem> allStudents = new();

    [ObservableProperty]
    private ObservableCollection<StudentListItem> students = new();

    [ObservableProperty]
    private ObservableCollection<Group> groups = new();

    [ObservableProperty]
    private Group? selectedGroupFilter;

    [ObservableProperty]
    private StudentListItem? selectedStudent;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int selectedGroupId;

    [ObservableProperty]
    private string fullName = string.Empty;

    [ObservableProperty]
    private string studentCardNumber = string.Empty;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string phone = string.Empty;

    [ObservableProperty]
    private string status = StudentStatuses[0];

    [ObservableProperty]
    private string formTitle = "Добавление студента";

    [ObservableProperty]
    private int visibleStudentCount;

    [ObservableProperty]
    private int activeStudentCount;

    [ObservableProperty]
    private int problemStatusCount;

    [ObservableProperty]
    private string rosterSummary = "Список студентов загружается.";

    [ObservableProperty]
    private string selectedStudentName = "Выберите студента";

    [ObservableProperty]
    private string selectedStudentGroupText = "Карточка студента появится после выбора строки.";

    [ObservableProperty]
    private string selectedStudentContacts = "Контакты не выбраны";

    [ObservableProperty]
    private string selectedStudentStatus = "Нет данных";

    [ObservableProperty]
    private bool canEditStudents;

    [ObservableProperty]
    private bool isReadOnlyStudentCardVisible;

    public static IReadOnlyList<string> StudentStatuses { get; } =
    [
        "Обучается",
        "Отчислен",
        "Академический отпуск",
        "Переведен",
        "Выпустился"
    ];

    public StudentsPageViewModel(
        StudentRepository studentRepository,
        GroupRepository groupRepository,
        AuthenticatedUser currentUser)
        : base("Студенты")
    {
        this.studentRepository = studentRepository;
        this.groupRepository = groupRepository;
        this.currentUser = currentUser;
        CanEditStudents = currentUser.RoleName == "Администратор";
        IsReadOnlyStudentCardVisible = !CanEditStudents;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    public override void OnNavigatedTo()
    {
        Load();
    }

    partial void OnSelectedStudentChanged(StudentListItem? value)
    {
        if (value is null)
        {
            ResetSelectedStudentCard();
            return;
        }

        SelectedStudentName = value.FullName;
        SelectedStudentGroupText = $"{value.GroupName}, курс: {value.CourseNumber?.ToString() ?? "не указан"}";
        SelectedStudentContacts = BuildContacts(value);
        SelectedStudentStatus = value.Status;

        if (CanEditStudents)
        {
            FormTitle = "Редактирование студента";
            FullName = value.FullName;
            StudentCardNumber = value.StudentCardNumber ?? string.Empty;
            Email = value.Email ?? string.Empty;
            Phone = value.Phone ?? string.Empty;
            Status = value.Status;
            SelectedGroupId = Groups.FirstOrDefault(group => group.GroupName == value.GroupName)?.GroupId ?? 0;
        }
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Groups = new ObservableCollection<Group>(LoadGroupsForCurrentUser());
            allStudents = LoadStudentsForCurrentUser();
            ApplyFilters();

            if (SelectedGroupId == 0 && Groups.Count > 0)
            {
                SelectedGroupId = Groups[0].GroupId;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить студентов: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanEditStudents)
        {
            ErrorMessage = "Изменять данные студентов может только администратор.";
            return;
        }

        if (SelectedGroupId == 0)
        {
            ErrorMessage = "Выберите группу студента.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            ErrorMessage = "Введите ФИО студента.";
            return;
        }

        if (!InputValidator.IsEmailValid(Email))
        {
            ErrorMessage = "Email указан некорректно.";
            return;
        }

        if (!InputValidator.IsPhoneValid(Phone))
        {
            ErrorMessage = "Телефон должен содержать от 10 до 15 цифр.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var student = new Student(
                SelectedStudent?.StudentId ?? 0,
                SelectedGroupId,
                FullName.Trim(),
                NullIfWhiteSpace(StudentCardNumber),
                NullIfWhiteSpace(Email),
                NullIfWhiteSpace(Phone),
                Status,
                string.Empty);

            if (SelectedStudent is null)
            {
                studentRepository.AddStudent(student);
                NotifySuccess("Студент добавлен.");
            }
            else
            {
                studentRepository.UpdateStudent(student);
                NotifySuccess("Карточка студента обновлена.");
            }

            ClearForm();
            Load();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось сохранить студента: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedStudent = null;
        ResetSelectedStudentCard();
        FormTitle = "Добавление студента";
        FullName = string.Empty;
        StudentCardNumber = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Status = StudentStatuses[0];
        SelectedGroupId = Groups.FirstOrDefault()?.GroupId ?? 0;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task DeleteSelectedStudent()
    {
        if (!CanEditStudents)
        {
            ErrorMessage = "Удалять студентов может только администратор.";
            return;
        }

        if (SelectedStudent is null)
        {
            ErrorMessage = "Сначала выберите студента.";
            return;
        }

        var confirmed = await ConfirmationDialogService.ConfirmAsync(
            "Удалить студента",
            $"Удалить студента {SelectedStudent.FullName}? Будут удалены его оценки, посещаемость, итоговые оценки и уведомления.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            IsBusy = true;
            studentRepository.DeleteStudent(SelectedStudent.StudentId);
            ClearForm();
            Load();
            ErrorMessage = null;
            NotifySuccess("Студент удален.");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось удалить студента: {UserMessageHelper.ToFriendlyDatabaseError(ex)}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<StudentListItem> filtered = allStudents;

        if (SelectedGroupFilter is not null)
        {
            filtered = filtered.Where(student => student.GroupName == SelectedGroupFilter.GroupName);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(student =>
                student.FullName.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
        }

        var visibleStudents = filtered.ToList();
        Students = new ObservableCollection<StudentListItem>(visibleStudents);
        UpdateSummary(visibleStudents);
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private List<Group> LoadGroupsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => groupRepository.GetGroupsForTeacher(currentUser.UserId),
            "Куратор группы" => groupRepository.GetGroupsForCurator(currentUser.UserId),
            _ => groupRepository.GetAll()
        };
    }

    private List<StudentListItem> LoadStudentsForCurrentUser()
    {
        return currentUser.RoleName switch
        {
            "Преподаватель" => studentRepository.GetStudentsForTeacher(currentUser.UserId),
            "Куратор группы" => studentRepository.GetStudentsForCurator(currentUser.UserId),
            _ => studentRepository.GetStudents()
        };
    }

    private void UpdateSummary(IReadOnlyCollection<StudentListItem> visibleStudents)
    {
        VisibleStudentCount = visibleStudents.Count;
        ActiveStudentCount = visibleStudents.Count(student => student.Status == "Обучается");
        ProblemStatusCount = visibleStudents.Count(student => student.Status != "Обучается");
        RosterSummary = visibleStudents.Count == 0
            ? "По выбранным фильтрам студентов нет."
            : $"Показано студентов: {VisibleStudentCount}. Обучаются: {ActiveStudentCount}. Требуют внимания: {ProblemStatusCount}.";
    }

    private void ResetSelectedStudentCard()
    {
        SelectedStudentName = "Выберите студента";
        SelectedStudentGroupText = "Карточка студента появится после выбора строки.";
        SelectedStudentContacts = "Контакты не выбраны";
        SelectedStudentStatus = "Нет данных";
    }

    private static string BuildContacts(StudentListItem student)
    {
        var email = string.IsNullOrWhiteSpace(student.Email) ? "email не указан" : student.Email;
        var phone = string.IsNullOrWhiteSpace(student.Phone) ? "телефон не указан" : student.Phone;
        var card = string.IsNullOrWhiteSpace(student.StudentCardNumber)
            ? "студенческий не указан"
            : $"студенческий: {student.StudentCardNumber}";

        return $"{email}; {phone}; {card}";
    }
}
