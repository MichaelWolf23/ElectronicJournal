namespace ElectronicJournal.Models.Dto;

public sealed record GroupChartItem(
    string GroupName,
    string AverageText,
    double AverageWidth,
    int DebtorCount,
    double DebtorWidth);
