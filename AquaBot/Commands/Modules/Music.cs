using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AquaBot.Commands.Attributes;
using AquaBot.Commands.Preconditions;
using AquaBot.Lavalink;
using AquaBot.Services;
using Discord;
using Discord.Commands;
using Lavalink4NET;
using Lavalink4NET.Player;
using Lavalink4NET.Rest;
using Microsoft.Extensions.Logging;

namespace AquaBot.Commands.Modules
{
    [Group("music"), Alias("m"), RequireContext(ContextType.Guild)]
    public class Music : AquaModule
    {
        private readonly ILogger<Music> _logger;
        private readonly Services.SymbolService _symbol;
        private readonly DiscordService _discordService;
        private readonly IAudioService _audioService;

        public Music(ILogger<Music> logger, Services.SymbolService symbol,
            DiscordService discordService,
            IAudioService audioService)
        {
            _logger = logger;
            _symbol = symbol;
            _discordService = discordService;
            _audioService = audioService;
        }


        protected override void AfterExecute(CommandInfo command)
        {
            base.AfterExecute(command);
            if (Context.Config?.Music is null
                || !Context.Config.Music.DeleteUserCommand)
                return;

            Task.Run(async () =>
            {
                var user = await ((IGuild) Context.Guild).GetCurrentUserAsync();
                if (user.GuildPermissions.Has(GuildPermission.ManageMessages))
                    _ = Context.Message.DeleteAsync();
            });
        }

        [AquaCommand]
        public async Task Test()
        {
            await ReplyAsync((await _discordService.SendConfirmAsync(Context, "Something in 5sec.", timeout: 5000))
                .ToString());
        }

        [AquaCommand, Alias("p")]
        [RequireVoiceChannel]
        public async Task Play([Remainder] string? query = null)
            => await InternalPlayAsync(query, false);

        [AquaCommand, Alias("pn")]
        [RequireVoiceChannel]
        public async Task PlayNext([Remainder] string? query = null)
            => await InternalPlayAsync(query, true);

        private async Task InternalPlayAsync(string? query, bool next)
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            if (query is null)
            {
                if (player.State != PlayerState.Paused)
                {
                    await ReplyAsync($"{_symbol.CrossMark} N-Nani !?");
                    return;
                }

                await ReplyAsync($"{_symbol.Play} Playing");
                await player.ResumeAsync();
                return;
            }

            var msg = await ReplyAsync($"{_symbol.Loading} Loading ... `[{query}]`");
            var trackLoad = await _audioService.LoadTracksAsync(
                (!Uri.TryCreate(query, UriKind.Absolute, out var uri) ||
                 !(uri.Scheme == Uri.UriSchemeHttp ||
                   uri.Scheme == Uri.UriSchemeHttps)
                    ? "ytsearch:"
                    : "") + query);

            if (trackLoad.LoadType == TrackLoadType.NoMatches)
            {
                await msg.ModifyAsync(x => x.Content = $"{_symbol.CrossMark} No results found for `[{query}]`");
                return;
            }
            else if (trackLoad.LoadType == TrackLoadType.TrackLoaded)
            {
                var track = trackLoad.Tracks![0];
            }

