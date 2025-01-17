﻿namespace Manul;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Exceptions;
using SecretModules;
using Services;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

public class Program
{
    public static Config Config;
    public static readonly List<VotingData> VotingData = new ();
    public static readonly List<SecretModule> SecretModules = new ()
    {
        new GreetingsModule(),
        new WolfModule(),
        new MyDreamsModule()
    };

    public static void Main(string[] args)
    {
        try
        {
            LoggingService.PrepareLogger();
            CheckFile(Config.ConfigFilename);
            Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Config.ConfigFilename));
            new Program().MainAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Log.Fatal("{Message}",exception.Message);
        }
    }

    private async Task MainAsync()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<LoggingService>();
        provider.GetRequiredService<CommandHandler>();
            
        await provider.GetRequiredService<StartupService>().StartAsync();
        await Task.Delay(Timeout.Infinite);
    }

    private static void CheckFile(in string filename)
    {
        var fileInfo = new FileInfo(filename);
            
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"File {filename} not found.");
        }

        if (fileInfo.Length <= 0)
        {
            throw new EmptyFileException($"File {filename} is empty.");
        }
    }
        
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = Config.MessageCacheSize,
                AlwaysDownloadUsers = true,
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMembers |
                                 GatewayIntents.GuildMessageReactions |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.GuildVoiceStates
            } ))
            .AddSingleton(new CommandService(new CommandServiceConfig
                { LogLevel = LogSeverity.Verbose, DefaultRunMode = RunMode.Async } ))
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<LoggingService>();
    }
}