using CommunityToolkit.Mvvm.ComponentModel;

namespace ElectronicJournal.Models.Dto;

public sealed partial class RetakeEntryRow : ObservableObject
{
    public RetakeEntryRow(
        GradeJournalItem grade,
        int? retakeId,
        double? lastRetakeOldValue,
        double? lastRetakeValue,
        string? lastRetakeDate,
        string? lastRetakeReason)
    {
        GradeId = grade.GradeId;
        StudentName = grade.StudentName;
        GroupName = grade.GroupName;
        SubjectName = grade.SubjectName;
        GradeType = grade.GradeType;
        OldValue = lastRetakeOldValue ?? grade.GradeValue;
        CurrentValue = grade.GradeValue;
        GradeDate = grade.GradeDate;
        RetakeId = retakeId;
        LastRetakeValue = lastRetakeValue;
        LastRetakeDate = lastRetakeDate;
        LastRetakeReason = lastRetakeReason;
        newValueText = OldValue < 4 ? "4" : "5";
        retakeDate = System.DateTime.Today.ToString("yyyy-MM-dd");
        reason = string.Empty;
    }

    public int GradeId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    public string SubjectName { get; }

    public string GradeType { get; }

    public double OldValue { get; }

    public double CurrentValue { get; }

    public string GradeDate { get; }

    public int? RetakeId { get; }

    public bool HasRetake => RetakeId is not null;

    public bool CanCreateRetake => !HasRetake;

    public double? LastRetakeValue { get; }

    public string? LastRetakeDate { get; }

    public string? LastRetakeReason { get; }

    public string LastRetakeText => RetakeId is null
        ? "не было"
        : $"{LastRetakeValue:0.##} от {LastRetakeDate}";

    public string StatusText => HasRetake ? "уже оформлена" : "можно оформить";

    public string ResultText => HasRetake
        ? $"{OldValue:0.##} -> {LastRetakeValue:0.##}"
        : $"{OldValue:0.##} -> ?";

    public string GradeDetails => $"{GroupName} · {SubjectName} · {GradeType} · {GradeDate}";

    public string SuggestedValueText => OldValue < 4 ? "Предложение: 4" : "Предложение: 5";

    public string RuleText => HasRetake
        ? "Пересдача уже оформлена. Повторная недоступна."
        : "Доступна одна пересдача для оценки 2, 3 или 4.";

    [ObservableProperty]
    private string newValueText;

    [ObservableProperty]
    private string retakeDate;

    [ObservableProperty]
    private string reason;
}
