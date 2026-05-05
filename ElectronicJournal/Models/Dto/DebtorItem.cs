namespace ElectronicJournal.Models.Dto;

public sealed record DebtorItem(
    int StudentId,
    int GroupId,
    int AssignmentId,
    string StudentName,
    string GroupName,
    string SubjectName,
    string TeacherName,
    double GradeValue,
    string GradeDate,
    string? Comment);
