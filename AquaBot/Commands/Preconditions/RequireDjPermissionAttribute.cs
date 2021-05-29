using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AquaBot.Commands.Preconditions
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RequireDjPermissionAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (context.Guild is null)
                return Task.FromResult(PreconditionResult.FromError("You must be in guild"));

            if (!(context is AquaCommandContext aquaContext))
                throw new InvalidOperationException($"Require {nameof(AquaCommandContext)}");

            var guildUser = (SocketGuildUser) aquaContext.User;

            if (!guildUser.GuildPermissions.Has(GuildPermission.Administrator) &&
                aquaContext.Config!.Music!.DjRoleId is not null &&
                guildUser.Roles.All(x => x.Id != aquaContext.Config.Music.DjRoleId))
                return Task.FromResult(PreconditionResult.FromError(
                    $"User require role **{context.Guild.GetRole(aquaContext.Config.Music.DjRoleId.Value)}**"));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}