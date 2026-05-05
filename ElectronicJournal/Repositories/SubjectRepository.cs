using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class SubjectRepository : RepositoryBase
{
    public SubjectRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<Subject> GetAll()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT subject_id, subject_name, description
            FROM subjects
            ORDER BY subject_name;
            """;

        using var reader = command.ExecuteReader();
        var subjects = new List<Subject>();

        while (reader.Read())
        {
            subjects.Add(new Subject(
                reader.GetInt32("subject_id"),
                reader.GetString("subject_name"),
                reader.GetNullableString("description")));
        }

        return subjects;
    }
}

