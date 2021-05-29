using AquaBot.Models;
using Microsoft.Extensions.Options;

namespace AquaBot.Services
{
    public class SymbolService
    {
        private readonly IOptions<BotConfig> _config;

        public SymbolService(IOptions<BotConfig> config)
        {
            _config = config;
        }

        private string Get(string key)
            => _config.Value.Symbols!.TryGetValue(key, out var value) ? value : key;

        public string Youtube => Get("Youtube");
        public string Loading => Get("Loading");
        public string CheckMark => Get("CheckMark");
        public string CrossMark => Get("CrossMark");
        public string Notes => Get("Notes");
        public string Page => Get("Page");
        public string Memo => Get("Memo");
        
        public string Play => Get("Play");
        public string Pause => Get("Pause");
        public string Stop => Get("Stop");
    }
}