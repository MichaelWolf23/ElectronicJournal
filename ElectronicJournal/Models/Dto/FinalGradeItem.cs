namespace ElectronicJournal.Models.Dto;

public sealed record FinalGradeItem(
    int FinalGradeId,
    string StudentName,
    string GroupName,
    string SubjectName,
    string TeacherName,
    string PeriodName,
    double FinalValue,
    double? CalculatedAverage,
    string? Comment,
    string? ApprovedAt);
