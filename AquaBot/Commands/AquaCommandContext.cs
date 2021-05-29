using System;
using AquaBot.Data;
using Discord.Commands;
using Discord.WebSocket;

namespace AquaBot.Commands
{
    public class AquaCommandContext : SocketCommandContext
    {
        public AquaCommandContext(DiscordSocketClient client, SocketUserMessage msg, IServiceProvider serviceProvider,
            GuildConfig? config)
            : base(client, msg)
        {
            ServiceProvider = serviceProvider;
            Config = config;
        }

        public IServiceProvider ServiceProvider { get; }
        public GuildConfig? Config { get; }
    }
}