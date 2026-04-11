using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using MareJira.Objects;

namespace MareJira.Commands;

public class ViewTasks {
    
    private DatabaseService _db;
    
    public ViewTasks(DatabaseService db) {
        _db = db;
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleViewTaskCommand(SocketSlashCommand command, DiscordSocketClient client) {
        var taskName = command.Data.Options.FirstOrDefault(o => o.Name == "task_name")?.Value?.ToString();

        if (string.IsNullOrEmpty(taskName)) {
            await command.RespondAsync("Please provide a task name.", ephemeral: true);
            return;
        }

        var task = _db.ViewTask(taskName);

        if (task == null) {
            await command.RespondAsync($"No task found with the name **{taskName}**.", ephemeral: true);
            return;
        }

        Priority.TryParse(task.Priority, out Priority myPriority);

        var assigned = client.GetUser(ulong.Parse(task.AssignedId)) as IUser;
        var assignee = client.GetUser(ulong.Parse(task.AssigneeId)) as IUser;
        var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);
        
        var embedBuilder = new EmbedBuilder()
            .WithAuthor(assignee?.Username + " → " + assigned?.Username)
            .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
            .WithDescription(task.Description ?? "No description.")
            .WithColor(myPriority.GetEnumColors());

        if (deadline.Year.Equals(9398)) {
            embedBuilder.WithFooter("Progress: " + task.Progress);
        } else
        {
            embedBuilder.WithFooter("Progress: " + task.Progress + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
        }

        await command.DeferAsync(ephemeral: true);
        await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleViewAllCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        var tasks = _db.ViewAll(command.User.Id.ToString());

        if (tasks == null || tasks.Count == 0) {
            await command.RespondAsync("No tasks found.", ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: false);

        foreach (TaskModel task in tasks) {
            
            Priority.TryParse(task.Priority, out Priority myPriority);

            var assigned = client.GetUser(ulong.Parse(task.AssignedId)) as IUser;
            var assignee = client.GetUser(ulong.Parse(task.AssigneeId)) as IUser;
            var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);
            
            var embedBuilder = new EmbedBuilder()
                .WithAuthor(assignee?.Username + " → " + assigned?.Username)
                .WithTitle(task.TaskName + "     " + myPriority.GetEnumDescription())
                .WithDescription(task.Description ?? "No description.")
                .WithColor(myPriority.GetEnumColors());
            
            if (deadline.Year.Equals(9398)) {
                embedBuilder.WithFooter("Progress: " + task.Progress);
            } else
            {
                embedBuilder.WithFooter("Progress: " + task.Progress + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
            }

            await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: false);
        }
    }

    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task HandleViewEveryoneCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        var tasks = _db.ViewEveryone();

        if (tasks == null || tasks.Count == 0) {
            await command.RespondAsync("No tasks found.");
            return;
        }

        await command.DeferAsync(ephemeral: true);

        foreach (TaskModel task in tasks)
        {

            Priority.TryParse(task.Priority, out Priority myPriority);

            var assigned = client.GetUser(ulong.Parse(task.AssignedId)) as IUser;
            var assignee = client.GetUser(ulong.Parse(task.AssigneeId)) as IUser;
            var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(assignee?.Username + " → " + assigned?.Username)
                .WithTitle(task.TaskName + "     " + myPriority.GetEnumDescription())
                .WithDescription(task.Description ?? "No description.")
                .WithColor(myPriority.GetEnumColors());

            if (deadline.Year.Equals(9398))
            {
                embedBuilder.WithFooter("Progress: " + task.Progress);
            }
            else
            {
                embedBuilder.WithFooter("Progress: " + task.Progress + "\nDeadline: " + deadline.DayOfWeek + ", " +
                                        deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
            }

            await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
    }
}