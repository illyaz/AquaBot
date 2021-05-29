using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AquaBot.Services;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Events;
using Lavalink4NET.Player;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AquaBot.Lavalink
{
    public class AquaLavalinkPlayer : QueuedLavalinkPlayer
    {
        private readonly ILogger<AquaLavalinkPlayer> _logger;
        private readonly IAudioService _audioService;
        private readonly DiscordSocketClient _client;
        private readonly SymbolService _symbol;

        public ISocketMessageChannel MessageChannel { get; protected set; } = null!;

        private readonly HashSet<ulong> _skipVotes;
        private EmbedBuilder? _skipVoteEmbedBuilder;
        private int _skipVoteRequired;
        private ulong? _skipVoteRequestedUserId;
        private RestUserMessage? _skipVoteMessage;
        private Timer? _skipVoteTimer;

        private RestUserMessage? _nowPlayingMessage;
        private EmbedBuilder? _nowPlayingEmbedBuilder;
        private string? _lastNowPlayingTrack;
        private DateTimeOffset? _nowPlayingLiveStartTimestamp;

        public AquaLavalinkPlayer(ILogger<AquaLavalinkPlayer> logger, IAudioService audioService,
            DiscordSocketClient client, SymbolService symbol)
        {
            _logger = logger;
            _audioService = audioService;
            _client = client;
            _symbol = symbol;
            _skipVotes = new HashSet<ulong>();
        }

        public void Initialize(ISocketMessageChannel textChannel)
        {
            MessageChannel = textChannel;
        }

        public bool IsSkipVoting()
            => _skipVoteRequired > 0;

        private void ClearSkipVotes()
        {
            _skipVotes.Clear();
            _skipVoteRequired = 0;
        }

        internal async Task OnTrackPositionUpdatedAsync()
        {
            if (_nowPlayingMessage is not null && _nowPlayingEmbedBuilder is not null)
            {
                UpdateNowPlayingPosition();
                await _nowPlayingMessage.ModifyAsync(x => x.Embed = _nowPlayingEmbedBuilder.Build());
            }
        }

        public override async Task OnTrackStartedAsync(TrackStartedEventArgs eventArgs)
        {
            await base.OnTrackStartedAsync(eventArgs);

            if (_nowPlayingMessage is not null && _nowPlayingEmbedBuilder is not null)
            {
                await UpdateNowPlayingAsync();
                await _nowPlayingMessage.ModifyAsync(x => x.Embed = _nowPlayingEmbedBuilder.Build());
                _lastNowPlayingTrack = eventArgs.TrackIdentifier;
            }
        }

        public override async Task OnTrackEndAsync(TrackEndEventArgs eventArgs)
        {
            if (IsSkipVoting())
                await StopSkipVoteAsync(true);

            await base.OnTrackEndAsync(eventArgs);

            if (_nowPlayingMessage is not null && _nowPlayingEmbedBuilder is not null)
            {
                await UpdateNowPlayingAsync();
                await _nowPlayingMessage.ModifyAsync(x => x.Embed = _nowPlayingEmbedBuilder.Build());
            }

            _nowPlayingLiveStartTimestamp = null;
        }

        private void UpdateSkipVoteEmbedBuilder(bool isCancelled = false)
        {
            if (_skipVoteEmbedBuilder is null)
                return;

            _skipVoteEmbedBuilder.Color = isCancelled ? Color.Red : IsSkipVoteApproved() ? Color.Green : (Color?) null;
            _skipVoteEmbedBuilder
                .Fields[0].WithValue(isCancelled ? "Cancelled" :
                    IsSkipVoteApproved() ? "Approved" : $"{_skipVotes.Count} / {_skipVoteRequired}");
        }

        private bool IsSkipVoteApproved() => _skipVotes.Count >= _skipVoteRequired;

        public async Task StartSkipVoteAsync(SocketUserMessage message)
        {
            if (CurrentTrack is null || VoiceChannelId is null)
                throw new InvalidOperationException("No track");

            if (_skipVoteTimer is not null && _skipVoteMessage is not null)
            {
                await _skipVoteMessage.ReplyAsync("Vote started");
                return;
            }

            _client.ReactionAdded += DiscordOnReactionAdded;
            _client.ReactionRemoved += DiscordOnReactionRemoved;
            _client.ReactionsCleared += DiscordOnReactionsCleared;
            _client.MessageDeleted += DiscordOnMessageDeleted;
            var users = (await Client.GetChannelUsersAsync(GuildId, VoiceChannelId.Value))
                .Where(s => s != Client.CurrentUserId)
                .ToArray();

            _skipVoteRequired = 2 > users.Length
                ? 0
                : (int) Math.Floor(users.Length * 0.75);

            _skipVoteRequestedUserId = message.Author.Id;

            _skipVoteEmbedBuilder = new EmbedBuilder()
                .WithTitle("Skip vote")
                .WithDescription($"[{CurrentTrack.Title}]({CurrentTrack.Source})")
                .WithThumbnailUrl(await CurrentTrack.GetThumbnailAsync())
                .WithFields(
                    new EmbedFieldBuilder()
                        .WithName("Status")
                        .WithIsInline(true))
                .WithFooter(c =>
                    c.WithIconUrl(message.Author.GetAvatarUrl(size: 32)).WithText($"Requested by {message.Author}"))
                .WithCurrentTimestamp();
            UpdateSkipVoteEmbedBuilder();

            _skipVoteMessage = await MessageChannel.SendMessageAsync(embed: _skipVoteEmbedBuilder.Build());


            if (IsSkipVoteApproved())
                await StopSkipVoteAsync(notEnoughUser: true);
            else
            {
                if (_skipVoteTimer is not null)
                    await _skipVoteTimer.DisposeAsync();

                _skipVoteTimer = new Timer(__ => _ = StopSkipVoteAsync(true), null, 30000, 0);
                await _skipVoteMessage.AddReactionAsync(new Emoji(_symbol.CheckMark));
            }
        }

        private async Task StopSkipVoteAsync(bool isCancelled = false, bool notEnoughUser = false)
        {
            if (_skipVoteTimer != null)
            {
                await _skipVoteTimer.DisposeAsync();
                _skipVoteTimer = null;
            }

            _client.ReactionAdded -= DiscordOnReactionAdded;
            _client.ReactionRemoved -= DiscordOnReactionRemoved;
            _client.ReactionsCleared -= DiscordOnReactionsCleared;
            _client.MessageDeleted -= DiscordOnMessageDeleted;
            ClearSkipVotes();

            if (!notEnoughUser)
            {
                if (_skipVoteMessage is not null && _skipVoteEmbedBuilder is not null)
                {
                    await _skipVoteMessage.ModifyAsync(m =>
                    {
                        UpdateSkipVoteEmbedBuilder(isCancelled);
                        m.Embed = _skipVoteEmbedBuilder.Build();
                    });
                }
            }

            if (isCancelled && _skipVoteMessage is not null)
            {
                var user = await ((IGuild) _client.GetGuild(GuildId)).GetCurrentUserAsync();
                if (user.GetPermissions((IGuildChannel) _skipVoteMessage.Channel).Has(ChannelPermission.ManageMessages))
                    await _skipVoteMessage.RemoveAllReactionsAsync();
            }

            if (IsSkipVoteApproved() && _skipVoteMessage is not null && !isCancelled || notEnoughUser)
            {
                var isLooping = IsLooping;
                if (isLooping)
                    IsLooping = false;

                await SkipAsync();

                IsLooping = isLooping;
                await _skipVoteMessage.ReplyAsync($"{_symbol.CheckMark} Skipped");
            }

            _skipVoteMessage = null;
            _skipVoteRequestedUserId = null;
            _skipVoteEmbedBuilder = null;
        }

        public async Task StartNowPlayingAsync()
        {
            if (_nowPlayingMessage is not null)
            {
                await _nowPlayingMessage.DeleteAsync();
                _nowPlayingMessage = null;
            }

            _client.MessageReceived += ClientOnMessageReceived;
            _lastNowPlayingTrack = null;
            _nowPlayingEmbedBuilder = new EmbedBuilder();

            await UpdateNowPlayingAsync();
            _nowPlayingMessage = await MessageChannel.SendMessageAsync(embed: _nowPlayingEmbedBuilder.Build());
        }

        private async Task UpdateNowPlayingAsync()
        {
            await UpdateNowPlayingTrack();
            UpdateNowPlayingPosition();
        }

        public override Task OnTrackExceptionAsync(TrackExceptionEventArgs eventArgs)
        {
            return base.OnTrackExceptionAsync(eventArgs);
        }

        private async Task UpdateNowPlayingTrack()
        {
            if (_nowPlayingEmbedBuilder is null)
                return;

            if (CurrentTrack is null)
            {
                _nowPlayingEmbedBuilder
                    .WithTitle("Not playing")
                    .WithDescription(null)
                    .WithThumbnailUrl(null)
                    .WithFooter(null, null)
                    .WithColor(Color.Default);
            }
            else
            {
                _nowPlayingEmbedBuilder
                    .WithTitle("Now playing")
                    .WithDescription($"[{CurrentTrack.Title}](https://{CurrentTrack.Source})")
                    .WithColor(Color.Green);

                if (CurrentTrack.Identifier != _lastNowPlayingTrack || _lastNowPlayingTrack is null)
                {
                    if (CurrentTrack.IsLiveStream && CurrentTrack.Provider == StreamProvider.YouTube)
                    {
                        var wc = new WebClient();
                        var info = HttpUtility.UrlDecode(await wc.DownloadStringTaskAsync(
                            $"https://www.youtube.com/watch?v={CurrentTrack.TrackIdentifier}"));

                        var toFind = "\"startTimestamp\":\"";
                        var idx = info.IndexOf(toFind, StringComparison.Ordinal);
                        if (idx >= 0)
                        {
                            idx += toFind.Length;
                            var endIdx = info.IndexOf("\"", idx, StringComparison.Ordinal);
                            if (endIdx >= 0)
                            {
                                var jsonDate = info.Substring(idx, endIdx - idx);

                                _nowPlayingLiveStartTimestamp =
                                    DateTime.SpecifyKind(
                                        DateTime.ParseExact(
                                            jsonDate.Substring(0, jsonDate.IndexOf(" ", StringComparison.Ordinal)),
                                            "yyyy-MM-ddTHH:mm:ss", null), DateTimeKind.Utc);
                            }
                        }
                    }

                    _nowPlayingEmbedBuilder.WithThumbnailUrl(await CurrentTrack.GetThumbnailAsync());
                }
            }

            UpdateNowPlayingPosition();
        }

        private void UpdateNowPlayingPosition()
        {
            if (CurrentTrack is not null)
            {
                var pos = TrackPosition;
                var dur = (TimeSpan?) CurrentTrack.Duration;

                if (CurrentTrack.Provider == StreamProvider.YouTube && CurrentTrack.IsLiveStream &&
                    _nowPlayingLiveStartTimestamp is not null)
                {
                    dur = DateTimeOffset.UtcNow - _nowPlayingLiveStartTimestamp;
                    pos = dur!.Value;
                }

                if (dur > TimeSpan.MaxValue)
                    dur = null;

                var durText = dur is not null ? $"{dur:hh\\:mm\\:ss}" : "--";
                _nowPlayingEmbedBuilder?.WithFooter($"{pos:hh\\:mm\\:ss} / {durText}" +
                                                    (Queue.Count > 0 ? $" • Queue: {Queue.Count:N0}" : ""));
            }
            else
                _nowPlayingEmbedBuilder?.WithFooter(null, null);
        }

        private Task DiscordOnReactionsCleared(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel)
        {
            return StopSkipVoteAsync(true);
        }

        private Task DiscordOnReactionRemoved(Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (_skipVoteMessage is null
                || message.Id != _skipVoteMessage.Id
                || reaction.UserId == _skipVoteRequestedUserId
                || reaction.UserId == Client.CurrentUserId
                || reaction.Emote.Name != _symbol.CheckMark
                || !_skipVotes.Contains(reaction.UserId))
                return Task.CompletedTask;

            if (_skipVoteTimer is null)
                return StopSkipVoteAsync(true);

            _skipVoteTimer.Change(30000, 0);
            _skipVotes.Remove(reaction.UserId);

            return _skipVoteMessage.ModifyAsync(m =>
            {
                UpdateSkipVoteEmbedBuilder();
                m.Embed = _skipVoteEmbedBuilder?.Build();
            });
        }

        private Task DiscordOnReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (_skipVoteMessage is null
                || message.Id != _skipVoteMessage.Id
                || reaction.UserId == _skipVoteRequestedUserId
                || reaction.UserId == Client.CurrentUserId
                || reaction.Emote.Name != _symbol.CheckMark
                || _skipVotes.Contains(reaction.UserId))
                return Task.CompletedTask;

            if (_skipVoteTimer is null)
                return StopSkipVoteAsync(true);

            _skipVoteTimer.Change(30000, 0);
            _skipVotes.Add(reaction.UserId);

            if (IsSkipVoteApproved())
                _ = StopSkipVoteAsync();

            return _skipVoteMessage.ModifyAsync(m =>
            {
                UpdateSkipVoteEmbedBuilder();
                m.Embed = _skipVoteEmbedBuilder?.Build();
            });
        }

        private Task DiscordOnMessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (message.Id != _skipVoteMessage?.Id)
                return Task.CompletedTask;

            _skipVoteMessage = null;
            return StopSkipVoteAsync(true);
        }


        private async Task ClientOnMessageReceived(SocketMessage e)
        {
            if (e.Channel.Id == _nowPlayingMessage?.Channel.Id && e.Id > _nowPlayingMessage?.Channel.Id)
            {
                _client.MessageReceived -= ClientOnMessageReceived;
                _nowPlayingEmbedBuilder = null;
                _lastNowPlayingTrack = null;
                var m = _nowPlayingMessage;
                _nowPlayingMessage = null;
                await m.DeleteAsync();
            }
        }
    }
}