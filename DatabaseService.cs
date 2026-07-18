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
                Progress TEXT NOT NULL,
                ReminderStage TEXT NOT NULL DEFAULT 'NONE',
                LastOverdueReminderDate TEXT
            );";
        
        command.ExecuteNonQuery();

        // Existing databases created before the reminder feature won't have these
        // columns yet, since CREATE TABLE IF NOT EXISTS doesn't alter existing tables.
        EnsureColumnExists(connection, "ReminderStage", "TEXT NOT NULL DEFAULT 'NONE'");
        EnsureColumnExists(connection, "LastOverdueReminderDate", "TEXT");
    }

    private static void EnsureColumnExists(SqliteConnection connection, string columnName, string columnDefinition) {
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "PRAGMA table_info(Task);";

        bool exists = false;
        using (var reader = checkCommand.ExecuteReader()) {
            while (reader.Read()) {
                if (string.Equals(reader.GetString(reader.GetOrdinal("name")), columnName, StringComparison.OrdinalIgnoreCase)) {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists) {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"ALTER TABLE Task ADD COLUMN {columnName} {columnDefinition};";
            alterCommand.ExecuteNonQuery();
        }
    }

    public void CreateTask(string assignedTo, string assignee, 
        string dateAssigned, string deadline, string taskName,
        string description, string priority, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();

        command.CommandText =
            @"INSERT INTO Task (TaskName, AssignedId, AssigneeId, DateAssigned, Deadline, Description, Priority, Progress, ReminderStage, LastOverdueReminderDate)
              VALUES ($taskName, $assignedTo, $assignee, $dateAssigned, $deadline, $description, $priority, $progress, 'NONE', NULL);";
        
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
            @"DELETE FROM Task WHERE TaskName = $taskName COLLATE NOCASE AND AssigneeId = $assigneeId;";
        
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
        
        bool deadlineChanged = false;
        if (deadline != null) {
            var currentDeadlineCommand = connection.CreateCommand();
            currentDeadlineCommand.CommandText = "SELECT Deadline FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
            currentDeadlineCommand.Parameters.AddWithValue("$taskName", taskName);
            var currentDeadline = Convert.ToString(currentDeadlineCommand.ExecuteScalar()) ?? "";
            deadlineChanged = !string.Equals(currentDeadline, deadline, StringComparison.Ordinal);
        }

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
        if (deadlineChanged) {
            setClauses.Add("ReminderStage = $reminderStage");
            command.Parameters.AddWithValue("$reminderStage", "NONE");
            setClauses.Add("LastOverdueReminderDate = $lastOverdueReminderDate");
            command.Parameters.AddWithValue("$lastOverdueReminderDate", DBNull.Value);
        }

        if (setClauses.Count == 0) return;

        command.CommandText = $@"
        UPDATE Task
        SET {string.Join(", ", setClauses)}
        WHERE TaskName = $taskName COLLATE NOCASE";

        command.Parameters.AddWithValue("$taskName", taskName);

        command.ExecuteNonQuery();
    }

    // Returns up to `maxResults` task names assigned to the given user, optionally filtered
    // to names containing `filter` (case-insensitive). Used to populate the task_name
    // autocomplete list on /updateprogress.
    public List<string> GetTaskNamesForAssignedUser(string assignedId, string? filter = null, int maxResults = 25) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT TaskName FROM Task
            WHERE AssignedId = $assignedId
              AND TaskName LIKE $likeFilter ESCAPE '\' COLLATE NOCASE
            ORDER BY TaskName
            LIMIT $limit;";

        command.Parameters.AddWithValue("$assignedId", assignedId);
        command.Parameters.AddWithValue("$likeFilter", "%" + EscapeLikePattern(filter ?? "") + "%");
        command.Parameters.AddWithValue("$limit", maxResults);

        using var reader = command.ExecuteReader();

        var names = new List<string>();
        while (reader.Read()) {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    private static string EscapeLikePattern(string input) {
        return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }

    public string GetAssigneeId(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssigneeId FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    // Returns (taskFound, isNowCompleted). taskFound is false if no task with that name exists,
    // which lets the caller tell the user the update didn't actually happen instead of falsely
    // reporting success.
    public (bool taskFound, bool isNowCompleted) SetProgress(string taskName, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Task SET Progress = $progress WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$progress", progress);

        var rowsAffected = command.ExecuteNonQuery();
        var taskFound = rowsAffected > 0;

        return (taskFound, taskFound && progress.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase));
    }

    public string GetProgress(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Progress FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDescription(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Description FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetPriority(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Priority FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDeadline(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Deadline FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetAssignedTo(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssignedId FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    // Updates the reminder-tracking state for a task. Called by ReminderService after it
    // sends a reminder, so it knows not to send that same reminder again.
    public void UpdateReminderState(string taskName, string reminderStage, string? lastOverdueReminderDate) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Task SET ReminderStage = $stage, LastOverdueReminderDate = $lastOverdue WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$stage", reminderStage);
        command.Parameters.AddWithValue("$lastOverdue", (object?)lastOverdueReminderDate ?? DBNull.Value);
        command.Parameters.AddWithValue("$taskName", taskName);

        command.ExecuteNonQuery();
    }

    // Returns every task that isn't completed yet, for the reminder service to scan
    // through and decide whether any deadline-based reminders are due.
    public List<TaskModel> GetActiveTasksWithDeadlines() {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Task WHERE Progress != 'COMPLETED';";

        using var reader = command.ExecuteReader();

        var tasks = new List<TaskModel>();
        while (reader.Read()) {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
    }
    
    public TaskModel? ViewTask(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return ReadTask(reader);
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
            tasks.Add(ReadTask(reader));
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
            tasks.Add(ReadTask(reader));
        }
        return tasks;
        
    }

    private static TaskModel ReadTask(SqliteDataReader reader) {
        return new TaskModel {
            TaskName     = reader.GetString(reader.GetOrdinal("TaskName")),
            AssignedId   = reader.GetString(reader.GetOrdinal("AssignedId")),
            AssigneeId   = reader.GetString(reader.GetOrdinal("AssigneeId")),
            DateAssigned = reader.GetString(reader.GetOrdinal("DateAssigned")),
            Deadline     = reader.GetString(reader.GetOrdinal("Deadline")),
            Description  = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Priority     = reader.GetString(reader.GetOrdinal("Priority")),
            Progress     = reader.GetString(reader.GetOrdinal("Progress")),
            ReminderStage = reader.IsDBNull(reader.GetOrdinal("ReminderStage")) ? "NONE" : reader.GetString(reader.GetOrdinal("ReminderStage")),
            LastOverdueReminderDate = reader.IsDBNull(reader.GetOrdinal("LastOverdueReminderDate")) ? null : reader.GetString(reader.GetOrdinal("LastOverdueReminderDate"))
        };
    }
}