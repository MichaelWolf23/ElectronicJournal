namespace ElectronicJournal.Models.Entities;

public sealed record GradeType(
    int GradeTypeId,
    string TypeName,
    double Weight,
    string? Description);
