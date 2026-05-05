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

public partial class StudentsPageViewModel : PageViewModelBase
{
    private readonly StudentRepository studentRepository;
    private readonly GroupRepository groupRepository;
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

    public static IReadOnlyList<string> StudentStatuses { get; } =
    [
        "Обучается",
        "Отчислен",
        "Академический отпуск",
        "Переведен",
        "Выпустился"
    ];

    public StudentsPageViewModel(StudentRepository studentRepository, GroupRepository groupRepository)
        : base("Студенты")
    {
        this.studentRepository = studentRepository;
        this.groupRepository = groupRepository;

        Load();
    }

    partial void OnSelectedGroupFilterChanged(Group? value) => ApplyFilters();

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedStudentChanged(StudentListItem? value)
    {
        if (value is null)
        {
            return;
        }

        FormTitle = "Редактирование студента";
        FullName = value.FullName;
        StudentCardNumber = value.StudentCardNumber ?? string.Empty;
        Email = value.Email ?? string.Empty;
        Phone = value.Phone ?? string.Empty;
        Status = value.Status;
        SelectedGroupId = Groups.FirstOrDefault(group => group.GroupName == value.GroupName)?.GroupId ?? 0;
    }

    [RelayCommand]
    private void Load()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            Groups = new ObservableCollection<Group>(groupRepository.GetAll());
            allStudents = studentRepository.GetStudents();
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
            }
            else
            {
                studentRepository.UpdateStudent(student);
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
        FormTitle = "Добавление студента";
        FullName = string.Empty;
        StudentCardNumber = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Status = StudentStatuses[0];
        SelectedGroupId = Groups.FirstOrDefault()?.GroupId ?? 0;
        ErrorMessage = null;
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

        Students = new ObservableCollection<StudentListItem>(filtered);
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
