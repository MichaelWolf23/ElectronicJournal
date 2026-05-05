namespace ElectronicJournal.Models.Entities;

public sealed record GradeRetake(
    int RetakeId,
    int OriginalGradeId,
    double OldValue,
    double NewValue,
    string RetakeDate,
    string? Reason,
    int ChangedByUserId,
    string CreatedAt);
