namespace ElectronicJournal.Models.Dto;

public sealed record LessonPrintRow(
    string StudentName,
    string Status,
    string Grades,
    string? Comment);
