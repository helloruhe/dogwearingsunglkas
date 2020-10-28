using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Discord.Addons.Interactive;
using dws.S;

namespace dws
{
    public class Program
    {
        private int multiplier;
        private CommandHandlingService _handler;
        private TwitterService _twitterService;
        private DiscordSocketClient client;

        public static void Main(string[] args) =>
            new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {

            bool isOn = true;
            if (Environment.GetEnvironmentVariable("token") == null)
            {
                try
                {
                    Console.WriteLine("token");
                    Environment.SetEnvironmentVariable("token", Console.ReadLine(), EnvironmentVariableTarget.Machine);
                    Console.WriteLine("api key");
                    Environment.SetEnvironmentVariable("consumerKey", Console.ReadLine(), EnvironmentVariableTarget.Machine);
                    Console.WriteLine("api sec");
                    Environment.SetEnvironmentVariable("consumerSecret", Console.ReadLine(), EnvironmentVariableTarget.Machine);
                }
                catch
                {
                    Console.WriteLine("token");
                    Environment.SetEnvironmentVariable("token", Console.ReadLine());
                    Console.WriteLine("api key");
                    Environment.SetEnvironmentVariable("consumerKey", Console.ReadLine());
                    Console.WriteLine("api sec");
                    Environment.SetEnvironmentVariable("consumerSecret", Console.ReadLine());
                }
            }
            using (var services = ConfigureServices())
            {

                client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
                await client.StartAsync();
                await client.SetGameAsync("🔥 Yuh Yuh Yuh Yuh. 🔥 Yup. Aye. Yuh. Yuh Yup. ⚠️ 패션 경고 나 섹시로 " +
                   "⚠️ 패션 경고 나 섹시로 🔥 Aye Yuh. Yyuh", type: ActivityType.Playing);// Load commands and modules into the command service
                // Here we initialize the logic required to register our commands.
                _twitterService = services.GetService<TwitterService>();
                await _twitterService.AuthClient();
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
               // await _twitterService.checkMentions();

                while (isOn)
                {
                    await CheckMentions();
                    await Task.Delay(TimeSpan.FromMinutes(3));
                }

                await Task.Delay(Timeout.Infinite);
            }
            
            // Connect to the websocket
        }
        private async Task CheckMentions()
        {
            multiplier = int.Parse(File.ReadAllText("multi.txt")); //incase of a forcepost, if it's been changed
            await _twitterService.checkMentions();
            multiplier += 1;
            if (multiplier >= 70)
            {
                try
                {
                    await _twitterService.PostImage();
                }
                catch
                {
                    if (_twitterService.GetReply(0) == null)
                    {
                        var oi = client.GetGuild(763851218145509397)
                            .GetTextChannel(763936785272930374).SendMessageAsync("@everyone the bot is out of images ://");
                    }
                }
                multiplier = 0;
            }
            File.WriteAllText("multi.txt", $"{multiplier}");
        }

        public static Task LogAsync(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Console.WriteLine($@"[{msg.Severity}] {msg.ToString()} -- ({msg.Source.ToString()})");
            Console.ResetColor();
            return Task.CompletedTask;
        }
        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<ImageService>()
                .BuildServiceProvider();
        }
    }

}

