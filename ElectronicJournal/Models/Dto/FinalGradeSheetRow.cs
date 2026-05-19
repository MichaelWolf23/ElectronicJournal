using CommunityToolkit.Mvvm.ComponentModel;

namespace ElectronicJournal.Models.Dto;

public sealed partial class FinalGradeSheetRow : ObservableObject
{
    public FinalGradeSheetRow(
        int? finalGradeId,
        int studentId,
        string studentName,
        string groupName,
        double? calculatedAverage,
        double? savedFinalValue,
        string? savedComment)
    {
        FinalGradeId = finalGradeId;
        StudentId = studentId;
        StudentName = studentName;
        GroupName = groupName;
        CalculatedAverage = calculatedAverage;
        SavedFinalValue = savedFinalValue;
        finalValueText = savedFinalValue?.ToString("0.##")
            ?? (calculatedAverage is null ? string.Empty : System.Math.Round(calculatedAverage.Value, 0, System.MidpointRounding.AwayFromZero).ToString("0"));
        comment = savedComment ?? string.Empty;
        originalFinalValueText = finalValueText;
        originalComment = comment;
    }

    private readonly string originalFinalValueText;
    private readonly string originalComment;

    public int? FinalGradeId { get; }

    public int StudentId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    public double? CalculatedAverage { get; }

    public double? SavedFinalValue { get; }

    public string AverageText => CalculatedAverage?.ToString("F2") ?? "-";

    public string StatusText
    {
        get
        {
            if (IsDirty)
            {
                return HasSavedFinalGrade ? "изменено" : "готово к сохранению";
            }

            return SavedFinalValue is null ? "не сохранено" : "сохранено";
        }
    }

    public bool HasSavedFinalGrade => FinalGradeId is not null;

    [ObservableProperty]
    private string finalValueText;

    [ObservableProperty]
    private string comment;

    [ObservableProperty]
    private bool isDirty;

    partial void OnFinalValueTextChanged(string value) => UpdateDirtyState();

    partial void OnCommentChanged(string value) => UpdateDirtyState();

    private void UpdateDirtyState()
    {
        IsDirty = FinalValueText != originalFinalValueText || Comment != originalComment;
        OnPropertyChanged(nameof(StatusText));
    }
}
