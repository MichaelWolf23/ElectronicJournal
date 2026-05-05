namespace ElectronicJournal.Models.Entities;

public sealed record Lesson(
    int LessonId,
    int AssignmentId,
    int? ScheduleId,
    string LessonDate,
    string Topic,
    int? ClassroomId,
    string? Note);
