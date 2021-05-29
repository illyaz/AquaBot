using System;
using System.Threading;
using System.Threading.Tasks;
using AquaBot.Commands;
using AquaBot.Models;
using AquaBot.Services;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AquaBot.HostedServices
{
    public class DiscordBot : IHostedService
    {
        private readonly ILogger<DiscordBot> _logger;
        private readonly DiscordSocketClient _client;
        private readonly IAudioService _audioService;
        private readonly IOptions<BotConfig> _options;
        private readonly CommandHandler _commandHandler;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly SymbolService _symbol;
        private bool _isCommandHandlerInit = false;

        public DiscordBot(
            ILogger<DiscordBot> logger,
            DiscordSocketClient client,
            IAudioService audioService,
            IOptions<BotConfig> options,
            CommandHandler commandHandler,
            IHostApplicationLifetime applicationLifetime,
            SymbolService symbol)
        {
            _logger = logger;
            _client = client;
            _audioService = audioService;
            _options = options;
            _commandHandler = commandHandler;
            _applicationLifetime = applicationLifetime;
            _symbol = symbol;
            _client.Log += ClientOnLog;
            _logger.LogInformation("Created");
        }

        private Task ClientOnLog(LogMessage e)
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

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting bot ...");
                var tcs = new TaskCompletionSource();

                Task OnReady()
                {
                    try
                    {
                        _logger.LogInformation("Started: {CurrentUser}, Shard: {ShardId}",
                            _client.CurrentUser.ToString(),
                            _client.ShardId);
                        return Task.CompletedTask;
                    }
                    finally
                    {
                        tcs.SetResult();
                    }
                }

                _client.Ready += OnReady;

                if (!_isCommandHandlerInit)
                {
                    await _commandHandler.Init();
                    _isCommandHandlerInit = true;
                }

                _commandHandler.Register();

                await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
                await _client.StartAsync();
                await tcs.Task;
                await _audioService.InitializeAsync();
                await _client.SetActivityAsync(new Game("!help", ActivityType.Playing));
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Unexpected exception");
                _applicationLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _commandHandler.UnRegister();
            return Task.CompletedTask;
        }
    }
}