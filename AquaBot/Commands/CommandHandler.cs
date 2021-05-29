using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AquaBot.Commands.TypeReaders;
using AquaBot.Data;
using AquaBot.Models;
using AquaBot.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaBot.Commands
{
    // Reference: https://docs.stillu.cc/guides/commands/intro.html
    public class CommandHandler
    {
        private readonly ILogger<CommandHandler> _logger;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly GuildConfigService _guildConfigService;
        private readonly IOptions<BotConfig> _config;
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<ulong, GuildConfig> _guildConfigCache
            = new ConcurrentDictionary<ulong, GuildConfig>();

        public CommandHandler(ILogger<CommandHandler> logger, DiscordSocketClient client, CommandService commands,
            GuildConfigService guildConfigService,
            IOptions<BotConfig> config,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _client = client;
            _commands = commands;
            _guildConfigService = guildConfigService;
            _config = config;
            _serviceProvider = serviceProvider;
            _commands.Log += CommandsOnLog;
        }

        private Task CommandsOnLog(LogMessage e)
        {
            var level = e.Severity switch
            {
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Critical => LogLevel.Critical,
                _ => LogLevel.None
            };

            if (e.Exception is not null)
                _logger.Log(level, e.Exception, "[{Source}] {Message}", e.Source, e.Message);
            else
                _logger.Log(level, "[{Source}] {Message}", e.Source, e.Message);

            return Task.CompletedTask;
        }

        public async Task Init()
        {
            _commands.AddTypeReader<bool>(new BooleanTypeReader(), true);
            // _commands.AddTypeReader<IUser>(new AquaUserTypeReader<IUser>(), true);
            // _commands.AddTypeReader<IGuildUser>(new AquaUserTypeReader<IGuildUser>(), true);

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            using var scope = _serviceProvider.CreateScope();
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),
                scope.ServiceProvider);
        }

        public void Register()
        {
            _client.MessageReceived += HandleCommandAsync;
        }

        public void UnRegister()
        {
            _client.MessageReceived -= HandleCommandAsync;
        }

        private Task HandleCommandAsync(SocketMessage messageParam)
        {
            _ = Task.Run(async () =>
            {
                // Don't process the command if it was a system message
                if (!(messageParam is SocketUserMessage message))
                    return;

                // Create a number to track where the prefix ends and the command begins
                var argPos = 0;

                var guildConfig = (GuildConfig) null!;
                if (message.Channel is SocketGuildChannel guildChannel)
                    guildConfig = await _guildConfigService.Get(guildChannel.Guild);

                // Determine if the message is a command based on the prefix and make sure no bots trigger commands
                if (!(message.HasStringPrefix(guildConfig?.Prefix ?? _config.Value.DefaultPrefix, ref argPos) ||
                      message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                    message.Author.IsBot)
                    return;

                using var activity = new Activity("AquaBot Command Execute");
                activity.Start();

                // Create a WebSocket-based command context based on the message
                var context = new AquaCommandContext(_client, message, _serviceProvider, guildConfig);

                // Execute the command with the command context we just
                // created, along with the service provider for precondition checks.

                using var scope = _serviceProvider.CreateScope();
                try
                {
                    var result = await _commands.ExecuteAsync(
                        context,
                        argPos,
                        scope.ServiceProvider);

                    if (!result.IsSuccess)
                    {
                        if (result.Error != CommandError.UnknownCommand)
                        {
                            await context.Channel.SendMessageAsync(embed: new EmbedBuilder()
                                .WithDescription(result.ErrorReason ?? "Unknown error")
                                .WithFooter(result.Error == CommandError.Exception ? activity.Id : null)
                                .WithColor(255, 0, 0)
                                .Build());
                        }

                        _logger.LogWarning("{Error} {ErrorReason}", result.Error, result.ErrorReason);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "An error occurred while executing command");
                    if ((await ((IGuild) context.Guild).GetCurrentUserAsync()).GuildPermissions.Has(GuildPermission
                        .SendMessages))
                        await message.ReplyAsync(embed: new EmbedBuilder()
                            .WithDescription(
                                $"An error occurred while executing command")
                            .WithFooter($"{Activity.Current?.Id}")
                            .WithColor(255, 0, 0)
                            .Build());
                }
            });

            return Task.CompletedTask;
        }
    }
}