            switch (trackLoad.LoadType)
            {
                case TrackLoadType.LoadFailed:
                    await msg.ModifyAsync(x => x.Content = $"{_symbol.CrossMark} Track load failed `[{query}]`");
                    break;
                case TrackLoadType.SearchResult when trackLoad.Tracks?.Any() ?? false:
                case TrackLoadType.TrackLoaded when trackLoad.Tracks?.Any() ?? false:
                case TrackLoadType.PlaylistLoaded when trackLoad.Tracks?.Any() ?? false:
                    var track = trackLoad.Tracks.ElementAt(
                        Math.Max(
                            trackLoad.LoadType == TrackLoadType.PlaylistLoaded
                                ? trackLoad.PlaylistInfo!.SelectedTrack
                                : 0, 0));

                    var position = await player.PlayAsync(track);
                    var trackDurationText = track.IsLiveStream ? "LIVE" : track.Duration.ToString();

                    await msg.ModifyAsync(async x =>
                    {
                        x.Content = null;
                        x.Embed = new EmbedBuilder()
                            .WithTitle(
                                position > 0
                                    ? $"Add to queue ({position}, {trackDurationText})"
                                    : $"Playing ({trackDurationText})")
                            .WithDescription($"[{track.Title}](https://{track.Source})")
                            .WithThumbnailUrl(await track.GetThumbnailAsync())
                            .WithFooter(f =>
                                f.WithText($"Added by {Context.User}")
                                    .WithIconUrl(Context.User.GetAvatarUrl(size: 32)))
                            .WithCurrentTimestamp()
                            .Build();
                    });

                    if (trackLoad.LoadType == TrackLoadType.PlaylistLoaded
                        && trackLoad.Tracks.Length > 1)
                    {
                        var builder =
                            new EmbedBuilder().WithDescription(
                                $"Load additional **{trackLoad.Tracks!.Length}** tracks from **{trackLoad.PlaylistInfo!.Name}** ?");

                        var r= await _discordService.SendConfirmAsync(Context, embed: builder
                            .Build(), onComplete: async (result, message) =>
                        {
                            switch (result)
                            {
                                case ConfirmationResult.Yes:
                                    builder
                                        .WithDescription($"Loaded **{trackLoad.Tracks!.Length - 1}** tracks")
                                        .WithColor(Color.Green);

                                    foreach (var anotherTrack in trackLoad.Tracks
                                        .Where(x => x != track))
                                        await player.PlayAsync(anotherTrack, player.State == PlayerState.Playing);

                                    break;
                                default:
                                    builder = null;
                                    break;
                            }

                            if (builder == null)
                                await message.DeleteAsync();
                            else
                            {
                                await message.ModifyAsync(x => x.Embed = builder.Build());
                                await message.RemoveAllReactionsAsync();
                            }
                        });
                    }
                    break;
                default:
                    await msg.ModifyAsync(x => x.Content = $"{_symbol.CrossMark} No results found for `[{query}]`");
                    break;
            }
        }

        [AquaCommand, Alias("q")]
        [RequireVoiceChannel]
        public async Task Queue(string? query = null, bool next = false)
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            if (query is null)
                await ReplyAsync(string.Join("\n", player.Queue.Select(x => x.Title)));

            else
                await InternalPlayAsync(query, next);
        }

        [AquaCommand]
        [RequireMusicListening]
        public async Task Pause()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            switch (player.State)
            {
                case PlayerState.Playing:
                    await player.PauseAsync();
                    await ReplyAsync($"{_symbol.Pause} Paused");
                    break;
                default:
                    await ReplyAsync($"{_symbol.CrossMark} Not playing");
                    return;
            }
        }

        [AquaCommand, Alias("disconnect", "dc")]
        [RequireMusicListening]
        public async Task Stop()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            if (player.State != PlayerState.Destroyed)
                await player.StopAsync(true);

            await ReplyAsync($"{_symbol.Stop} Stopped");
        }

        [AquaCommand]
        [RequireMusicListening]
        public async Task Skip()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            await player.StartSkipVoteAsync(Context.Message);
        }

        [AquaCommand, Alias("fs")]
        [RequireMusicPlayer]
        [RequireDjPermission]
        public async Task ForceSkip()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            var isLooping = player.IsLooping;
            if (isLooping)
                player.IsLooping = false;

            await player.SkipAsync();

            player.IsLooping = isLooping;
        }

        [AquaCommand, Alias("np")]
        [RequireMusicListening]
        public async Task NowPlaying()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            await player.StartNowPlayingAsync();
        }
        
        [AquaCommand]
        [RequireMusicListening]
        public async Task Clear()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            player.Queue.Clear();
            await ReplyAsync($"{_symbol.CheckMark} Queue cleared");
        }


        [AquaCommand, Alias("loop")]
        [RequireMusicListening]
        public async Task Repeat()
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            player.IsLooping = !player.IsLooping;

            await ReplyAsync(
                $"{_symbol.Memo} Repeat mode: `{(player.IsLooping ? "ON" : "OFF")}`");
        }

        [AquaCommand, Alias("s")]
        [RequireMusicListening]
        public async Task Seek(TimeSpan position)
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            var track = player.CurrentTrack;
            if (track is null)
            {
                await ReplyAsync("There is no track to seek");
                return;
            }

            var to = TimeSpan.FromSeconds(Math.Clamp(position.TotalSeconds, 0, track.Duration.TotalSeconds));
            await player.SeekPositionAsync(to);
            await ReplyAsync($"Seek to `{to}`");
        }

        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [AquaCommand, Alias("vol")]
        [RequireMusicListening]
        public async Task Volume([Range(0, 200)] int? volume = null)
        {
            var player = await _audioService.GetPlayerOrJoinAsync<AquaLavalinkPlayer>(Context);
            if (player is null)
                return;

            if (volume is null)
                await ReplyAsync($"{_symbol.Page} Currently volume is: `{(int) (player.Volume * 100)}`");
            else
                await player.SetVolumeAsync(volume.Value / 100f);
        }
    }
}