using System;
using System.Collections.Generic;
using ElectronicJournal.Models.Dto;
using ElectronicJournal.Models.Entities;
using ElectronicJournal.Services;
using ElectronicJournal.Utilities;

namespace ElectronicJournal.Repositories;

public sealed class StudentRepository : RepositoryBase
{
    public StudentRepository(DatabaseService databaseService)
        : base(databaseService)
    {
    }

    public List<StudentListItem> GetStudents()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                g.course_number,
                s.student_card_number,
                s.email,
                s.phone,
                s.status
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            ORDER BY g.group_name, s.full_name;
            """;

        using var reader = command.ExecuteReader();
        var students = new List<StudentListItem>();

        while (reader.Read())
        {
            students.Add(new StudentListItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("student_card_number"),
                reader.GetNullableString("email"),
                reader.GetNullableString("phone"),
                reader.GetString("status")));
        }

        return students;
    }

    public List<StudentListItem> GetStudentsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT
                s.student_id,
                s.full_name,
                g.group_name,
                g.course_number,
                s.student_card_number,
                s.email,
                s.phone,
                s.status
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            JOIN teacher_assignments ta ON ta.group_id = s.group_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var students = new List<StudentListItem>();

        while (reader.Read())
        {
            students.Add(new StudentListItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("student_card_number"),
                reader.GetNullableString("email"),
                reader.GetNullableString("phone"),
                reader.GetString("status")));
        }

        return students;
    }

    public List<StudentListItem> GetStudentsForCurator(int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                g.course_number,
                s.student_card_number,
                s.email,
                s.phone,
                s.status
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            JOIN group_curators gc ON gc.group_id = g.group_id
            WHERE gc.curator_user_id = $curator_user_id
            ORDER BY g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var students = new List<StudentListItem>();

        while (reader.Read())
        {
            students.Add(new StudentListItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("student_card_number"),
                reader.GetNullableString("email"),
                reader.GetNullableString("phone"),
                reader.GetString("status")));
        }

        return students;
    }

    public List<LookupItem> GetStudentLookups()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT student_id, full_name
            FROM students
            ORDER BY full_name;
            """;

        using var reader = command.ExecuteReader();
        var students = new List<LookupItem>();

        while (reader.Read())
        {
            students.Add(new LookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name")));
        }

        return students;
    }

    public List<LookupItem> GetStudentLookupsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT s.student_id, s.full_name
            FROM students s
            JOIN teacher_assignments ta ON ta.group_id = s.group_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var students = new List<LookupItem>();

        while (reader.Read())
        {
            students.Add(new LookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name")));
        }

        return students;
    }

    public List<LookupItem> GetStudentLookupsForCurator(int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT s.student_id, s.full_name
            FROM students s
            JOIN group_curators gc ON gc.group_id = s.group_id
            WHERE gc.curator_user_id = $curator_user_id
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var students = new List<LookupItem>();

        while (reader.Read())
        {
            students.Add(new LookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name")));
        }

        return students;
    }

    public List<StudentListItem> GetStudentsByGroup(int groupId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                g.course_number,
                s.student_card_number,
                s.email,
                s.phone,
                s.status
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            WHERE s.group_id = $group_id
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$group_id", groupId);

        using var reader = command.ExecuteReader();
        var students = new List<StudentListItem>();

        while (reader.Read())
        {
            students.Add(new StudentListItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableInt32("course_number"),
                reader.GetNullableString("student_card_number"),
                reader.GetNullableString("email"),
                reader.GetNullableString("phone"),
                reader.GetString("status")));
        }

        return students;
    }

    public int AddStudent(Student student)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO students (group_id, full_name, student_card_number, email, phone, status)
            VALUES ($group_id, $full_name, $student_card_number, $email, $phone, $status);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$group_id", student.GroupId);
        command.Parameters.AddWithValue("$full_name", student.FullName);
        command.Parameters.AddWithValue("$student_card_number", (object?)student.StudentCardNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$email", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$phone", (object?)student.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", student.Status);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void UpdateStudent(Student student)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE students
            SET group_id = $group_id,
                full_name = $full_name,
                student_card_number = $student_card_number,
                email = $email,
                phone = $phone,
                status = $status
            WHERE student_id = $student_id;
            """;
        command.Parameters.AddWithValue("$student_id", student.StudentId);
        command.Parameters.AddWithValue("$group_id", student.GroupId);
        command.Parameters.AddWithValue("$full_name", student.FullName);
        command.Parameters.AddWithValue("$student_card_number", (object?)student.StudentCardNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("$email", (object?)student.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("$phone", (object?)student.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", student.Status);
        command.ExecuteNonQuery();
    }

    public void UpdateStudentStatus(int studentId, string status)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE students
            SET status = $status
            WHERE student_id = $student_id;
            """;
        command.Parameters.AddWithValue("$student_id", studentId);
        command.Parameters.AddWithValue("$status", status);
        command.ExecuteNonQuery();
    }
}

