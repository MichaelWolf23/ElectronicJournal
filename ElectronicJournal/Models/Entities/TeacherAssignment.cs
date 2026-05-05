namespace ElectronicJournal.Models.Entities;

public sealed record TeacherAssignment(
    int AssignmentId,
    int TeacherUserId,
    int GroupId,
    int SubjectId,
    int PeriodId);
