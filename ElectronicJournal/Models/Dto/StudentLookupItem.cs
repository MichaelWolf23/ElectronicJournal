namespace ElectronicJournal.Models.Dto;

public sealed record StudentLookupItem(
    int Id,
    string Name,
    string GroupName,
    string? StudentCardNumber)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(StudentCardNumber)
            ? $"{Name} — {GroupName}"
            : $"{Name} — {GroupName}, билет {StudentCardNumber}";
}
