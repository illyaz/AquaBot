using System;
using System.Diagnostics;
using AquaBot.Data;
using AquaBot.Lavalink;
using AquaBot.Models;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Npgsql;
using Serilog;
using ILogger = Lavalink4NET.Logging.ILogger;

namespace AquaBot
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet(settings: new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            services
                .Configure<BotConfig>(Configuration.GetSection("Bot"))
                .Configure<LavalinkNodeOptions>(Configuration.GetSection("LavaLink"));

            services.AddDbContext<AquaDbContext>(
                c => c.UseNpgsql(Configuration.GetConnectionString("Default")));

            services
                .AddSingleton(Stopwatch.StartNew())
                .AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig()
                {
                    LogLevel = LogSeverity.Verbose,
                    AlwaysDownloadUsers = true
                }))
                .AddSingleton(c => (BaseSocketClient) c.GetRequiredService<DiscordSocketClient>());

            services
                .AddSingleton<ILogger, AquaLavalinkLogger>()
                .AddSingleton<IAudioService, AquaLavalinkNode>()
                .AddSingleton<IDiscordClientWrapper, AquaDiscordClientWrapper>()
                .AddTransient<AquaLavalinkPlayer>()
                .AddSingleton(c => c.GetRequiredService<IOptions<LavalinkNodeOptions>>().Value);

            services
                .AddSingleton<Services.DiscordService>()
                .AddSingleton<Services.SymbolService>()
                .AddSingleton<Services.MusicService>()
                .AddSingleton<Services.GuildConfigService>();

            services
                .AddSingleton<CommandService>()
                .AddSingleton<Commands.CommandHandler>();

            services
                .AddHostedService<HostedServices.DiscordBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            app.UseSerilogRequestLogging();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseWelcomePage();
        }
    }
}