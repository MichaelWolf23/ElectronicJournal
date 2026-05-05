namespace ElectronicJournal.Models.Entities;

public sealed record Student(
    int StudentId,
    int GroupId,
    string FullName,
    string? StudentCardNumber,
    string? Email,
    string? Phone,
    string Status,
    string CreatedAt);
