using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class AssignmentRepository : RepositoryBase
{
    public AssignmentRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<LookupItem> GetAssignmentLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ta.assignment_id,
                g.group_name || ' — ' || sub.subject_name || ' — ' || u.full_name AS assignment_name
            FROM teacher_assignments ta
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            ORDER BY g.group_name, sub.subject_name, u.full_name;
            """;

        using var reader = command.ExecuteReader();
        var assignments = new List<LookupItem>();

        while (reader.Read())
        {
            assignments.Add(new LookupItem(
                reader.GetInt32("assignment_id"),
                reader.GetString("assignment_name")));
        }

        return assignments;
    }

    public List<LookupItem> GetAssignmentLookupsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ta.assignment_id,
                g.group_name || ' — ' || sub.subject_name || ' — ' || u.full_name AS assignment_name
            FROM teacher_assignments ta
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN users u ON u.user_id = ta.teacher_user_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY g.group_name, sub.subject_name, u.full_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var assignments = new List<LookupItem>();

        while (reader.Read())
        {
            assignments.Add(new LookupItem(
                reader.GetInt32("assignment_id"),
                reader.GetString("assignment_name")));
        }

        return assignments;
    }
}
