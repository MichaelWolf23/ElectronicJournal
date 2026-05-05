using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class AttendanceRepository : RepositoryBase
{
    public AttendanceRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<AttendanceJournalItem> GetAttendanceJournal()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                a.attendance_id,
                l.lesson_date,
                l.topic,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                a.status,
                a.comment
            FROM attendance a
            JOIN lessons l ON l.lesson_id = a.lesson_id
            JOIN students s ON s.student_id = a.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            ORDER BY l.lesson_date DESC, g.group_name, s.full_name;
            """;

        using var reader = command.ExecuteReader();
        var journal = new List<AttendanceJournalItem>();

        while (reader.Read())
        {
            journal.Add(new AttendanceJournalItem(
                reader.GetInt32("attendance_id"),
                reader.GetString("lesson_date"),
                reader.GetString("topic"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("status"),
                reader.GetNullableString("comment")));
        }

        return journal;
    }

    public void UpsertAttendance(int lessonId, int studentId, string status, string? comment)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO attendance (lesson_id, student_id, status, comment)
            VALUES ($lesson_id, $student_id, $status, $comment)
            ON CONFLICT(lesson_id, student_id) DO UPDATE SET
                status = excluded.status,
                comment = excluded.comment,
                marked_at = CURRENT_TIMESTAMP;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$student_id", studentId);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$comment", (object?)comment ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
}

