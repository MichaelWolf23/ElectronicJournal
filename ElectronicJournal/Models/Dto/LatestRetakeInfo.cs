namespace ElectronicJournal.Models.Dto;

public sealed record LatestRetakeInfo(
    int OriginalGradeId,
    int RetakeId,
    double OldValue,
    double NewValue,
    string RetakeDate,
    string? Reason);
