using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Hickz
{
	public class Program
	{
		JObject config = Functions.GetConfig();

		static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			using var services = ConfigureServices();

			Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Yellow, "-> Le bot démarre...");
			DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

			client.Log += Log;
			services.GetRequiredService<CommandService>().Log += Log;

			string token = config["token"].Value<string>();
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
			//services.GetRequiredService<ChannelManagerService>();

			await Task.Delay(-1);
		}

		public ServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
				.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
				{
					AlwaysDownloadUsers = true,
					MessageCacheSize = 1000,
					GatewayIntents = GatewayIntents.All,
					LogLevel = LogSeverity.Verbose
				}))
				.AddSingleton(new CommandService(new CommandServiceConfig
				{
					LogLevel = LogSeverity.Info,
					DefaultRunMode = RunMode.Async,
					CaseSensitiveCommands = false,
				}))
				.AddSingleton<CommandHandlingService>()
				.BuildServiceProvider();
		}

		private Task Log(LogMessage log)
		{
			Console.WriteLine(log.ToString());
			return Task.CompletedTask;
		}
	}
}
