namespace ElectronicJournal.Models.Entities;

public sealed record Group(
    int GroupId,
    string GroupName,
    int? CourseNumber,
    string? Description);
