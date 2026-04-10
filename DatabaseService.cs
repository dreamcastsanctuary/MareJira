using System;
using System.Collections.Generic;
using Discord;
using MareJira.Objects;
using Microsoft.Data.Sqlite;

namespace MareJira;

public class DatabaseService {
    
    private readonly string _connectionString;

    public DatabaseService(string? dbPath = null) {
        dbPath ??= Environment.GetEnvironmentVariable("DB_PATH") ?? "bot.db";
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase() {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Task (
                TaskName TEXT PRIMARY KEY,
                AssignedId TEXT NOT NULL,
                AssigneeId TEXT NOT NULL,
                DateAssigned TEXT NOT NULL,
                Deadline TEXT NOT NULL,
                Description TEXT,
                Priority TEXT NOT NULL,
                Progress TEXT NOT NULL
            );";
        
        command.ExecuteNonQuery();
    }

    public void CreateTask(string assignedTo, string assignee, 
        string dateAssigned, string deadline, string taskName,
        string description, string priority, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();

        command.CommandText =
            @"INSERT INTO Task (TaskName, AssignedId, AssigneeId, DateAssigned, Deadline, Description, Priority, Progress) VALUES ($taskName, $assignedTo, $assignee, $dateAssigned, $deadline, $description, $priority, $progress);";
        
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$assignedTo", assignedTo);
        command.Parameters.AddWithValue("$assignee", assignee);
        command.Parameters.AddWithValue("$dateAssigned", dateAssigned);
        command.Parameters.AddWithValue("$deadline", deadline);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$priority", priority);
        command.Parameters.AddWithValue("$progress", progress);
        
        command.ExecuteNonQuery();
    }

    public void DeleteTask(string taskName, string assignee) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        
        command.CommandText =
            @"DELETE FROM Task WHERE TaskName = $taskName AND AssigneeId = $assigneeId;";
        
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$assigneeId", assignee);
        
        command.ExecuteNonQuery();
    }
    
    public void UpdateTask(string taskName, 
                           string assignee,
                           string? deadline = null,
                           string? description = null,
                           string? priority = null) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();

        var setClauses = new List<string>();

        if (deadline != null) {
            setClauses.Add("Deadline = $deadline");
            command.Parameters.AddWithValue("$deadline", deadline);
        }
        if (description != null) {
            setClauses.Add("Description = $description");
            command.Parameters.AddWithValue("$description", description);
        }
        if (priority != null) {
            setClauses.Add("Priority = $priority");
            command.Parameters.AddWithValue("$priority", priority);
        }

        if (setClauses.Count == 0) return;

        command.CommandText = $@"
        UPDATE Task
        SET {string.Join(", ", setClauses)}
        WHERE TaskName = $taskName";

        command.Parameters.AddWithValue("$taskName", taskName);

        command.ExecuteNonQuery();
    }

    public string GetAssigneeId(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssigneeId FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public bool SetProgress(string taskName, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Task SET Progress = $progress WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$progress", progress);

        if (progress.Equals("COMPLETED")) {
            return true;
        }
        
        return false;
    }

    public string GetProgress(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Progress FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDescription(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Description FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetPriority(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Priority FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDeadline(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Deadline FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetAssignedTo(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssignedId FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public TaskModel? ViewTask(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task WHERE TaskName = $taskName;";
        command.Parameters.AddWithValue("$taskName", taskName);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new TaskModel {
            TaskName     = reader.GetString(reader.GetOrdinal("TaskName")),
            AssignedId   = reader.GetString(reader.GetOrdinal("AssignedId")),
            AssigneeId   = reader.GetString(reader.GetOrdinal("AssigneeId")),
            DateAssigned = reader.GetString(reader.GetOrdinal("DateAssigned")),
            Deadline     = reader.GetString(reader.GetOrdinal("Deadline")),
            Description  = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Priority     = reader.GetString(reader.GetOrdinal("Priority")),
            Progress     = reader.GetString(reader.GetOrdinal("Progress"))
        };
    }
    
    public List<TaskModel?> ViewAll(string assignedId) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task WHERE AssignedId = $assignedId;";
        command.Parameters.AddWithValue("$assignedId", assignedId);

        using var reader = command.ExecuteReader();
        
        if (!reader.HasRows) return null;
        
        List<TaskModel> tasks = new List<TaskModel>();
        while (reader.Read())
        {
            tasks.Add(new TaskModel
            {
                TaskName     = reader.GetString(reader.GetOrdinal("TaskName")),
                AssignedId   = reader.GetString(reader.GetOrdinal("AssignedId")),
                AssigneeId   = reader.GetString(reader.GetOrdinal("AssigneeId")),
                DateAssigned = reader.GetString(reader.GetOrdinal("DateAssigned")),
                Deadline     = reader.GetString(reader.GetOrdinal("Deadline")),
                Description  = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                Priority     = reader.GetString(reader.GetOrdinal("Priority")),
                Progress     = reader.GetString(reader.GetOrdinal("Progress"))
            });
        }
        return tasks;
    }
    
    public List<TaskModel?> ViewEveryone() {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task;";

        using var reader = command.ExecuteReader();
        
        if (!reader.HasRows) return null;
        
        List<TaskModel> tasks = new List<TaskModel>();
        while (reader.Read())
        {
            tasks.Add(new TaskModel
            {
                TaskName     = reader.GetString(reader.GetOrdinal("TaskName")),
                AssignedId   = reader.GetString(reader.GetOrdinal("AssignedId")),
                AssigneeId   = reader.GetString(reader.GetOrdinal("AssigneeId")),
                DateAssigned = reader.GetString(reader.GetOrdinal("DateAssigned")),
                Deadline     = reader.GetString(reader.GetOrdinal("Deadline")),
                Description  = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                Priority     = reader.GetString(reader.GetOrdinal("Priority")),
                Progress     = reader.GetString(reader.GetOrdinal("Progress"))
            });
        }
        return tasks;
        
    }
}