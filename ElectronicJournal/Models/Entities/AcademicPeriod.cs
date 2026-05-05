namespace ElectronicJournal.Models.Entities;

public sealed record AcademicPeriod(
    int PeriodId,
    string PeriodName,
    string StartDate,
    string EndDate,
    bool IsArchived);
