using System;
using System.Runtime.CompilerServices;
using Discord.Commands;

namespace AquaBot.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AquaCommand : CommandAttribute
    {
        public string MethodName { get; }

        public AquaCommand([CallerMemberName] string memberName = "")
            : base(memberName.ToLower())
        {
            MethodName = memberName;
        }
    }
}