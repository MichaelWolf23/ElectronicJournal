using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class GroupRepository : RepositoryBase
{
    public GroupRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<Group> GetAll()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT group_id, group_name, course_number, description
            FROM groups
            ORDER BY group_name;
            """;

        using var reader = command.ExecuteReader();
        var groups = new List<Group>();

        while (reader.Read())
        {
            groups.Add(new Group(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("description")));
        }

        return groups;
    }

    public List<Group> GetGroupsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT g.group_id, g.group_name, g.course_number, g.description
            FROM groups g
            JOIN teacher_assignments ta ON ta.group_id = g.group_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY g.group_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var groups = new List<Group>();

        while (reader.Read())
        {
            groups.Add(new Group(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("description")));
        }

        return groups;
    }

    public List<Group> GetGroupsForCurator(int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT g.group_id, g.group_name, g.course_number, g.description
            FROM groups g
            JOIN group_curators gc ON gc.group_id = g.group_id
            WHERE gc.curator_user_id = $curator_user_id
            ORDER BY g.group_name;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var groups = new List<Group>();

        while (reader.Read())
        {
            groups.Add(new Group(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("description")));
        }

        return groups;
    }

    public Group? GetById(int groupId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT group_id, group_name, course_number, description
            FROM groups
            WHERE group_id = $group_id;
            """;
        command.Parameters.AddWithValue("$group_id", groupId);

        using var reader = command.ExecuteReader();
        return reader.Read()
            ? new Group(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("description"))
            : null;
    }

    public List<GroupStatisticsItem> GetGroupStatistics(double minPositiveGrade)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                g.group_id,
                g.group_name,
                COUNT(DISTINCT s.student_id) AS student_count,
                ROUND(
                    SUM(gr.grade_value * gt.weight) / NULLIF(SUM(gt.weight), 0),
                    2
                ) AS average_grade,
                COUNT(DISTINCT CASE
                    WHEN gr.grade_value < $min_positive_grade THEN s.student_id
                END) AS debtor_count
            FROM groups g
            LEFT JOIN students s ON s.group_id = g.group_id
            LEFT JOIN grades gr ON gr.student_id = s.student_id
            LEFT JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            GROUP BY g.group_id, g.group_name
            ORDER BY g.group_name;
            """;
        command.Parameters.AddWithValue("$min_positive_grade", minPositiveGrade);

        using var reader = command.ExecuteReader();
        var statistics = new List<GroupStatisticsItem>();

        while (reader.Read())
        {
            statistics.Add(new GroupStatisticsItem(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetInt32("student_count"),
                reader.GetNullableDouble("average_grade"),
                reader.GetInt32("debtor_count")));
        }

        return statistics;
    }

    public List<GroupStatisticsItem> GetGroupStatisticsForCurator(double minPositiveGrade, int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                g.group_id,
                g.group_name,
                COUNT(DISTINCT s.student_id) AS student_count,
                ROUND(
                    SUM(gr.grade_value * gt.weight) / NULLIF(SUM(gt.weight), 0),
                    2
                ) AS average_grade,
                COUNT(DISTINCT CASE
                    WHEN gr.grade_value < $min_positive_grade THEN s.student_id
                END) AS debtor_count
            FROM groups g
            JOIN group_curators gc ON gc.group_id = g.group_id
            LEFT JOIN students s ON s.group_id = g.group_id
            LEFT JOIN grades gr ON gr.student_id = s.student_id
            LEFT JOIN grade_types gt ON gt.grade_type_id = gr.grade_type_id
            WHERE gc.curator_user_id = $curator_user_id
            GROUP BY g.group_id, g.group_name
            ORDER BY g.group_name;
            """;
        command.Parameters.AddWithValue("$min_positive_grade", minPositiveGrade);
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var statistics = new List<GroupStatisticsItem>();

        while (reader.Read())
        {
            statistics.Add(new GroupStatisticsItem(
                reader.GetInt32("group_id"),
                reader.GetString("group_name"),
                reader.GetInt32("student_count"),
                reader.GetNullableDouble("average_grade"),
                reader.GetInt32("debtor_count")));
        }

        return statistics;
    }
}

