namespace ElectronicJournal.Models.Dto;

public sealed record LessonScheduleCard(
    string DayName,
    string TimeRange,
    string GroupName,
    string SubjectName,
    string ClassroomName);
