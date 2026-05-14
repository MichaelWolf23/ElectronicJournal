using CommunityToolkit.Mvvm.ComponentModel;

namespace ElectronicJournal.Models.Dto;

public sealed partial class GradeEntryRow : ObservableObject
{
    public GradeEntryRow(
        int studentId,
        string studentName,
        string groupName,
        string? studentCardNumber,
        int? gradeId,
        double? gradeValue,
        string? comment)
    {
        StudentId = studentId;
        StudentName = studentName;
        GroupName = groupName;
        StudentCardNumber = studentCardNumber;
        this.gradeId = gradeId;
        gradeValueText = gradeValue?.ToString("0.##") ?? string.Empty;
        this.comment = comment ?? string.Empty;
    }

    public int StudentId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    public string? StudentCardNumber { get; }

    public string StudentInfo => string.IsNullOrWhiteSpace(StudentCardNumber)
        ? GroupName
        : $"{GroupName}, билет {StudentCardNumber}";

    public string StatusText => GradeId is null ? "нет оценки" : "сохранена";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private int? gradeId;

    [ObservableProperty]
    private string gradeValueText;

    [ObservableProperty]
    private string comment;
}
