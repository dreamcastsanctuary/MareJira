using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace MareJira.Commands;

public class AssignedTasks {
    
    private DatabaseService _db;
    
    public AssignedTasks(DatabaseService db) {
        _db = db;
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleUpdateProgressCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        var taskName = "";
        var progress = "";
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "progress":
                    progress = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        var (taskFound, isNowCompleted) = _db.SetProgress(taskName, progress);

        if (!taskFound) {
            await command.RespondAsync($"No task found with the name **{taskName}**. Nothing was updated.", ephemeral: true);
            return;
        }

        await command.RespondAsync("Updated progress for " + taskName + "!", ephemeral: true);
        if (isNowCompleted) {
            var assigner = client.GetUser(ulong.Parse(_db.GetAssigneeId(taskName))) as SocketGuildUser;
            var assigned = client.GetUser(ulong.Parse(_db.GetAssignedTo(taskName))) as SocketGuildUser;
            if (assigner != null && assigned != null) {
                var assignedName = assigned.Nickname ?? assigned.Username;
                await UserExtensions.SendMessageAsync(assigner, assignedName + " has marked their task, " + taskName + ", completed!\nGo check in with them.");
            }
        }
    }
}