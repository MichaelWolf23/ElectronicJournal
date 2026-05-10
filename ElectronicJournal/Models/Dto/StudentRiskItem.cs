namespace ElectronicJournal.Models.Dto;

public sealed record StudentRiskItem(
    int StudentId,
    int GroupId,
    int? AssignmentId,
    string StudentName,
    string GroupName,
    string RiskType,
    string SubjectName,
    string TeacherName,
    string ValueText,
    string DateText,
    string? Comment);
