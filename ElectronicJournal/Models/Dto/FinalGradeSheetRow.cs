using CommunityToolkit.Mvvm.ComponentModel;

namespace ElectronicJournal.Models.Dto;

public sealed partial class FinalGradeSheetRow : ObservableObject
{
    public FinalGradeSheetRow(
        int studentId,
        string studentName,
        string groupName,
        double? calculatedAverage,
        double? savedFinalValue,
        string? savedComment)
    {
        StudentId = studentId;
        StudentName = studentName;
        GroupName = groupName;
        CalculatedAverage = calculatedAverage;
        SavedFinalValue = savedFinalValue;
        finalValueText = savedFinalValue?.ToString("0.##")
            ?? (calculatedAverage is null ? string.Empty : System.Math.Round(calculatedAverage.Value, 0, System.MidpointRounding.AwayFromZero).ToString("0"));
        comment = savedComment ?? string.Empty;
    }

    public int StudentId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    public double? CalculatedAverage { get; }

    public double? SavedFinalValue { get; }

    public string AverageText => CalculatedAverage?.ToString("F2") ?? "нет оценок";

    public string StatusText => SavedFinalValue is null ? "не сохранено" : "сохранено";

    [ObservableProperty]
    private string finalValueText;

    [ObservableProperty]
    private string comment;
}
