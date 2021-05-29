using System;
using System.Threading.Tasks;
using AquaBot.Commands;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events;

namespace AquaBot.Services
{
    public class MusicService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IAudioService _audioService;

        public MusicService(DiscordSocketClient discordSocketClient, IAudioService audioService)
        {
            _discordSocketClient = discordSocketClient;
            _audioService = audioService;
            //_audioService.TrackEnd += AudioServiceOnTrackEnd;
        }

        // private async Task AudioServiceOnTrackEnd(object sender, TrackEndEventArgs e)
        // {
        //     if (!e.MayStartNext)
        //         return;
        // }
        //
        // private async Task LavaNodeOnTrackEnded(TrackEndedEventArgs e)
        // {
        //     if (!e.Reason.ShouldPlayNext())
        //         return;
        //
        //     if (e.Player is AquaPlayer aquaPlayer)
        //     {
        //         switch (aquaPlayer.RepeatMode)
        //         {
        //             case AquaPlayerRepeatModes.Track:
        //                 e.Player.Queue.EnqueueFirst(e.Track);
        //                 break;
        //             case AquaPlayerRepeatModes.Queue:
        //                 e.Player.Queue.Enqueue(e.Track);
        //                 break;
        //         }
        //     }
        //
        //     if (!e.Player.Queue.TryDequeue(out var track))
        //         return;
        //
        //     await e.Player.PlayAsync(track);
        // }
        //
        // public AquaPlayer GetPlayer(IGuild guild)
        //     => _audioService.TryGetPlayer(guild, out var player) ? player : null;
        //
        // public async Task<AquaPlayer> GetPlayerOrJoinAsync(AquaCommandContext context)
        // {
        //     var player = GetPlayer(context.Guild);
        //
        //     if (player is not null)
        //         return player;
        //
        //     if (context.User is IVoiceState voiceState
        //         && voiceState.VoiceChannel is not null)
        //     {
        //         player = await _audioService.JoinAsync(voiceState.VoiceChannel, context.Channel as ITextChannel);
        //         await player.UpdateVolumeAsync((ushort) context.Config.Music.DefaultVolume);
        //     }
        //
        //     return player;
        // }
        //
        // public async Task PlayAsync(AquaPlayer player, LavaTrack track)
        // {
        //     await player.PlayAsync(track);
        // }
    }
}