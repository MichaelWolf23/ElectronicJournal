namespace ElectronicJournal.Models.Dto;

public sealed record TeacherAssignmentItem(
    int AssignmentId,
    string TeacherName,
    string GroupName,
    string SubjectName,
    string PeriodName);
