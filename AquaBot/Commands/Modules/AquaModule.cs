using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AquaBot.Commands.ParameterPreconditions;
using Discord.Commands;
using Discord.Commands.Builders;

namespace AquaBot.Commands.Modules
{
    public abstract class AquaModule : ModuleBase<AquaCommandContext>
    {
        protected override void OnModuleBuilding(CommandService commandService, ModuleBuilder builder)
        {
            foreach (var commandBuilder in builder.Commands)
            {
                foreach (var parameterBuilder in commandBuilder.Parameters)
                {
                    if (parameterBuilder.Attributes.OfType<ValidationAttribute>().Any())
                        parameterBuilder.AddPrecondition(new DataAnnotationValidatorAttribute());
                }

                commandBuilder.AddAttributes(new AliasAttribute(commandBuilder.Name));
                commandBuilder.AddAttributes(new SummaryAttribute($"Injected summary: {commandBuilder.Name}"));
            }

            base.OnModuleBuilding(commandService, builder);
        }
    }
}