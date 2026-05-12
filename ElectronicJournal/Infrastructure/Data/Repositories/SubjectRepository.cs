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

    public List<Subject> GetSubjectsForTeacher(int teacherUserId)
    {
        return GetSubjectsByScope("ta.teacher_user_id = $user_id", teacherUserId);
    }

    public List<Subject> GetSubjectsForCurator(int curatorUserId)
    {
        return GetSubjectsByScope(
            "EXISTS (SELECT 1 FROM group_curators gc WHERE gc.group_id = ta.group_id AND gc.curator_user_id = $user_id)",
            curatorUserId);
    }

    private List<Subject> GetSubjectsByScope(string scopeWhere, int userId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT DISTINCT sub.subject_id, sub.subject_name, sub.description
            FROM subjects sub
            JOIN teacher_assignments ta ON ta.subject_id = sub.subject_id
            WHERE {scopeWhere}
            ORDER BY sub.subject_name;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

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

