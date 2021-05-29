using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AquaBot.Commands;
using Discord;
using Lavalink4NET;
using Lavalink4NET.Player;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AquaBot.Lavalink
{
    public static class Extensions
    {
        private static readonly Lazy<HttpClient> LazyHttpClient = new Lazy<HttpClient>();
        private static HttpClient HttpClient => LazyHttpClient.Value;

        private static readonly Lazy<JsonSerializer> LazyJsonSerializer = new Lazy<JsonSerializer>();
        private static JsonSerializer JsonSerializer => LazyJsonSerializer.Value;

        public static async Task<T?> GetPlayerOrJoinAsync<T>(this IAudioService audioService,
            AquaCommandContext context)
            where T : LavalinkPlayer
        {
            if (audioService.HasPlayer(context.Guild.Id))
                return audioService.GetPlayer<T>(context.Guild.Id);

            if (!(context.User is IVoiceState voiceState) || voiceState.VoiceChannel is null)
                return null;

            var player =
                await audioService.JoinAsync(() => context.ServiceProvider.GetRequiredService<T>(), context.Guild.Id,
                    voiceState.VoiceChannel.Id, true);

            if(player is AquaLavalinkPlayer aquaPlayer)
                aquaPlayer.Initialize(context.Channel);
            
            if (context.Config?.Music is not null)
                await player.SetVolumeAsync(context.Config.Music.DefaultVolume / 100f);

            return player;
        }

        public static async Task<string> GetThumbnailAsync(this LavalinkTrack track)
        {
            var noImageUrl = "https://raw.githubusercontent.com/Yucked/Victoria/v5/src/Logo.png";

            var (shouldSearch, requestUrl) = track.Provider switch
            {
                StreamProvider.YouTube
                    => (false, $"https://img.youtube.com/vi/{track.TrackIdentifier}/maxresdefault.jpg"),
                StreamProvider.Twitch
                    => (true, $"https://api.twitch.tv/v4/oembed?url={track.Source}"),
                StreamProvider.SoundCloud
                    => (true, $"https://soundcloud.com/oembed?url={track.Source}&format=json"),
                StreamProvider.Vimeo
                    => (false, $"https://i.vimeocdn.com/video/{track.TrackIdentifier}.png"),
                _ => (false, noImageUrl)
            };

            if (!shouldSearch)
                return requestUrl;

            var responseMessage = await HttpClient.GetAsync(requestUrl);
            responseMessage.EnsureSuccessStatusCode();
            await using var stream = await responseMessage.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var jsonReader = new JsonTextReader(reader);

            var jo = JsonSerializer.Deserialize<JObject>(jsonReader);
            return jo.TryGetValue("thumbnail_url", StringComparison.OrdinalIgnoreCase, out var thumbnailUrl)
                ? thumbnailUrl.ToObject<string>()
                : noImageUrl;
        }
    }
}