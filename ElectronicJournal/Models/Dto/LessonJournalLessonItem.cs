namespace ElectronicJournal.Models.Dto;

public sealed record LessonJournalLessonItem(
    int LessonId,
    int AssignmentId,
    int GroupId,
    string LessonDate,
    string Topic,
    string GroupName,
    string SubjectName,
    string TeacherName,
    string? ClassroomName,
    string? Note);
