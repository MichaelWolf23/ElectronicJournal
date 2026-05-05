namespace ElectronicJournal.Models.Dto;

public sealed record LessonListItem(
    int LessonId,
    string LessonDate,
    string Topic,
    string GroupName,
    string SubjectName,
    string TeacherName,
    string? ClassroomName,
    string? Note);
