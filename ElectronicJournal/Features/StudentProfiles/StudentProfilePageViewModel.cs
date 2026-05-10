using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Repositories;

namespace ElectronicJournal.ViewModels;

public sealed partial class StudentProfilePageViewModel : PageViewModelBase
{
    private readonly StudentRepository studentRepository;
    private readonly GradeRepository gradeRepository;
    private readonly AttendanceRepository attendanceRepository;
    private readonly SettingsRepository settingsRepository;
    private readonly AuthenticatedUser currentUser;
    private List<GradeJournalItem> allGrades = new();
    private List<AttendanceJournalItem> allAttendance = new();
    private List<DebtorItem> allDebts = new();

    [ObservableProperty]
    private ObservableCollection<StudentListItem> students = new();

    [ObservableProperty]
    private ObservableCollection<GradeJournalItem> recentGrades = new();

    [ObservableProperty]
    private ObservableCollection<AttendanceJournalItem> recentAttendance = new();

    [ObservableProperty]
    private ObservableCollection<DebtorItem> debts = new();

    [ObservableProperty]
    private StudentListItem? selectedStudent;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string profileTitle = "Выберите студента";

    [ObservableProperty]
    private string profileDetails = "Карточка объединяет контакты, оценки, посещаемость и долги.";

    [ObservableProperty]
    private string contactText = string.Empty;

    [ObservableProperty]
    private string averageText = "Нет данных";

    [ObservableProperty]
    private int lowGradeCount;

    [ObservableProperty]
    private int absenceCount;

    [ObservableProperty]
    private string resultMessage = "Выберите студента из списка.";

    public StudentProfilePageViewModel(
        StudentRepository studentRepository,
        GradeRepository gradeRepository,
        AttendanceRepository attendanceRepository,
        SettingsRepository settingsRepository,
        AuthenticatedUser currentUser)
        : base("Карточка студента")
    {
        this.studentRepository = studentRepository;
        this.gradeRepository = gradeRepository;
        this.attendanceRepository = attendanceRepository;
        this.settingsRepository = settingsRepository;
        this.currentUser = currentUser;

        Load();
    }

    public event Action<string>? NavigateRequested;

    partial void OnSelectedStudentChanged(StudentListItem? value) => UpdateProfile();

    partial void OnSearchTextChanged(string value) => ApplySearch();

    [RelayCommand]
    private void Load()
    {
        try
        {
            ErrorMessage = null;
            var loadedStudents = currentUser.RoleName switch
            {
                "Преподаватель" => studentRepository.GetStudentsForTeacher(currentUser.UserId),
                "Куратор группы" => studentRepository.GetStudentsForCurator(currentUser.UserId),
                _ => studentRepository.GetStudents()
            };
            Students = new ObservableCollection<StudentListItem>(loadedStudents);
            allGrades = currentUser.RoleName switch
            {
                "Преподаватель" => gradeRepository.GetJournalForTeacher(currentUser.UserId),
                "Куратор группы" => gradeRepository.GetJournalForCurator(currentUser.UserId),
                _ => gradeRepository.GetJournal()
            };
            allAttendance = currentUser.RoleName == "Преподаватель"
                ? attendanceRepository.GetAttendanceJournalForTeacher(currentUser.UserId)
                : attendanceRepository.GetAttendanceJournal();
            allDebts = currentUser.RoleName switch
            {
                "Преподаватель" => gradeRepository.GetDebtorsForTeacher(settingsRepository.GetMinPositiveGrade(), currentUser.UserId),
                "Куратор группы" => gradeRepository.GetDebtorsForCurator(settingsRepository.GetMinPositiveGrade(), currentUser.UserId),
                _ => gradeRepository.GetDebtors(settingsRepository.GetMinPositiveGrade())
            };
            SelectedStudent = Students.FirstOrDefault();
            ResultMessage = $"Загружено карточек: {Students.Count}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось загрузить карточки студентов: {ex.Message}";
        }
    }

    public void SelectStudentById(int studentId)
    {
        var student = Students.FirstOrDefault(item => item.StudentId == studentId);
        if (student is not null)
        {
            SelectedStudent = student;
            ResultMessage = $"Открыта карточка: {student.FullName}.";
        }
    }

    [RelayCommand]
    private void OpenRisks()
    {
        NavigateRequested?.Invoke("Студенты риска");
    }

    [RelayCommand]
    private void OpenLessonJournal()
    {
        NavigateRequested?.Invoke("Журнал занятия");
    }

    private void ApplySearch()
    {
        var source = currentUser.RoleName switch
        {
            "Преподаватель" => studentRepository.GetStudentsForTeacher(currentUser.UserId),
            "Куратор группы" => studentRepository.GetStudentsForCurator(currentUser.UserId),
            _ => studentRepository.GetStudents()
        };

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            source = source
                .Where(student => student.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || student.GroupName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Students = new ObservableCollection<StudentListItem>(source);
        SelectedStudent = Students.FirstOrDefault();
    }

    private void UpdateProfile()
    {
        if (SelectedStudent is null)
        {
            ProfileTitle = "Выберите студента";
            ProfileDetails = "Карточка объединяет контакты, оценки, посещаемость и долги.";
            ContactText = string.Empty;
            RecentGrades.Clear();
            RecentAttendance.Clear();
            Debts.Clear();
            return;
        }

        var grades = allGrades
            .Where(grade => grade.StudentName == SelectedStudent.FullName)
            .OrderByDescending(grade => grade.GradeDate)
            .ToList();
        var attendance = allAttendance
            .Where(item => item.StudentName == SelectedStudent.FullName)
            .OrderByDescending(item => item.LessonDate)
            .ToList();
        var debts = allDebts
            .Where(debt => debt.StudentId == SelectedStudent.StudentId)
            .ToList();

        ProfileTitle = SelectedStudent.FullName;
        ProfileDetails = $"{SelectedStudent.GroupName} · курс {SelectedStudent.CourseNumber?.ToString() ?? "не указан"} · {SelectedStudent.Status}";
        ContactText = $"Email: {SelectedStudent.Email ?? "не указан"} · Телефон: {SelectedStudent.Phone ?? "не указан"}";
        AverageText = grades.Count == 0 ? "Нет данных" : grades.Average(grade => grade.GradeValue).ToString("F2");
        LowGradeCount = debts.Count;
        AbsenceCount = attendance.Count(item => item.Status.Contains("отсутств", StringComparison.OrdinalIgnoreCase));
        RecentGrades = new ObservableCollection<GradeJournalItem>(grades.Take(6));
        RecentAttendance = new ObservableCollection<AttendanceJournalItem>(attendance.Take(6));
        Debts = new ObservableCollection<DebtorItem>(debts);
    }
}
