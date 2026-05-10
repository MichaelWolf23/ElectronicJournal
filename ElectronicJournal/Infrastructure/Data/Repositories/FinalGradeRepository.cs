using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class FinalGradeRepository : RepositoryBase
{
    public FinalGradeRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<FinalGradeItem> GetFinalGrades()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                fg.final_grade_id,
                s.full_name AS student_name,
                g.group_name,
                sub.subject_name,
                u.full_name AS teacher_name,
                ap.period_name,
                fg.final_value,
                fg.calculated_average,
                fg.comment,
                fg.approved_at
            FROM final_grades fg
            JOIN students s ON s.student_id = fg.student_id
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.assignment_id = fg.assignment_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            JOIN academic_periods ap ON ap.period_id = fg.period_id
            ORDER BY ap.period_name DESC, g.group_name, s.full_name, sub.subject_name;
            """;

        using var reader = command.ExecuteReader();
        var finalGrades = new List<FinalGradeItem>();

        while (reader.Read())
        {
            finalGrades.Add(new FinalGradeItem(
                reader.GetInt32("final_grade_id"),
                reader.GetString("student_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("teacher_name"),
                reader.GetString("period_name"),
                reader.GetDouble("final_value"),
                reader.GetNullableDouble("calculated_average"),
                reader.GetNullableString("comment"),
                reader.GetNullableString("approved_at")));
        }

        return finalGrades;
    }

    public List<LookupItem> GetPeriodLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT period_id, period_name
            FROM academic_periods
            ORDER BY start_date DESC;
            """;

        using var reader = command.ExecuteReader();
        var periods = new List<LookupItem>();

        while (reader.Read())
        {
            periods.Add(new LookupItem(
                reader.GetInt32("period_id"),
                reader.GetString("period_name")));
        }

        return periods;
    }

    public double? CalculateAverage(int studentId, int assignmentId)
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

    public void SaveFinalGrade(FinalGrade finalGrade)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO final_grades (
                student_id,
                assignment_id,
                period_id,
                final_value,
                calculated_average,
                comment,
                approved_by_user_id,
                approved_at)
            VALUES (
                $student_id,
                $assignment_id,
                $period_id,
                $final_value,
                $calculated_average,
                $comment,
                $approved_by_user_id,
                CURRENT_TIMESTAMP)
            ON CONFLICT(student_id, assignment_id, period_id)
            DO UPDATE SET
                final_value = excluded.final_value,
                calculated_average = excluded.calculated_average,
                comment = excluded.comment,
                approved_by_user_id = excluded.approved_by_user_id,
                approved_at = CURRENT_TIMESTAMP;
            """;
        command.Parameters.AddWithValue("$student_id", finalGrade.StudentId);
        command.Parameters.AddWithValue("$assignment_id", finalGrade.AssignmentId);
        command.Parameters.AddWithValue("$period_id", finalGrade.PeriodId);
        command.Parameters.AddWithValue("$final_value", finalGrade.FinalValue);
        command.Parameters.AddWithValue("$calculated_average", (object?)finalGrade.CalculatedAverage ?? DBNull.Value);
        command.Parameters.AddWithValue("$comment", (object?)finalGrade.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("$approved_by_user_id", (object?)finalGrade.ApprovedByUserId ?? DBNull.Value);
        command.ExecuteNonQuery();
    }
}
