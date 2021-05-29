using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;

namespace AquaBot.Commands.ParameterPreconditions
{
    public class DataAnnotationValidatorAttribute : ParameterPreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            ParameterInfo parameter, object value, IServiceProvider services)
        {
            foreach (var attribute in parameter.Attributes.OfType<ValidationAttribute>())
                if (!attribute.IsValid(value))
                    return Task.FromResult(PreconditionResult.FromError(attribute.FormatErrorMessage(parameter.Name)));

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}