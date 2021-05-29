using System;
using System.Threading.Tasks;
using AquaBot.Lavalink;
using Discord;
using Discord.Commands;
using Lavalink4NET;
using Microsoft.Extensions.DependencyInjection;

namespace AquaBot.Commands.Preconditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireMusicListeningAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            var audioService = services.GetRequiredService<IAudioService>();
            var player = audioService.GetPlayer<AquaLavalinkPlayer>(context.Guild.Id);

            if (player?.VoiceChannelId is null)
                return PreconditionResult.FromError("There is no player active in this guild");

            if (!(context.User is IVoiceState voiceState) || voiceState.VoiceChannel is null)
                return PreconditionResult.FromError("You must be connected to a voice channel");

            if (voiceState.IsDeafened || voiceState.IsSelfDeafened ||
                voiceState.VoiceChannel.Id != player.VoiceChannelId)
                return PreconditionResult.FromError(
                    $"You must be listening in **{(await context.Client.GetChannelAsync(player.VoiceChannelId.Value)).Name}**");

            if(player.MessageChannel.Id != context.Message.Channel.Id)
                return PreconditionResult.FromError(
                    $"You must be execute command in **{player.MessageChannel.Name}**");
            
            return PreconditionResult.FromSuccess();
        }
    }
}