using System;
using System.Reflection;
using System.Threading.Tasks;
using Lavalink4NET;
using Lavalink4NET.Logging;
using Lavalink4NET.Payloads;
using Lavalink4NET.Payloads.Player;
using Lavalink4NET.Player;

namespace AquaBot.Lavalink
{
    public class AquaLavalinkNode : LavalinkNode
    {
        private static readonly PropertyInfo LavalinkSocketProperty =
            typeof(LavalinkPlayer).GetProperty("LavalinkSocket", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly MethodInfo EnsureNotDisposedMethod =
            typeof(LavalinkNode).GetMethod("EnsureNotDisposed", BindingFlags.Instance | BindingFlags.NonPublic)!;

        public AquaLavalinkNode(LavalinkNodeOptions options, IDiscordClientWrapper client, ILogger? logger = null,
            ILavalinkCache? cache = null)
            : base(options, client, logger, cache)
        {
        }

        protected override async Task OnPlayerPayloadReceived(IPlayerPayload payload)
        {
            await base.OnPlayerPayloadReceived(payload);

            var player = GetPlayer<AquaLavalinkPlayer>(ulong.Parse(payload.GuildId));
            if (player != null && payload is PlayerUpdatePayload playerUpdatePayload)
                await player.OnTrackPositionUpdatedAsync();
        }

        protected override async Task VoiceServerUpdated(object sender, VoiceServer voiceServer)
        {
            var player = GetPlayer<LavalinkPlayer>(voiceServer.GuildId);
            var need = player is not null && player.State == PlayerState.Playing;

            try
            {
                if (need && player is not null)
                    await player.PauseAsync();
            }
            catch
            {
                /* IGNORED */
            }

            await base.VoiceServerUpdated(sender, voiceServer);

            try
            {
                if (need && player is not null)
                {
                    await Task.Delay(1000);
                    await player.ResumeAsync();
                }
            }
            catch
            {
                /* IGNORED */
            }
        }

        private void EnsureNotDisposed()
            => EnsureNotDisposedMethod.Invoke(this, Array.Empty<object>());
    }
}