using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AquaBot.Commands;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace AquaBot.Services
{
    public class DiscordService
    {
        private readonly BaseSocketClient _client;
        private readonly SymbolService _symbol;

        private readonly Dictionary<ulong, ConfirmationInfo> _confirmationInfos =
            new Dictionary<ulong, ConfirmationInfo>();

        public DiscordService(BaseSocketClient client, SymbolService symbol)
        {
            _client = client;
            _symbol = symbol;
            _client.ReactionAdded += ClientOnReactionAdded;
        }

        public async Task<ConfirmationResult> SendConfirmAsync(
            AquaCommandContext context,
            string? text = null,
            bool isTts = false,
            Embed? embed = null,
            int timeout = 3000,
            Func<ConfirmationResult, RestUserMessage, Task>? onComplete = null)
        {
            var info = new ConfirmationInfo();
            var message = await context.Channel.SendMessageAsync(text, isTts, embed);
            await message.AddReactionsAsync(new IEmote[]
            {
                new Emoji(_symbol.CheckMark),
                new Emoji(_symbol.CrossMark)
            });

            try
            {
                _confirmationInfos.Add(message.Id, info);
                info.SetTimeout(TimeSpan.FromMilliseconds(timeout));
                
                var result = await info.Result.Task;
                if (onComplete is not null)
                    await onComplete(result, message);
                
                return result;
            }
            finally
            {
                _confirmationInfos.Remove(message.Id);
            }
        }

        public async Task SendVoteAsync()
        {
            
        }

        private Task ClientOnReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (_confirmationInfos.TryGetValue(message.Id, out var confirmationInfo) &&
                reaction.UserId != _client.CurrentUser.Id)
            {
                if (reaction.Emote.Name == _symbol.CheckMark)
                    confirmationInfo.Result.SetResult(ConfirmationResult.Yes);
                else if (reaction.Emote.Name == _symbol.CrossMark)
                    confirmationInfo.Result.SetResult(ConfirmationResult.No);
            }

            return Task.CompletedTask;
        }

        private class ConfirmationInfo
        {
            public TaskCompletionSource Completion { get; } = new TaskCompletionSource();

            public TaskCompletionSource<ConfirmationResult> Result { get; } =
                new TaskCompletionSource<ConfirmationResult>();

            private Timer? _timer;

            public void SetTimeout(TimeSpan timeout)
            {
                _timer = new Timer(_ =>
                {
                    Result.SetResult(ConfirmationResult.Timeout);
                    _timer!.Dispose();
                    _timer = null;
                }, null, timeout, Timeout.InfiniteTimeSpan);
                Result.Task.ContinueWith(c => _timer?.Dispose());
            }
        }
    }

    public enum ConfirmationResult
    {
        Yes,
        No,
        Timeout
    }

    public enum VoteResult
    {
        Approved,
        Rejected,
        Cancelled,
        Timeout
    }
}