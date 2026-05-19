using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class GradeRepository : RepositoryBase
{
    public GradeRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<GradeJournalItem> GetJournal()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                gr.grade_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                gt.type_name AS grade_type,
                gt.weight AS grade_weight,
                gr.grade_value,
                gr.grade_date,
                gr.comment
            FROM grades gr
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            ORDER BY g.group_name, s.full_name, sub.subject_name, gr.grade_date;
            """;

        using var reader = command.ExecuteReader();
        var journal = new List<GradeJournalItem>();

        while (reader.Read())
        {
            journal.Add(new GradeJournalItem(
                reader.GetInt32("grade_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetString("grade_type"),
                reader.GetDouble("grade_weight"),
                reader.GetDouble("grade_value"),
                reader.GetString("grade_date"),
                reader.GetNullableString("comment")));
        }

        return journal;
    }

    public List<GradeJournalItem> GetJournalForTeacher(int teacherUserId)
    {
        return GetJournalByScope("ta.teacher_user_id = $user_id", teacherUserId);
    }

    public List<GradeJournalItem> GetJournalForCurator(int curatorUserId)
    {
        return GetJournalByScope(
            "EXISTS (SELECT 1 FROM group_curators gc WHERE gc.group_id = g.group_id AND gc.curator_user_id = $user_id)",
            curatorUserId);
    }

    public List<GradeEntryRow> GetGradeEntryRowsForLesson(int lessonId, int gradeTypeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name AS student_name,
                g.group_name,
                s.student_card_number,
                gr.grade_id,
                gr.grade_value,
                gr.comment
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN students s ON s.group_id = ta.group_id
            JOIN groups g ON g.group_id = s.group_id
            LEFT JOIN grades gr ON gr.grade_id = (
                SELECT gr2.grade_id
                FROM grades gr2
                WHERE gr2.student_id = s.student_id
                  AND gr2.lesson_id = l.lesson_id
                  AND gr2.assignment_id = l.assignment_id
                  AND gr2.grade_type_id = $grade_type_id
                ORDER BY COALESCE(gr2.updated_at, gr2.created_at) DESC, gr2.grade_id DESC
                LIMIT 1
            )
            WHERE l.lesson_id = $lesson_id
              AND s.status = 'Обучается'
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$grade_type_id", gradeTypeId);

        using var reader = command.ExecuteReader();
        var rows = new List<GradeEntryRow>();

        while (reader.Read())
        {
            rows.Add(new GradeEntryRow(
                reader.GetInt32("student_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetNullableString("student_card_number"),
                reader.GetNullableInt32("grade_id"),
                reader.GetNullableDouble("grade_value"),
                reader.GetNullableString("comment")));
        }

        return rows;
    }

    public int AddGrade(Grade grade)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO grades (
                student_id,
                assignment_id,
                lesson_id,
                grade_type_id,
                grade_value,
                grade_date,
                comment,
                created_by_user_id)
            VALUES (
                $student_id,
                $assignment_id,
                $lesson_id,
                $grade_type_id,
                $grade_value,
                $grade_date,
                $comment,
                $created_by_user_id);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$student_id", grade.StudentId);
        command.Parameters.AddWithValue("$assignment_id", grade.AssignmentId);
        command.Parameters.AddWithValue("$lesson_id", (object?)grade.LessonId ?? DBNull.Value);
        command.Parameters.AddWithValue("$grade_type_id", grade.GradeTypeId);
        command.Parameters.AddWithValue("$grade_value", grade.GradeValue);
        command.Parameters.AddWithValue("$grade_date", grade.GradeDate);
        command.Parameters.AddWithValue("$comment", (object?)grade.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_by_user_id", grade.CreatedByUserId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void UpsertLessonGrade(
        int? gradeId,
        int studentId,
        int assignmentId,
        int lessonId,
        int gradeTypeId,
        double gradeValue,
        string gradeDate,
        string? comment,
        int currentUserId)
    {
        if (gradeId is int existingGradeId)
        {
            UpdateGrade(existingGradeId, gradeValue, comment);
            return;
        }

        AddGrade(new Grade(
            0,
            studentId,
            assignmentId,
            lessonId,
            gradeTypeId,
            gradeValue,
            gradeDate,
            comment,
            currentUserId,
            string.Empty,
            null));
    }

    public void UpdateGrade(int gradeId, double gradeValue, string? comment)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE grades
            SET grade_value = $grade_value,
                comment = $comment,
                updated_at = CURRENT_TIMESTAMP
            WHERE grade_id = $grade_id;
            """;
        command.Parameters.AddWithValue("$grade_id", gradeId);
        command.Parameters.AddWithValue("$grade_value", gradeValue);
        command.Parameters.AddWithValue("$comment", (object?)comment ?? DBNull.Value);
        command.ExecuteNonQuery();
    }

    public void DeleteGrade(int gradeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM grades
            WHERE grade_id = $grade_id;
            """;
        command.Parameters.AddWithValue("$grade_id", gradeId);
        command.ExecuteNonQuery();
    }

    public int DeleteLessonGrades(int lessonId, int gradeTypeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM grades
            WHERE lesson_id = $lesson_id
              AND grade_type_id = $grade_type_id;
            SELECT changes();
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$grade_type_id", gradeTypeId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public bool GradeExists(int studentId, int assignmentId, int gradeTypeId, string gradeDate, int? lessonId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM grades
            WHERE student_id = $student_id
              AND assignment_id = $assignment_id
              AND grade_type_id = $grade_type_id
              AND grade_date = $grade_date
              AND (
                    ($lesson_id IS NULL AND lesson_id IS NULL)
                    OR lesson_id = $lesson_id
                  );
            """;
        command.Parameters.AddWithValue("$student_id", studentId);
        command.Parameters.AddWithValue("$assignment_id", assignmentId);
        command.Parameters.AddWithValue("$grade_type_id", gradeTypeId);
        command.Parameters.AddWithValue("$grade_date", gradeDate);
        command.Parameters.AddWithValue("$lesson_id", (object?)lessonId ?? DBNull.Value);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool CanTeacherAccessGrade(int gradeId, int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM grades gr
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            WHERE gr.grade_id = $grade_id
              AND ta.teacher_user_id = $teacher_user_id;
            """;
        command.Parameters.AddWithValue("$grade_id", gradeId);
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool CanTeacherUseAssignment(int assignmentId, int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM teacher_assignments
            WHERE assignment_id = $assignment_id
              AND teacher_user_id = $teacher_user_id;
            """;
        command.Parameters.AddWithValue("$assignment_id", assignmentId);
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool CanStudentUseAssignment(int studentId, int assignmentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM students s
            JOIN teacher_assignments ta ON ta.group_id = s.group_id
            WHERE s.student_id = $student_id
              AND ta.assignment_id = $assignment_id;
            """;
        command.Parameters.AddWithValue("$student_id", studentId);
        command.Parameters.AddWithValue("$assignment_id", assignmentId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public bool LessonBelongsToAssignment(int lessonId, int assignmentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM lessons
            WHERE lesson_id = $lesson_id
              AND assignment_id = $assignment_id;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);
        command.Parameters.AddWithValue("$assignment_id", assignmentId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public double? GetGradeValue(int gradeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT grade_value
            FROM grades
            WHERE grade_id = $grade_id;
            """;
        command.Parameters.AddWithValue("$grade_id", gradeId);

        var result = command.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToDouble(result);
    }

    public List<Grade> GetGradesByStudent(int studentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                grade_id,
                student_id,
                assignment_id,
                lesson_id,
                grade_type_id,
                grade_value,
                grade_date,
                comment,
                created_by_user_id,
                created_at,
                updated_at
            FROM grades
            WHERE student_id = $student_id
            ORDER BY grade_date;
            """;
        command.Parameters.AddWithValue("$student_id", studentId);

        using var reader = command.ExecuteReader();
        var grades = new List<Grade>();

        while (reader.Read())
        {
            grades.Add(new Grade(
                reader.GetInt32("grade_id"),
                reader.GetInt32("student_id"),
                reader.GetInt32("assignment_id"),
                reader.GetNullableInt32("lesson_id"),
                reader.GetInt32("grade_type_id"),
                reader.GetDouble("grade_value"),
                reader.GetString("grade_date"),
                reader.GetNullableString("comment"),
                reader.GetInt32("created_by_user_id"),
                reader.GetString("created_at"),
                reader.GetNullableString("updated_at")));
        }

        return grades;
    }

    public double? CalculateWeightedAverage(int studentId, int assignmentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                SUM(gr.grade_value * gt.weight) / NULLIF(SUM(gt.weight), 0) AS average_grade
            FROM grades gr
            JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            WHERE gr.student_id = $student_id
              AND gr.assignment_id = $assignment_id;
            """;
        command.Parameters.AddWithValue("$student_id", studentId);
        command.Parameters.AddWithValue("$assignment_id", assignmentId);

        var result = command.ExecuteScalar();
        return result is null or DBNull ? null : Convert.ToDouble(result);
    }

    public List<DebtorItem> GetDebtors(double minPositiveGrade)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                g.group_id,
                gr.assignment_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                gr.grade_value,
                gr.grade_date,
                gr.comment
            FROM grades gr
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            WHERE gr.grade_value < $min_positive_grade
            ORDER BY g.group_name, s.full_name, sub.subject_name;
            """;
        command.Parameters.AddWithValue("$min_positive_grade", minPositiveGrade);

        using var reader = command.ExecuteReader();
        var debtors = new List<DebtorItem>();

        while (reader.Read())
        {
            debtors.Add(new DebtorItem(
                reader.GetInt32("student_id"),
                reader.GetInt32("group_id"),
                reader.GetInt32("assignment_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetDouble("grade_value"),
                reader.GetString("grade_date"),
                reader.GetNullableString("comment")));
        }

        return debtors;
    }

    public List<DebtorItem> GetDebtorsForTeacher(double minPositiveGrade, int teacherUserId)
    {
        return GetDebtorsByScope(minPositiveGrade, "ta.teacher_user_id = $user_id", teacherUserId);
    }

    public List<DebtorItem> GetDebtorsForCurator(double minPositiveGrade, int curatorUserId)
    {
        return GetDebtorsByScope(
            minPositiveGrade,
            "EXISTS (SELECT 1 FROM group_curators gc WHERE gc.group_id = g.group_id AND gc.curator_user_id = $user_id)",
            curatorUserId);
    }

    private List<DebtorItem> GetDebtorsByScope(double minPositiveGrade, string scopeWhere, int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                s.student_id,
                g.group_id,
                gr.assignment_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                gr.grade_value,
                gr.grade_date,
                gr.comment
            FROM grades gr
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            WHERE gr.grade_value < $min_positive_grade
              AND {scopeWhere}
            ORDER BY g.group_name, s.full_name, sub.subject_name;
            """;
        command.Parameters.AddWithValue("$min_positive_grade", minPositiveGrade);
        command.Parameters.AddWithValue("$user_id", userId);

        using var reader = command.ExecuteReader();
        var debtors = new List<DebtorItem>();

        while (reader.Read())
        {
            debtors.Add(new DebtorItem(
                reader.GetInt32("student_id"),
                reader.GetInt32("group_id"),
                reader.GetInt32("assignment_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetDouble("grade_value"),
                reader.GetString("grade_date"),
                reader.GetNullableString("comment")));
        }

        return debtors;
    }

    private List<GradeJournalItem> GetJournalByScope(string scopeWhere, int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                gr.grade_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                gt.type_name AS grade_type,
                gt.weight AS grade_weight,
                gr.grade_value,
                gr.grade_date,
                gr.comment
            FROM grades gr
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            WHERE {scopeWhere}
            ORDER BY g.group_name, s.full_name, sub.subject_name, gr.grade_date;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        using var reader = command.ExecuteReader();
        var journal = new List<GradeJournalItem>();

        while (reader.Read())
        {
            journal.Add(new GradeJournalItem(
                reader.GetInt32("grade_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetString("grade_type"),
                reader.GetDouble("grade_weight"),
                reader.GetDouble("grade_value"),
                reader.GetString("grade_date"),
                reader.GetNullableString("comment")));
        }

        return journal;
    }
}

