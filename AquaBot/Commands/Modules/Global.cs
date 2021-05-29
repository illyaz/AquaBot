using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AquaBot.Commands.Attributes;
using AquaBot.Models;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;

namespace AquaBot.Commands.Modules
{
    public class Global : AquaModule
    {
        private readonly IOptions<BotConfig> _config;
        private readonly CommandService _commandService;
        private readonly Stopwatch _stopwatch;

        public Global(IOptions<BotConfig> config, CommandService commandService, Stopwatch stopwatch)
        {
            _config = config;
            _commandService = commandService;
            _stopwatch = stopwatch;
        }

        private string GetParameter(ParameterInfo parameter)
        {
            if (!parameter.IsOptional)
                return $"{parameter.Name}";

            var defaultValue = parameter.DefaultValue;

            if (parameter.DefaultValue is null)
                defaultValue = null;
            else if (parameter.DefaultValue is bool b)
                defaultValue = b ? "yes" : "no";
            else
                defaultValue = parameter.DefaultValue.ToString();

            return $"[{parameter.Name}|{defaultValue ?? "empty"}]";
        }

        [AquaCommand, Alias("h")]
        public async Task Help(string? category = null)
        {
            var commands = _commandService.Commands
                .Where(x => !x.Module.IsSubmodule)
                .GroupBy(x => x.Module)
                .ToList();

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Help");
            embedBuilder.WithDescription(
                "`parameter_name` for a required parameter\n`[parameter_name|default_value]` for an optional parameter");
            embedBuilder.WithFooter($"Requested by {Context.User}", Context.User.GetAvatarUrl(size: 32));
            embedBuilder.WithCurrentTimestamp();

            foreach (var groupCommands in commands
                .Where(x => x.Key is not null))
            {
                var group = groupCommands.Key!;

                var sb = new StringBuilder();

                var gName = group.Aliases.FirstOrDefault() ?? group.Group?.ToLower();
                if (!string.IsNullOrEmpty(gName))
                    gName += " ";

                foreach (var command in groupCommands)
                {
                    sb.Append(
                        $"**{Context.Config?.Prefix ?? _config.Value.DefaultPrefix}{gName}{command.Name}**{(command.Summary is not null ? $" - {command.Summary}" : "")}");

                    if (command.Parameters.Any())
                        sb.Append(' ');

                    sb.AppendLine(string.Join(" ", command.Parameters.Select(parameter =>
                        $"`{GetParameter(parameter)}`")));
                }

                foreach (var submodule in groupCommands.Key.Submodules)
                {
                    foreach (var command in submodule.Commands)
                    {
                        sb.Append(
                            $"**{Context.Config?.Prefix ?? _config.Value.DefaultPrefix}{gName}{submodule.Group} {command.Name}**{(command.Summary is not null ? $" - {command.Summary}" : "")}");

                        if (command.Parameters.Any())
                            sb.Append(' ');

                        sb.AppendLine(string.Join(" ", command.Parameters.Select(parameter =>
                            $"`{GetParameter(parameter)}`")));
                    }
                }

                var groupName = group.Group?.ToUpper() ?? "GLOBAL";
                var groupCommandAndAliases = new List<string>();

                if (!string.IsNullOrEmpty(group.Group))
                    groupCommandAndAliases.Add(group.Group);

                groupCommandAndAliases.AddRange(group.Aliases.Where(x => !string.IsNullOrEmpty(x)));

                if (groupCommandAndAliases.Any())
                    groupName +=
                        $" | <{string.Join(" | ", groupCommandAndAliases.Select(x => x.ToLower()).Distinct())}>";
                embedBuilder.AddField(groupName, sb.ToString());
            }

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("avatar")]
        public async Task Avatar(IGuildUser? user = null)
        {
            var u = user ?? (IGuildUser) Context.User;
            await ReplyAsync(embed: new EmbedBuilder()
                .WithTitle($"{u}'s avatar")
                .WithImageUrl(u.GetAvatarUrl(size: 512))
                .Build());
        }

        [AquaCommand]
        public async Task Uptime()
        {
            await Context.Message.ReplyAsync(embed: new EmbedBuilder()
                .WithDescription(_stopwatch.Elapsed.ToString())
                .Build());
        }
    }
}