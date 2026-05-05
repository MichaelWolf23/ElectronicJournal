namespace ElectronicJournal.Models.Entities;

public sealed record FinalGrade(
    int FinalGradeId,
    int StudentId,
    int AssignmentId,
    int PeriodId,
    double FinalValue,
    double? CalculatedAverage,
    string? Comment,
    int? ApprovedByUserId,
    string? ApprovedAt);
