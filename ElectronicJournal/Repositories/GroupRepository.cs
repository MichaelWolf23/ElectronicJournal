using System;
using System.Collections.Generic;
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
}

