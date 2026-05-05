namespace ElectronicJournal.Models.Dto;

public sealed record DebtorItem(
    int StudentId,
    string StudentName,
    string GroupName,
    string SubjectName,
    string TeacherName,
    double GradeValue,
    string GradeDate,
    string? Comment);
