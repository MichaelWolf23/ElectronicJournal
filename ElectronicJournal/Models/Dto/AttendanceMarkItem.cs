using CommunityToolkit.Mvvm.ComponentModel;

namespace ElectronicJournal.Models.Dto;

public sealed partial class AttendanceMarkItem : ObservableObject
{
    public AttendanceMarkItem(
        int? attendanceId,
        int studentId,
        string studentName,
        string groupName,
        string status,
        string? comment)
    {
        AttendanceId = attendanceId;
        StudentId = studentId;
        StudentName = studentName;
        GroupName = groupName;
        this.status = status;
        this.comment = comment ?? string.Empty;
    }

    public int? AttendanceId { get; }

    public int StudentId { get; }

    public string StudentName { get; }

    public string GroupName { get; }

    [ObservableProperty]
    private string status;

    [ObservableProperty]
    private string comment;
}
