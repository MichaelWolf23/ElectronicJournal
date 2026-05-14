using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class GradeRetakeRepository : RepositoryBase
{
    public GradeRetakeRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<GradeRetakeItem> GetRetakes()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                r.retake_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                teacher.full_name AS teacher_name,
                r.old_value,
                r.new_value,
                r.retake_date,
                r.reason,
                changed.full_name AS changed_by_name
            FROM grade_retakes r
            JOIN grades gr ON gr.grade_id = r.original_grade_id
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users teacher ON teacher.user_id = ta.teacher_user_id
            JOIN users changed ON changed.user_id = r.changed_by_user_id
            ORDER BY r.retake_date DESC, s.full_name;
            """;

        using var reader = command.ExecuteReader();
        var retakes = new List<GradeRetakeItem>();

        while (reader.Read())
        {
            retakes.Add(new GradeRetakeItem(
                reader.GetInt32("retake_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetDouble("old_value"),
                reader.GetDouble("new_value"),
                reader.GetString("retake_date"),
                reader.GetNullableString("reason"),
                reader.GetString("changed_by_name")));
        }

        return retakes;
    }

    public List<GradeRetakeItem> GetRetakesForTeacher(int teacherUserId)
    {
        return GetRetakesByScope("ta.teacher_user_id = $user_id", teacherUserId);
    }

    public List<GradeRetakeItem> GetRetakesForCurator(int curatorUserId)
    {
        return GetRetakesByScope(
            "EXISTS (SELECT 1 FROM group_curators gc WHERE gc.group_id = g.group_id AND gc.curator_user_id = $user_id)",
            curatorUserId);
    }

    public List<LatestRetakeInfo> GetLatestRetakes()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                r.original_grade_id,
                r.retake_id,
                r.old_value,
                r.new_value,
                r.retake_date,
                r.reason
            FROM grade_retakes r
            WHERE r.retake_id = (
                SELECT r2.retake_id
                FROM grade_retakes r2
                WHERE r2.original_grade_id = r.original_grade_id
                ORDER BY r2.retake_date DESC, r2.retake_id DESC
                LIMIT 1
            );
            """;

        using var reader = command.ExecuteReader();
        var retakes = new List<LatestRetakeInfo>();

        while (reader.Read())
        {
            retakes.Add(new LatestRetakeInfo(
                reader.GetInt32("original_grade_id"),
                reader.GetInt32("retake_id"),
                reader.GetDouble("old_value"),
                reader.GetDouble("new_value"),
                reader.GetString("retake_date"),
                reader.GetNullableString("reason")));
        }

        return retakes;
    }

    public int AddRetake(GradeRetake retake)
    {
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        using var retakeCommand = connection.CreateCommand();
        retakeCommand.Transaction = transaction;
        retakeCommand.CommandText = """
            INSERT INTO grade_retakes (
                original_grade_id,
                old_value,
                new_value,
                retake_date,
                reason,
                changed_by_user_id)
            VALUES (
                $original_grade_id,
                $old_value,
                $new_value,
                $retake_date,
                $reason,
                $changed_by_user_id);
            SELECT last_insert_rowid();
            """;
        retakeCommand.Parameters.AddWithValue("$original_grade_id", retake.OriginalGradeId);
        retakeCommand.Parameters.AddWithValue("$old_value", retake.OldValue);
        retakeCommand.Parameters.AddWithValue("$new_value", retake.NewValue);
        retakeCommand.Parameters.AddWithValue("$retake_date", retake.RetakeDate);
        retakeCommand.Parameters.AddWithValue("$reason", (object?)retake.Reason ?? DBNull.Value);
        retakeCommand.Parameters.AddWithValue("$changed_by_user_id", retake.ChangedByUserId);
        var retakeId = Convert.ToInt32(retakeCommand.ExecuteScalar());

        using var gradeCommand = connection.CreateCommand();
        gradeCommand.Transaction = transaction;
        gradeCommand.CommandText = """
            UPDATE grades
            SET grade_value = $new_value,
                updated_at = CURRENT_TIMESTAMP
            WHERE grade_id = $grade_id;
            """;
        gradeCommand.Parameters.AddWithValue("$new_value", retake.NewValue);
        gradeCommand.Parameters.AddWithValue("$grade_id", retake.OriginalGradeId);
        gradeCommand.ExecuteNonQuery();

        transaction.Commit();
        return retakeId;
    }

    public bool HasRetakeForGrade(int gradeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM grade_retakes
            WHERE original_grade_id = $grade_id;
            """;
        command.Parameters.AddWithValue("$grade_id", gradeId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public void DeleteRetake(int retakeId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText = """
            SELECT original_grade_id, old_value
            FROM grade_retakes
            WHERE retake_id = $retake_id;
            """;
        selectCommand.Parameters.AddWithValue("$retake_id", retakeId);

        int? gradeId = null;
        double? oldValue = null;
        using (var reader = selectCommand.ExecuteReader())
        {
            if (reader.Read())
            {
                gradeId = reader.GetInt32("original_grade_id");
                oldValue = reader.GetDouble("old_value");
            }
        }

        if (gradeId is null || oldValue is null)
        {
            transaction.Rollback();
            return;
        }

        using var deleteCommand = connection.CreateCommand();
        deleteCommand.Transaction = transaction;
        deleteCommand.CommandText = """
            DELETE FROM grade_retakes
            WHERE retake_id = $retake_id;
            """;
        deleteCommand.Parameters.AddWithValue("$retake_id", retakeId);
        deleteCommand.ExecuteNonQuery();

        using var gradeCommand = connection.CreateCommand();
        gradeCommand.Transaction = transaction;
        gradeCommand.CommandText = """
            UPDATE grades
            SET grade_value = $old_value,
                updated_at = CURRENT_TIMESTAMP
            WHERE grade_id = $grade_id;
            """;
        gradeCommand.Parameters.AddWithValue("$old_value", oldValue.Value);
        gradeCommand.Parameters.AddWithValue("$grade_id", gradeId.Value);
        gradeCommand.ExecuteNonQuery();

        transaction.Commit();
    }

    private List<GradeRetakeItem> GetRetakesByScope(string scopeWhere, int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                r.retake_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                teacher.full_name AS teacher_name,
                r.old_value,
                r.new_value,
                r.retake_date,
                r.reason,
                changed.full_name AS changed_by_name
            FROM grade_retakes r
            JOIN grades gr ON gr.grade_id = r.original_grade_id
            JOIN students s ON s.student_id = gr.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = gr.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users teacher ON teacher.user_id = ta.teacher_user_id
            JOIN users changed ON changed.user_id = r.changed_by_user_id
            WHERE {scopeWhere}
            ORDER BY r.retake_date DESC, s.full_name;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        using var reader = command.ExecuteReader();
        var retakes = new List<GradeRetakeItem>();

        while (reader.Read())
        {
            retakes.Add(new GradeRetakeItem(
                reader.GetInt32("retake_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetDouble("old_value"),
                reader.GetDouble("new_value"),
                reader.GetString("retake_date"),
                reader.GetNullableString("reason"),
                reader.GetString("changed_by_name")));
        }

        return retakes;
    }
}
