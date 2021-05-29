using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AquaBot.Commands.Preconditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireVoiceChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(!(context.User is IVoiceState voiceState) || voiceState.VoiceChannel is null
                ? PreconditionResult.FromError("You must be connected to a voice channel")
                : PreconditionResult.FromSuccess());
        }
    }
}