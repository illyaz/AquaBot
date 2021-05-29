using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AquaBot.Data;
using Discord;
using Microsoft.Extensions.DependencyInjection;

namespace AquaBot.Services
{
    public class GuildConfigService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<ulong, GuildConfig> _configs;

        public GuildConfigService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _configs = new ConcurrentDictionary<ulong, GuildConfig>();
        }

        public async Task<GuildConfig> Get(IGuild guild)
        {
            if (_configs.TryGetValue(guild.Id, out var config))
                return config;

            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider;
            var db = service.GetRequiredService<AquaDbContext>();

            config = await db.GuildConfigs.FindAsync(guild.Id)
                     ?? (await db.GuildConfigs.AddAsync(new GuildConfig
                     {
                         Id = guild.Id,
                         Music = new GuildConfig.MusicConfig()
                     })).Entity;
            await db.SaveChangesAsync();
            _configs.AddOrUpdate(guild.Id, config, (_, __) => config);
            return config;
        }

        public void Update(IGuild guild, GuildConfig config)
        {
            _configs.AddOrUpdate(guild.Id, config, (_, __) => config);
        }
    }
}