using System.Collections.Generic;
using System;
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

    public List<TeacherAssignmentItem> GetTeacherAssignments()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                ta.assignment_id,
                u.full_name AS teacher_name,
                g.group_name,
                sub.subject_name,
                ap.period_name
            FROM teacher_assignments ta
            JOIN users u ON u.user_id = ta.teacher_user_id
            JOIN groups g ON g.group_id = ta.group_id
            JOIN subjects sub ON sub.subject_id = ta.subject_id
            JOIN academic_periods ap ON ap.period_id = ta.period_id
            ORDER BY u.full_name, g.group_name, sub.subject_name, ap.period_name;
            """;

        using var reader = command.ExecuteReader();
        var assignments = new List<TeacherAssignmentItem>();

        while (reader.Read())
        {
            assignments.Add(new TeacherAssignmentItem(
                reader.GetInt32("assignment_id"),
                reader.GetString("teacher_name"),
                reader.GetString("group_name"),
                reader.GetString("subject_name"),
                reader.GetString("period_name")));
        }

        return assignments;
    }

    public List<GroupCuratorItem> GetGroupCurators()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                gc.group_curator_id,
                g.group_name,
                u.full_name AS curator_name,
                gc.assigned_at
            FROM group_curators gc
            JOIN groups g ON g.group_id = gc.group_id
            JOIN users u ON u.user_id = gc.curator_user_id
            ORDER BY g.group_name, u.full_name;
            """;

        using var reader = command.ExecuteReader();
        var curators = new List<GroupCuratorItem>();

        while (reader.Read())
        {
            curators.Add(new GroupCuratorItem(
                reader.GetInt32("group_curator_id"),
                reader.GetString("group_name"),
                reader.GetString("curator_name"),
                reader.GetString("assigned_at")));
        }

        return curators;
    }

    public List<LookupItem> GetPeriodLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT period_id, period_name
            FROM academic_periods
            ORDER BY is_archived, start_date DESC, period_name;
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

    public bool TeacherAssignmentExists(int teacherUserId, int groupId, int subjectId, int periodId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM teacher_assignments
            WHERE teacher_user_id = $teacher_user_id
              AND group_id = $group_id
              AND subject_id = $subject_id
              AND period_id = $period_id;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);
        command.Parameters.AddWithValue("$group_id", groupId);
        command.Parameters.AddWithValue("$subject_id", subjectId);
        command.Parameters.AddWithValue("$period_id", periodId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int AddTeacherAssignment(int teacherUserId, int groupId, int subjectId, int periodId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO teacher_assignments (teacher_user_id, group_id, subject_id, period_id)
            VALUES ($teacher_user_id, $group_id, $subject_id, $period_id);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);
        command.Parameters.AddWithValue("$group_id", groupId);
        command.Parameters.AddWithValue("$subject_id", subjectId);
        command.Parameters.AddWithValue("$period_id", periodId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteTeacherAssignment(int assignmentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM teacher_assignments
            WHERE assignment_id = $assignment_id;
            """;
        command.Parameters.AddWithValue("$assignment_id", assignmentId);
        command.ExecuteNonQuery();
    }

    public bool CuratorAssignmentExists(int groupId, int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM group_curators
            WHERE group_id = $group_id
              AND curator_user_id = $curator_user_id;
            """;
        command.Parameters.AddWithValue("$group_id", groupId);
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    public int AddGroupCurator(int groupId, int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO group_curators (group_id, curator_user_id)
            VALUES ($group_id, $curator_user_id);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$group_id", groupId);
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void DeleteGroupCurator(int groupCuratorId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM group_curators
            WHERE group_curator_id = $group_curator_id;
            """;
        command.Parameters.AddWithValue("$group_curator_id", groupCuratorId);
        command.ExecuteNonQuery();
    }
}
