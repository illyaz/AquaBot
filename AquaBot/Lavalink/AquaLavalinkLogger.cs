using System;
using Microsoft.Extensions.Logging;
using ILogger = Lavalink4NET.Logging.ILogger;
using LavaLogLevel = Lavalink4NET.Logging.LogLevel;

namespace AquaBot.Lavalink
{
    public class AquaLavalinkLogger : ILogger
    {
        private Microsoft.Extensions.Logging.ILogger _logger;

        public AquaLavalinkLogger(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("Lavalink");
        }

        public void Log(object source, string message, LavaLogLevel level = LavaLogLevel.Information,
            Exception? exception = null)
        {
            _logger.Log(level switch
            {
                LavaLogLevel.Trace => LogLevel.Trace,
                LavaLogLevel.Debug => LogLevel.Debug,
                LavaLogLevel.Information => LogLevel.Information,
                LavaLogLevel.Warning => LogLevel.Warning,
                LavaLogLevel.Error => LogLevel.Critical,
                _ => LogLevel.None
            }, exception, "[{Source}] {Message}", source, message);
        }
    }
}