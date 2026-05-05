using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class GradeTypeRepository : RepositoryBase
{
    public GradeTypeRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<GradeType> GetAll()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT grade_type_id, type_name, weight, description
            FROM grade_types
            ORDER BY type_name;
            """;

        using var reader = command.ExecuteReader();
        var gradeTypes = new List<GradeType>();

        while (reader.Read())
        {
            gradeTypes.Add(new GradeType(
                reader.GetInt32("grade_type_id"),
                reader.GetString("type_name"),
                reader.GetDouble("weight"),
                reader.GetNullableString("description")));
        }

        return gradeTypes;
    }

    public List<LookupItem> GetGradeTypeLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT grade_type_id, type_name
            FROM grade_types
            ORDER BY type_name;
            """;

        using var reader = command.ExecuteReader();
        var gradeTypes = new List<LookupItem>();

        while (reader.Read())
        {
            gradeTypes.Add(new LookupItem(
                reader.GetInt32("grade_type_id"),
                reader.GetString("type_name")));
        }

        return gradeTypes;
    }
}

