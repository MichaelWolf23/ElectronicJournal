namespace ElectronicJournal.Models.Dto;

public sealed record GradeRetakeItem(
    int RetakeId,
    string StudentName,
    string GroupName,
    string SubjectName,
    string TeacherName,
    double OldValue,
    double NewValue,
    string RetakeDate,
    string? Reason,
    string ChangedByName);
