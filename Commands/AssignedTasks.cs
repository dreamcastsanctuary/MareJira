using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace MareJira.Commands;

public class AssignedTasks {
    
    private DatabaseService _db;
    
    public AssignedTasks(DatabaseService db) {
        _db = db;
    }
    
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
        
        var check = _db.SetProgress(taskName, progress);
        await command.RespondAsync("Updated progress for " + taskName + "!", ephemeral: true);
        if (check) {
            var assigner = client.GetUser(ulong.Parse(_db.GetAssigneeId(taskName))) as SocketGuildUser;
            var assigned = client.GetUser(ulong.Parse(_db.GetAssignedTo(taskName))) as SocketGuildUser;
            UserExtensions.SendMessageAsync(assigner, assigned.Nickname + " has marked their task, " + taskName + ", completed!\nGo check in with them.");
        }
    }
}