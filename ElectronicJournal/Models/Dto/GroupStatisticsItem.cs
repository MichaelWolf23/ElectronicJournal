namespace ElectronicJournal.Models.Dto;

public sealed record GroupStatisticsItem(
    int GroupId,
    string GroupName,
    int StudentCount,
    double? AverageGrade,
    int DebtorCount);
