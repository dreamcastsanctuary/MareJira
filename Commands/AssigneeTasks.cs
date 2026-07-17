using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MareJira.Objects;

namespace MareJira.Commands;

public class AssigneeTasks {
    
    private DatabaseService _db;
    
    public AssigneeTasks(DatabaseService db) {
        _db = db;
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleAssignTaskCommand(SocketSlashCommand command) {
        SocketGuildUser assignee = (SocketGuildUser)command.User; 
        SocketGuildUser assignedTo = null;
        var taskName = "";
        var description = "";
        string priority = "";
        string? deadlineStr = null;
        DateTime deadline = new DateTime(9398, 12, 20);

        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "assigned_to":
                    assignedTo = ((SocketGuildUser)option.Value);
                    break;
                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "description":
                    description = option.Value.ToString();
                    break;
                case "priority":
                    priority = option.Value.ToString();
                    break;
                case "deadline":
                    deadlineStr = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (deadlineStr != null) {
            if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out deadline)) {
                await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
                return;
            }
        }

        Priority.TryParse(priority, out Priority myPriority);
        
        var embedBuilder = new EmbedBuilder()
            .WithAuthor((assignee.Nickname ?? assignee.Username) + " → " + (assignedTo.Nickname ?? assignedTo.Username))
            .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
            .WithDescription(description)
            .WithFooter("Progress: TO-DO")
            .WithColor(myPriority.GetEnumColors());

        if (deadline.Year != 9398 && deadline < DateTime.Now) {
            await command.RespondAsync("That deadline is in the past!", ephemeral: true);
            return;
        } if (deadline.Year != 9398) {
            embedBuilder.WithFooter("Progress: TO-DO\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
        }
        
        await command.RespondAsync(text: "This message has been sent to both you and the other member: ",embed: embedBuilder.Build(), ephemeral: true);
        await UserExtensions.SendMessageAsync(assignedTo, "", false, embedBuilder.Build());

        _db.CreateTask(assignedTo.Id.ToString(), assignee.Id.ToString(),DateTimeOffset.Now.ToString("MM/dd/yyyy"), deadline.ToString("MM/dd/yyyy"), taskName, description, priority, "TO-DO");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleRemoveTaskCommand(SocketSlashCommand command) {
        SocketGuildUser assignee = (SocketGuildUser)command.User;
        var taskName = "";

        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (!assignee.Id.ToString().Equals(_db.GetAssigneeId(taskName))) {
            await command.RespondAsync(text: "You aren't the creator of that task.\nTrying to get your friend off the hook? : )", ephemeral: true);
            return;
        }

        _db.DeleteTask(taskName, assignee.Id.ToString());
        await command.RespondAsync(text: "Task " + taskName + " has been deleted!", ephemeral: false);
    }
    
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task HandleForceRemoveTaskCommand(SocketSlashCommand command) {
        SocketGuildUser assignee = (SocketGuildUser)command.User;
        var taskName = "";

        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        _db.DeleteTask(taskName, assignee.Id.ToString());
        await command.RespondAsync(text: "Task " + taskName + " has been deleted!", ephemeral: false);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleUpdateTaskCommand(SocketSlashCommand command, DiscordSocketClient client) {
    
    SocketGuildUser assignee = (SocketGuildUser)command.User;
    var taskName = "";
    string? description = null;
    string? priority = null;
    string? deadlineStr = null;

    foreach (var option in command.Data.Options) {
        switch (option.Name) {
            case "task_name":
                taskName = option.Value.ToString();
                break;
            case "description":
                description = option.Value.ToString();
                break;
            case "priority":
                priority = option.Value.ToString();
                break;
            case "deadline":
                deadlineStr = option.Value.ToString();
                break;
        }
    }

    if (!assignee.Id.ToString().Equals(_db.GetAssigneeId(taskName))) {
        await command.RespondAsync("You aren't the creator of that task.\nTrying to get your friend off the hook? : )", ephemeral: true);
        return;
    }

    var existingDescription = description ?? _db.GetDescription(taskName);
    var existingPriority     = priority ?? _db.GetPriority(taskName);
    var existingDeadlineStr  = _db.GetDeadline(taskName);

    DateTime deadline;
    if (deadlineStr != null) {
        if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, DateTimeStyles.None, out deadline)) {
            await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
            return;
        }
        if (deadline < DateTime.Now && deadline.Year != 9398) {
            await command.RespondAsync("That deadline is in the past!", ephemeral: true);
            return;
        }
    } else {
        deadline = DateTime.ParseExact(existingDeadlineStr, "MM/dd/yyyy", null);
    }

    _db.UpdateTask(taskName, assignee.Id.ToString(), deadline.ToString("MM/dd/yyyy"), existingDescription, existingPriority);

    Priority.TryParse(existingPriority, out Priority myPriority);

    var embedBuilder = new EmbedBuilder()
        .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
        .WithDescription(existingDescription)
        .WithColor(myPriority.GetEnumColors());

    if (deadline.Year != 9398) {
        embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName) + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
    } else {
        embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName));
    }
    
    var user = client.GetUser(ulong.Parse(_db.GetAssignedTo(taskName)));
    
    await command.RespondAsync(text: "Task updated!", embed: embedBuilder.Build(), ephemeral: true);
    if (user != null) {
        await UserExtensions.SendMessageAsync(user, "Your task has been updated!", false, embed: embedBuilder.Build());
    }
}
    
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task HandleForceUpdateTaskCommand(SocketSlashCommand command, DiscordSocketClient client) {
    
    SocketGuildUser assignee = (SocketGuildUser)command.User;
    var taskName = "";
    string? description = null;
    string? priority = null;
    string? deadlineStr = null;

    foreach (var option in command.Data.Options) {
        switch (option.Name) {
            case "task_name":
                taskName = option.Value.ToString();
                break;
            case "description":
                description = option.Value.ToString();
                break;
            case "priority":
                priority = option.Value.ToString();
                break;
            case "deadline":
                deadlineStr = option.Value.ToString();
                break;
        }
    }

    var existingDescription = description ?? _db.GetDescription(taskName);
    var existingPriority     = priority ?? _db.GetPriority(taskName);
    var existingDeadlineStr  = _db.GetDeadline(taskName);

    DateTime deadline;
    if (deadlineStr != null) {
        if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, DateTimeStyles.None, out deadline)) {
            await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
            return;
        }
        if (deadline < DateTime.Now && deadline.Year != 9398) {
            await command.RespondAsync("That deadline is in the past!", ephemeral: true);
            return;
        }
    } else {
        deadline = DateTime.ParseExact(existingDeadlineStr, "MM/dd/yyyy", null);
    }

    _db.UpdateTask(taskName, assignee.Id.ToString(), deadline.ToString("MM/dd/yyyy"), existingDescription, existingPriority);

    Priority.TryParse(existingPriority, out Priority myPriority);

    var embedBuilder = new EmbedBuilder()
        .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
        .WithDescription(existingDescription)
        .WithColor(myPriority.GetEnumColors());

    if (deadline.Year != 9398) {
        embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName) + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
    } else {
        embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName));
    }
    
    var user = client.GetUser(ulong.Parse(_db.GetAssignedTo(taskName)));
    
    await command.RespondAsync(text: "Task updated!", embed: embedBuilder.Build(), ephemeral: true);
    if (user != null) {
        await UserExtensions.SendMessageAsync(user, "Your task has been updated!", false, embed: embedBuilder.Build());
    }
    }
}