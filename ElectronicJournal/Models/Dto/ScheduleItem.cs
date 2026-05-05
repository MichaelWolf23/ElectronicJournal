namespace ElectronicJournal.Models.Dto;

public sealed record ScheduleItem(
    int ScheduleId,
    string GroupName,
    string SubjectName,
    string TeacherName,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? ClassroomName);
