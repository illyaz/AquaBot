// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Collections.Generic;

namespace AquaBot.Models
{
    public class BotConfig
    {
        public string? Token { get; set; }
        public string? DefaultPrefix { get; set; }
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, string>? Symbols { get; set; }
    }
}