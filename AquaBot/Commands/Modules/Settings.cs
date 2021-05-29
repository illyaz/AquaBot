using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using AquaBot.Commands.Attributes;
using AquaBot.Data;
using AquaBot.Models;
using AquaBot.Services;
using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Microsoft.Extensions.Options;

namespace AquaBot.Commands.Modules
{
    public class AquaResult : RuntimeResult
    {
        public AquaResult(CommandError? error, string? reason) : base(error, reason)
        {
        }

        public static AquaResult Success()
            => new AquaResult(null, null);
    }

    [Group("settings")]
    public class Settings : AquaModule
    {
        private readonly AquaDbContext _db;
        private readonly IOptions<BotConfig> _botConfig;
        private readonly GuildConfigService _guildConfigService;
        private readonly SymbolService _symbolService;

        public Settings(AquaDbContext db, IOptions<BotConfig> botConfig, GuildConfigService guildConfigService,
            SymbolService symbolService)
        {
            _db = db;
            _botConfig = botConfig;
            _guildConfigService = guildConfigService;
            _symbolService = symbolService;
        }

        [Command("prefix")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task PrefixAsync([Remainder] string? prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                var config = await _db.GuildConfigs.FindAsync(Context.Guild.Id);
                config.Prefix = prefix;
                await _db.SaveChangesAsync();

                _guildConfigService.Update(Context.Guild, config);
                await ReplyAsync($"{_symbolService.Memo} Global.Prefix: `{prefix}`");
            }
            else
                await ReplyAsync(
                    $"{_symbolService.Page} Global.Prefix: `{Context.Config!.Prefix ?? _botConfig.Value.DefaultPrefix}`");
        }

        [Group("music")]
        public class Music : AquaModule
        {
            private readonly AquaDbContext _db;
            private readonly IOptions<BotConfig> _botConfig;
            private readonly GuildConfigService _guildConfigService;
            private readonly SymbolService _symbolService;

            public Music(AquaDbContext db, IOptions<BotConfig> botConfig, GuildConfigService guildConfigService,
                SymbolService symbolService)
            {
                _db = db;
                _botConfig = botConfig;
                _guildConfigService = guildConfigService;
                _symbolService = symbolService;
            }

            #region DJ

            [AquaCommand]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task Dj(IRole? role = null)
            {
                if (role is not null)
                {
                    var config = await _db.GuildConfigs.FindAsync(Context.Guild.Id);
                    config.Music!.DjRoleId = role.Id;
                    _db.Entry(config).Property(p => p.Music).IsModified = true;
                    await _db.SaveChangesAsync();

                    _guildConfigService.Update(Context.Guild, config);
                    await ReplyAsync(
                        $"{_symbolService.Memo} Music.Dj: `{role}`");
                }
                else if (Context.Config!.Music?.DjRoleId is not null)
                    await ReplyAsync(
                        $"{_symbolService.Page} Music.Dj: `{Context.Guild.GetRole(Context.Config.Music.DjRoleId.Value)}`");
            }

            #endregion

            #region defaultVolume

            [Command("defaultvolume"), Alias("defaultvol")]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task DefaultVolumeAsync([Range(0, 200)] short? value = null)
            {
                if (value.HasValue)
                {
                    var config = await _db.GuildConfigs.FindAsync(Context.Guild.Id);
                    config.Music!.DefaultVolume = value.Value;
                    _db.Entry(config).Property(p => p.Music).IsModified = true;
                    await _db.SaveChangesAsync();

                    _guildConfigService.Update(Context.Guild, config);
                    await ReplyAsync($"{_symbolService.Memo} Music.DefaultVolume: `{value:N0}`");
                }
                else
                    await ReplyAsync(
                        $"{_symbolService.Page} Music.DefaultVolume: `{Context.Config!.Music?.DefaultVolume:N0}`");
            }

            #endregion

            #region DeleteUserCommand

            [Command("deleteusercommand")]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task DeleteUserCommandAsync(bool? value = null)
            {
                if (value.HasValue)
                {
                    var config = await _db.GuildConfigs.FindAsync(Context.Guild.Id);
                    config.Music!.DeleteUserCommand = value.Value;
                    _db.Entry(config).Property(p => p.Music).IsModified = true;
                    await _db.SaveChangesAsync();

                    _guildConfigService.Update(Context.Guild, config);
                    await ReplyAsync(
                        $"{_symbolService.Memo} Music.DeleteUserCommand: `{(value.Value ? "yes" : "no")}`");
                }
                else
                    await ReplyAsync(
                        $"{_symbolService.Page} Music.DeleteUserCommand: `{(Context.Config!.Music!.DeleteUserCommand ? "yes" : "no")}`");
            }

            #endregion

            #region PreventDuplicates

            [Command("preventduplicates")]
            [RequireUserPermission(GuildPermission.Administrator)]
            public async Task PreventDuplicatesAsync(bool? value = null)
            {
                if (value.HasValue)
                {
                    var config = await _db.GuildConfigs.FindAsync(Context.Guild.Id);
                    config.Music!.PreventDuplicates = value.Value;
                    _db.Entry(config).Property(p => p.Music).IsModified = true;
                    await _db.SaveChangesAsync();

                    _guildConfigService.Update(Context.Guild, config);
                    await ReplyAsync(
                        $"{_symbolService.Memo} Music.PreventDuplicates: `{(value.Value ? "yes" : "no")}`");
                }
                else
                    await ReplyAsync(
                        $"{_symbolService.Page} Music.PreventDuplicates: `{(Context.Config!.Music!.PreventDuplicates ? "yes" : "no")}`");
            }

            #endregion
        }
    }
}