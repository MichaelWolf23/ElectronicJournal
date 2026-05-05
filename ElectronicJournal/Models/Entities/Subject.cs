namespace ElectronicJournal.Models.Entities;

public sealed record Subject(
    int SubjectId,
    string SubjectName,
    string? Description);
