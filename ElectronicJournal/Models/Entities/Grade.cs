namespace ElectronicJournal.Models.Entities;

public sealed record Grade(
    int GradeId,
    int StudentId,
    int AssignmentId,
    int? LessonId,
    int GradeTypeId,
    double GradeValue,
    string GradeDate,
    string? Comment,
    int CreatedByUserId,
    string CreatedAt,
    string? UpdatedAt);
