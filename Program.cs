using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MareJira.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace MareJira;
public class Program {
    
    private DiscordSocketClient _client;
    private ulong _guildId;
    private CommandHandler _commandHandler;
    private static IServiceProvider _serviceProvider;

    public static async Task Main()
        => await new Program().RunAsync();

    public async Task RunAsync() {
        
        var token = Environment.GetEnvironmentVariable("MARE_BOT_TOKEN") ?? throw new Exception("MARE_BOT_TOKEN environment variable not set.");

        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID") ?? throw new Exception("GUILD_ID environment variable not set."));
        
        _serviceProvider = CreateProvider();
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        _commandHandler = _serviceProvider.GetRequiredService<CommandHandler>();
        
        _client.Log += Log;
        _client.Ready += async () => {
            await _commandHandler.RegisterCommands(_guildId);
            
            ulong roleId = 1473508563887329447;
            var memberCount = _client.GetGuild(_guildId).Users.Count(u => !u.IsBot && u.Roles.Any(r => r.Id == roleId));
            await _client.SetActivityAsync(new CustomStatusGame("Assisting " + memberCount + " faculty members..."));
            
        };
        
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        
        await Task.Delay(-1);
    }
    
    private static Task Log(LogMessage msg) {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    
    static IServiceProvider CreateProvider() {
        
        var config = new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.Guilds
                             | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        };
        
        return new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandHandler>()
            .AddSingleton<AssigneeTasks>()
            .AddSingleton<AssignedTasks>()
            .AddSingleton<ViewTasks>()
            .AddSingleton<DatabaseService>()
            .BuildServiceProvider();
    }
}