namespace ElectronicJournal.Models.Dto;

public sealed record AttendanceJournalItem(
    int AttendanceId,
    string LessonDate,
    string Topic,
    string StudentName,
    string GroupName,
    string SubjectName,
    string Status,
    string? Comment);
