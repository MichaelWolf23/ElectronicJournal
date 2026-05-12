using System;
using System.Collections.Generic;
using System.Linq;
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
        return GetStudentLookupItems()
            .Select(student => new LookupItem(student.Id, student.DisplayName))
            .ToList();
    }

    public List<LookupItem> GetStudentLookupsForLesson(int lessonId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                s.student_card_number
            FROM lessons l
            JOIN teacher_assignments ta ON ta.assignment_id = l.assignment_id
            JOIN students s ON s.group_id = ta.group_id
            JOIN groups g ON g.group_id = s.group_id
            WHERE l.lesson_id = $lesson_id
            ORDER BY s.full_name;
            """;
        command.Parameters.AddWithValue("$lesson_id", lessonId);

        using var reader = command.ExecuteReader();
        var students = new List<LookupItem>();

        while (reader.Read())
        {
            var card = reader.GetNullableString("student_card_number");
            var name = string.IsNullOrWhiteSpace(card)
                ? $"{reader.GetString("full_name")} — {reader.GetString("group_name")}"
                : $"{reader.GetString("full_name")} — {reader.GetString("group_name")}, билет {card}";
            students.Add(new LookupItem(reader.GetInt32("student_id"), name));
        }

        return students;
    }

    public List<StudentLookupItem> GetStudentLookupItems()
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                s.student_card_number
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            ORDER BY g.group_name, s.full_name;
            """;

        using var reader = command.ExecuteReader();
        var students = new List<StudentLookupItem>();

        while (reader.Read())
        {
            students.Add(new StudentLookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableString("student_card_number")));
        }

        return students;
    }

    public List<LookupItem> GetStudentLookupsForTeacher(int teacherUserId)
    {
        return GetStudentLookupItemsForTeacher(teacherUserId)
            .Select(student => new LookupItem(student.Id, student.DisplayName))
            .ToList();
    }

    public List<StudentLookupItem> GetStudentLookupItemsForTeacher(int teacherUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT s.student_id, s.full_name
                , g.group_name,
                s.student_card_number
            FROM students s
            JOIN teacher_assignments ta ON ta.group_id = s.group_id
            JOIN groups g ON g.group_id = s.group_id
            WHERE ta.teacher_user_id = $teacher_user_id
            ORDER BY g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$teacher_user_id", teacherUserId);

        using var reader = command.ExecuteReader();
        var students = new List<StudentLookupItem>();

        while (reader.Read())
        {
            students.Add(new StudentLookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableString("student_card_number")));
        }

        return students;
    }

    public List<LookupItem> GetStudentLookupsForCurator(int curatorUserId)
    {
        return GetStudentLookupItemsForCurator(curatorUserId)
            .Select(student => new LookupItem(student.Id, student.DisplayName))
            .ToList();
    }

    public List<StudentLookupItem> GetStudentLookupItemsForCurator(int curatorUserId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.student_id,
                s.full_name,
                g.group_name,
                s.student_card_number
            FROM students s
            JOIN groups g ON g.group_id = s.group_id
            JOIN group_curators gc ON gc.group_id = s.group_id
            WHERE gc.curator_user_id = $curator_user_id
            ORDER BY g.group_name, s.full_name;
            """;
        command.Parameters.AddWithValue("$curator_user_id", curatorUserId);

        using var reader = command.ExecuteReader();
        var students = new List<StudentLookupItem>();

        while (reader.Read())
        {
            students.Add(new StudentLookupItem(
                reader.GetInt32("student_id"),
                reader.GetString("full_name"),
                reader.GetString("group_name"),
                reader.GetNullableString("student_card_number")));
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

    public void DeleteStudent(int studentId)
    {
        using var connection = DatabaseService.CreateConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                DELETE FROM grade_retakes
                WHERE original_grade_id IN (
                    SELECT grade_id
                    FROM grades
                    WHERE student_id = $student_id
                );
                """;
            command.Parameters.AddWithValue("$student_id", studentId);
            command.ExecuteNonQuery();
        }

        foreach (var sql in new[]
        {
            "DELETE FROM curator_notifications WHERE student_id = $student_id;",
            "DELETE FROM final_grades WHERE student_id = $student_id;",
            "DELETE FROM grades WHERE student_id = $student_id;",
            "DELETE FROM attendance WHERE student_id = $student_id;",
            "DELETE FROM students WHERE student_id = $student_id;"
        })
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            command.Parameters.AddWithValue("$student_id", studentId);
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}

