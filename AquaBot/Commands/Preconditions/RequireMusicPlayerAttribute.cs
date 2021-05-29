using System;
using System.Threading.Tasks;
using AquaBot.Lavalink;
using Discord.Commands;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;

namespace AquaBot.Commands.Preconditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireMusicPlayerAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var audioService = services.GetRequiredService<IAudioService>();
            return Task.FromResult(audioService.GetPlayer<AquaLavalinkPlayer>(context.Guild.Id) is null
                ? PreconditionResult.FromError("There is no player active in this guild")
                : PreconditionResult.FromSuccess());
        }
    }
}