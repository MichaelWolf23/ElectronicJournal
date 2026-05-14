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

    public List<AttendanceJournalItem> GetAttendanceJournalForTeacher(int teacherUserId)
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
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY l.lesson_date DESC, g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

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

    public List<AttendanceJournalItem> GetAttendanceJournalForCurator(int curatorUserId)
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
            WHERE EXISTS (
                SELECT 1
                FROM group_curators gc
                WHERE gc.group_id = g.group_id
                  AND gc.curator_user_id = $curator_user_id)
            ORDER BY l.lesson_date DESC, g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

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

    public List<AttendanceMarkItem> GetLessonAttendanceMarks(int lessonId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                a.attendance_id,
                s.student_id,
                s.full_name AS student_name,
                g.group_name,
                COALESCE(a.status, 'Присутствовал') AS status,
                a.comment
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN students s ON s.group_id = ta.group_id
            JOIN groups g ON g.group_id = s.group_id
            LEFT JOIN attendance a
                ON a.lesson_id = l.lesson_id
               AND a.student_id = s.student_id
            WHERE l.lesson_id = $lesson_id
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);

        using var reader = command.ExecuteReader();
        var marks = new List<AttendanceMarkItem>();

        while (reader.Read())
        {
            marks.Add(new AttendanceMarkItem(
                reader.GetNullableInt32("attendance_id"),
                reader.GetInt32("student_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("status"),
                reader.GetNullableString("comment")));
        }

        return marks;
    }

    public List<StudentRiskItem> GetAbsenceRisks()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                g.group_id,
                l.assignment_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                a.status,
                l.lesson_date,
                a.comment
            FROM attendance a
            JOIN lessons l ON l.lesson_id = a.lesson_id
            JOIN students s ON s.student_id = a.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            WHERE LOWER(a.status) LIKE '%отсутств%'
               OR LOWER(a.status) LIKE '%опоздал%'
            ORDER BY l.lesson_date DESC, g.group_name, s.full_name;
            """;

        using var reader = command.ExecuteReader();
        var risks = new List<StudentRiskItem>();

        while (reader.Read())
        {
            risks.Add(new StudentRiskItem(
                reader.GetInt32("student_id"),
                reader.GetInt32("group_id"),
                reader.GetNullableInt32("assignment_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                "Посещаемость",
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetString("status"),
                reader.GetString("lesson_date"),
                reader.GetNullableString("comment")));
        }

        return risks;
    }

    public List<StudentRiskItem> GetAbsenceRisksForTeacher(int teacherUserId)
    {
        return GetAbsenceRisksByScope("ta.teacher_user_id = $user_id", teacherUserId);
    }

    public List<StudentRiskItem> GetAbsenceRisksForCurator(int curatorUserId)
    {
        return GetAbsenceRisksByScope(
            "EXISTS (SELECT 1 FROM group_curators gc WHERE gc.group_id = g.group_id AND gc.curator_user_id = $user_id)",
            curatorUserId);
    }

    private List<StudentRiskItem> GetAbsenceRisksByScope(string scopeWhere, int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                s.student_id,
                g.group_id,
                l.assignment_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                a.status,
                l.lesson_date,
                a.comment
            FROM attendance a
            JOIN lessons l ON l.lesson_id = a.lesson_id
            JOIN students s ON s.student_id = a.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            WHERE (LOWER(a.status) LIKE '%отсутств%' OR LOWER(a.status) LIKE '%опоздал%')
              AND {scopeWhere}
            ORDER BY l.lesson_date DESC, g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        using var reader = command.ExecuteReader();
        var risks = new List<StudentRiskItem>();

        while (reader.Read())
        {
            risks.Add(new StudentRiskItem(
                reader.GetInt32("student_id"),
                reader.GetInt32("group_id"),
                reader.GetNullableInt32("assignment_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                "Посещаемость",
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetString("status"),
                reader.GetString("lesson_date"),
                reader.GetNullableString("comment")));
        }

        return risks;
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

    public void DeleteAttendance(int attendanceId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM attendance
            WHERE attendance_id = $attendance_id;
            """;
        command.Parameters.AddWithValue("$attendance_id", attendanceId);
        command.ExecuteNonQuery();
    }

    public bool CanStudentAttendLesson(int lessonId, int studentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN students s ON s.group_id = ta.group_id
            WHERE l.lesson_id = $lesson_id
              AND s.student_id = $student_id;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$student_id", studentId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool CanTeacherAccessLesson(int lessonId, int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            WHERE l.lesson_id = $lesson_id
              AND ta.teacher_user_id = $teacher_user_id;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}

