namespace ElectronicJournal.Models.Dto;

public sealed record ScheduleItem(
    int ScheduleId,
    string GroupName,
    string SubjectName,
    string TeacherName,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? ClassroomName)
{
    public string DayName => DayOfWeek switch
    {
        1 => "Понедельник",
        2 => "Вторник",
        3 => "Среда",
        4 => "Четверг",
        5 => "Пятница",
        6 => "Суббота",
        7 => "Воскресенье",
        _ => "День не указан"
    };

    public string TimeRange => $"{StartTime}-{EndTime}";
}
