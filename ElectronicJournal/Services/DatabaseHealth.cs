namespace ElectronicJournal.Services;

public sealed record DatabaseHealth(
    bool IsAvailable,
    string DatabasePath,
    int TableCount,
    string? ErrorMessage);
