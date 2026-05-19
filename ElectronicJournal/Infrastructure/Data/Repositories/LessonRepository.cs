using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class LessonRepository : RepositoryBase
{
    public LessonRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<LessonListItem> GetLessons()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.assignment_id,
                l.classroom_id,
                l.lesson_date,
                l.topic,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                c.classroom_name,
                l.note
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = l.classroom_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name;
            """;

        using var reader = command.ExecuteReader();
        var lessons = new List<LessonListItem>();

        while (reader.Read())
        {
            lessons.Add(new LessonListItem(
                reader.GetInt32("lesson_id"),
                reader.GetInt32("assignment_id"),
                reader.GetNullableInt32("classroom_id"),
                reader.GetString("lesson_date"),
                reader.GetString("topic"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetNullableString("classroom_name"),
                reader.GetNullableString("note")));
        }

        return lessons;
    }

    public List<LessonListItem> GetLessonsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.assignment_id,
                l.classroom_id,
                l.lesson_date,
                l.topic,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                c.classroom_name,
                l.note
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = l.classroom_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var lessons = new List<LessonListItem>();

        while (reader.Read())
        {
            lessons.Add(new LessonListItem(
                reader.GetInt32("lesson_id"),
                reader.GetInt32("assignment_id"),
                reader.GetNullableInt32("classroom_id"),
                reader.GetString("lesson_date"),
                reader.GetString("topic"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetNullableString("classroom_name"),
                reader.GetNullableString("note")));
        }

        return lessons;
    }

    public List<LessonJournalLessonItem> GetJournalLessons()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.assignment_id,
                ta.group_id,
                l.lesson_date,
                l.topic,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                c.classroom_name,
                l.note
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = l.classroom_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name;
            """;

        using var reader = command.ExecuteReader();
        var lessons = new List<LessonJournalLessonItem>();

        while (reader.Read())
        {
            lessons.Add(new LessonJournalLessonItem(
                reader.GetInt32("lesson_id"),
                reader.GetInt32("assignment_id"),
                reader.GetInt32("group_id"),
                reader.GetString("lesson_date"),
                reader.GetString("topic"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetNullableString("classroom_name"),
                reader.GetNullableString("note")));
        }

        return lessons;
    }

    public List<LessonJournalLessonItem> GetJournalLessonsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.assignment_id,
                ta.group_id,
                l.lesson_date,
                l.topic,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                c.classroom_name,
                l.note
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = l.classroom_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var lessons = new List<LessonJournalLessonItem>();

        while (reader.Read())
        {
            lessons.Add(new LessonJournalLessonItem(
                reader.GetInt32("lesson_id"),
                reader.GetInt32("assignment_id"),
                reader.GetInt32("group_id"),
                reader.GetString("lesson_date"),
                reader.GetString("topic"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetNullableString("classroom_name"),
                reader.GetNullableString("note")));
        }

        return lessons;
    }

    public int AddLesson(Lesson lesson)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO lessons (assignment_id, schedule_id, lesson_date, topic, classroom_id, note)
            VALUES ($assignment_id, $schedule_id, $lesson_date, $topic, $classroom_id, $note);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$assignment_id", lesson.AssignmentId);
        command.Parameters.AddWithValue("$schedule_id", (object?)lesson.ScheduleId ?? DBNull.Value);
        command.Parameters.AddWithValue("$lesson_date", lesson.LessonDate);
        command.Parameters.AddWithValue("$topic", lesson.Topic);
        command.Parameters.AddWithValue("$classroom_id", (object?)lesson.ClassroomId ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", (object?)lesson.Note ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool LessonExists(int assignmentId, string lessonDate, string topic)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM lessons
            WHERE assignment_id = $assignment_id
              AND lesson_date = $lesson_date
              AND lower(topic) = lower($topic);
            """;
        command.Parameters.AddWithValue("$assignment_id", assignmentId);
        command.Parameters.AddWithValue("$lesson_date", lessonDate);
        command.Parameters.AddWithValue("$topic", topic.Trim());

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void UpdateLesson(Lesson lesson)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE lessons
            SET assignment_id = $assignment_id,
                lesson_date = $lesson_date,
                topic = $topic,
                classroom_id = $classroom_id,
                note = $note,
                updated_at = CURRENT_TIMESTAMP
            WHERE lesson_id = $lesson_id;
            """;
        command.Parameters.AddWithValue("$lesson_id", lesson.LessonId);
        command.Parameters.AddWithValue("$assignment_id", lesson.AssignmentId);
        command.Parameters.AddWithValue("$lesson_date", lesson.LessonDate);
        command.Parameters.AddWithValue("$topic", lesson.Topic);
        command.Parameters.AddWithValue("$classroom_id", (object?)lesson.ClassroomId ?? DBNull.Value);
        command.Parameters.AddWithValue("$note", (object?)lesson.Note ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void DeleteLesson(int lessonId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        using (var detachGrades = connection.CreateCommand())
        {
            detachGrades.Transaction = transaction;
            detachGrades.CommandText = """
                UPDATE grades
                SET lesson_id = NULL,
                    updated_at = CURRENT_TIMESTAMP
                WHERE lesson_id = $lesson_id;
                """;
            detachGrades.Parameters.AddWithValue("$lesson_id", lessonId);
            detachGrades.ExecuteNonQuery();
        }

        using (var deleteAttendance = connection.CreateCommand())
        {
            deleteAttendance.Transaction = transaction;
            deleteAttendance.CommandText = """
                DELETE FROM attendance
                WHERE lesson_id = $lesson_id;
                """;
            deleteAttendance.Parameters.AddWithValue("$lesson_id", lessonId);
            deleteAttendance.ExecuteNonQuery();
        }

        using (var deleteLesson = connection.CreateCommand())
        {
            deleteLesson.Transaction = transaction;
            deleteLesson.CommandText = """
                DELETE FROM lessons
                WHERE lesson_id = $lesson_id;
                """;
            deleteLesson.Parameters.AddWithValue("$lesson_id", lessonId);
            deleteLesson.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public List<ScheduleItem> GetSchedule()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ls.schedule_id,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                ls.day_of_week,
                ls.start_time,
                ls.end_time,
                c.classroom_name
            FROM lesson_schedule ls
            JOIN teacher_assignments ta ON ta.assignment_id = ls.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = ls.classroom_id
            ORDER BY ls.day_of_week, ls.start_time;
            """;

        using var reader = command.ExecuteReader();
        var schedule = new List<ScheduleItem>();

        while (reader.Read())
        {
            schedule.Add(new ScheduleItem(
                reader.GetInt32("schedule_id"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetInt32("day_of_week"),
                reader.GetString("start_time"),
                reader.GetString("end_time"),
                reader.GetNullableString("classroom_name")));
        }

        return schedule;
    }

    public List<ScheduleItem> GetScheduleForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ls.schedule_id,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                ls.day_of_week,
                ls.start_time,
                ls.end_time,
                c.classroom_name
            FROM lesson_schedule ls
            JOIN teacher_assignments ta ON ta.assignment_id = ls.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            LEFT JOIN classrooms c ON c.classroom_id = ls.classroom_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY ls.day_of_week, ls.start_time;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var schedule = new List<ScheduleItem>();

        while (reader.Read())
        {
            schedule.Add(new ScheduleItem(
                reader.GetInt32("schedule_id"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetInt32("day_of_week"),
                reader.GetString("start_time"),
                reader.GetString("end_time"),
                reader.GetNullableString("classroom_name")));
        }

        return schedule;
    }

    public List<LookupItem> GetLessonLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.lesson_date || ' — ' || g.group_name || ' — ' || sub.subject_name || ' — ' || l.topic AS lesson_name
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name, l.topic;
            """;

        using var reader = command.ExecuteReader();
        var lessons = new List<LookupItem>();

        while (reader.Read())
        {
            lessons.Add(new LookupItem(
                reader.GetInt32("lesson_id"),
                reader.GetString("lesson_name")));
        }

        return lessons;
    }

    public List<LookupItem> GetLessonLookupsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                l.lesson_id,
                l.lesson_date || ' — ' || g.group_name || ' — ' || sub.subject_name || ' — ' || l.topic AS lesson_name
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY l.lesson_date DESC, g.group_name, sub.subject_name, l.topic;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var lessons = new List<LookupItem>();

        while (reader.Read())
        {
            lessons.Add(new LookupItem(
                reader.GetInt32("lesson_id"),
                reader.GetString("lesson_name")));
        }

        return lessons;
    }

    public List<LookupItem> GetClassroomLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT classroom_id, classroom_name
            FROM classrooms
            ORDER BY classroom_name;
            """;

        using var reader = command.ExecuteReader();
        var classrooms = new List<LookupItem>();

        while (reader.Read())
        {
            classrooms.Add(new LookupItem(
                reader.GetInt32("classroom_id"),
                reader.GetString("classroom_name")));
        }

        return classrooms;
    }
}

