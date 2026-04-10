using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MareJira.Commands;
using Newtonsoft.Json;

namespace MareJira;
public class CommandHandler {
    
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _serviceProvider;
    private AssigneeTasks _assigneeTasks;
    private AssignedTasks _assignedTasks;
    private ViewTasks _viewTasks;
    private DatabaseService _db;

    public CommandHandler(DiscordSocketClient client, 
                          IServiceProvider serviceProvider,
                          AssigneeTasks assigneeTasks,
                          AssignedTasks assignedTasks,
                          ViewTasks viewTasks, 
                          DatabaseService db) {
        _client = client;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _serviceProvider = serviceProvider;
        _assigneeTasks = assigneeTasks;
        _assignedTasks = assignedTasks;
        _viewTasks = viewTasks;
        _db = db;
    }
    public async Task RegisterCommands(ulong guildId) {
        
        var guild = _client.GetGuild(guildId);
        List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();
        
        commands.Add(new SlashCommandBuilder()
            .WithName("assigntask")
            .WithDescription("Assigns a task to the given user.")
            .AddOption("assigned_to", ApplicationCommandOptionType.User, "The member receiving the task", isRequired: true)
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                        .WithName("priority").WithDescription("The priority of the task").WithRequired(true)
                        .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                        .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\"", isRequired: false));

        commands.Add(new SlashCommandBuilder()
            .WithName("removetask")
            .WithDescription("Removes the given task from the database.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true));

        commands.Add(new SlashCommandBuilder()
            .WithName("forceremovetask")
            .WithDescription("Removes the given task from the database.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("updatetask")
            .WithDescription("Updates the given task.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("priority").WithDescription("The priority of the task")
                .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\""));
            
        commands.Add(new SlashCommandBuilder()
            .WithName("forceupdatetask")
            .WithDescription("Updates the given task.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("priority").WithDescription("The priority of the task")
                .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\""));
        

        
        commands.Add(new SlashCommandBuilder()
            .WithName("viewtask")
            .WithDescription("Views a task.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true));

        commands.Add(new SlashCommandBuilder()
            .WithName("viewall")
            .WithDescription("Views all tasks."));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("vieweveryone")
            .WithDescription("Views everyone's tasks."));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("updateprogress")
            .WithDescription("Updates the given task's progress.")
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("progress").WithDescription("The progress of the task").WithRequired(true)
                .AddChoice("TO-DO", "TO-DO").AddChoice("IN PROGRESS", "IN PROGRESS").AddChoice("COMPLETED", "COMPLETED")
                .WithType(ApplicationCommandOptionType.String)));
        
        
            
        
        try {
            var builtCommands = commands.Select(c => (ApplicationCommandProperties)c.Build()).ToArray();
            await ((IGuild)guild).BulkOverwriteApplicationCommandsAsync(builtCommands);
        } catch (ApplicationCommandException exception) {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    private async Task SlashCommandHandler(SocketSlashCommand command) {
        switch(command.Data.Name) {
            case "assigntask":
                await _assigneeTasks.HandleAssignTaskCommand(command);
                break;
            case "removetask":
                await _assigneeTasks.HandleRemoveTaskCommand(command);
                break;
            case "forceremovetask":
                await _assigneeTasks.HandleForceRemoveTaskCommand(command);
                break;
            case "updatetask":
                await _assigneeTasks.HandleUpdateTaskCommand(command, _client);
                break;
            case "forceupdatetask":
                await _assigneeTasks.HandleForceUpdateTaskCommand(command, _client);
                break;
            case "viewtask":
                await _viewTasks.HandleViewTaskCommand(command, _client);
                break;
            case "viewall":
                await _viewTasks.HandleViewAllCommand(command, _client);
                break;
            case "vieweveryone":
                await _viewTasks.HandleViewEveryoneCommand(command, _client);
                break;
            case "updateprogress":
                await _assignedTasks.HandleUpdateProgressCommand(command, _client);
                break;
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                break;
        }
    }
}