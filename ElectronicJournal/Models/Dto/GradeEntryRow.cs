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
        originalGradeValueText = gradeValueText;
        originalComment = this.comment;
    }

    private readonly string originalGradeValueText;

    private readonly string originalComment;

    public int StudentId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    public string? StudentCardNumber { get; }

    public string StudentInfo => string.IsNullOrWhiteSpace(StudentCardNumber)
        ? GroupName
        : $"{GroupName}, билет {StudentCardNumber}";

    public bool HasSavedGrade => GradeId is not null;

    public bool IsDirty =>
        GradeValueText.Trim() != originalGradeValueText ||
        Comment.Trim() != originalComment;

    public string StatusText
    {
        get
        {
            if (IsDirty)
            {
                return HasSavedGrade ? "изменено" : "готово к сохранению";
            }

            return HasSavedGrade ? "сохранена" : "не сохранена";
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(HasSavedGrade))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private int? gradeId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string gradeValueText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    private string comment;
}
