namespace ElectronicJournal.Models.Dto;

public sealed record StudentListItem(
    int StudentId,
    string FullName,
    string GroupName,
    int? CourseNumber,
    string? StudentCardNumber,
    string? Email,
    string? Phone,
    string Status);
