using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace AquaBot.Commands.TypeReaders
{
    public class BooleanTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            if (bool.TryParse(input, out var result))
                return Task.FromResult(TypeReaderResult.FromSuccess(result));


            return Task.FromResult(input?.ToLower() switch
            {
                "y" or "yes" or "on" => TypeReaderResult.FromSuccess(true),
                "n" or "no" or "off" => TypeReaderResult.FromSuccess(false),
                _ => TypeReaderResult.FromError(CommandError.ParseFailed, "Input could not be parsed as a boolean.")
            });
        }
    }
}