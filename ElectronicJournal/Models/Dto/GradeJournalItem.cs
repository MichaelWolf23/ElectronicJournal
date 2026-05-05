namespace ElectronicJournal.Models.Dto;

public sealed record GradeJournalItem(
    int GradeId,
    string StudentName,
    string GroupName,
    string SubjectName,
    string TeacherName,
    string GradeType,
    double GradeWeight,
    double GradeValue,
    string GradeDate,
    string? Comment);
