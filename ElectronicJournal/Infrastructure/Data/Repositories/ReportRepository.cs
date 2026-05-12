using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class ReportRepository : RepositoryBase
{
    public ReportRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<LessonPrintRow> GetLessonPrintRows(int lessonId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.full_name AS student_name,
                COALESCE(a.status, 'Не отмечен') AS status,
                COALESCE(GROUP_CONCAT(gt.type_name || ': ' || gr.grade_value, ', '), '') AS grades,
                a.comment
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN students s ON s.group_id = ta.group_id
            LEFT JOIN attendance a ON a.lesson_id = l.lesson_id AND a.student_id = s.student_id
            LEFT JOIN grades gr ON gr.lesson_id = l.lesson_id AND gr.student_id = s.student_id
            LEFT JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            WHERE l.lesson_id = $lesson_id
            GROUP BY s.student_id, s.full_name, a.status, a.comment
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);

        using var reader = command.ExecuteReader();
        var rows = new List<LessonPrintRow>();

        while (reader.Read())
        {
            rows.Add(new LessonPrintRow(
                reader.GetString("student_name"),
                reader.GetString("status"),
                reader.GetString("grades"),
                reader.GetNullableString("comment")));
        }

        return rows;
    }
}
