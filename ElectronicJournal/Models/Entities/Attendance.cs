namespace ElectronicJournal.Models.Entities;

public sealed record Attendance(
    int AttendanceId,
    int LessonId,
    int StudentId,
    string Status,
    string? Comment,
    string MarkedAt);
