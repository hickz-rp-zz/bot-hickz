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

namespace Hickz
{
	public class Program
	{

		static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

		public async Task MainAsync()
		{
			if (!Directory.Exists("data"))
				Directory.CreateDirectory("data");

			JObject config = Functions.GetConfig();

			using var services = ConfigureServices();

			Functions.ColoredMessage(ConsoleColor.Black, ConsoleColor.Yellow, "-> Le bot démarre...");
			DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

			client.Log += Log;
			services.GetRequiredService<CommandService>().Log += Log;

			// Au démarrage, on récupère les ids des channels contenant des messages persistant et on les stock dans une variable
			// pour que ce soit moins lourd à vérifier lors de l'envoie d'un message
			Database database = Functions.InitializeDatabase(@"URI=file:db\persistent.db");

			if (!database.IsTableExisting("persistent"))
			{
				database.CreateTable("persistent", "channel_id BIGINT PRIMARY KEY, embed_content VARCHAR(255), last_msg_id BIGINT, author_id BIGINT");
			}
			else
			{
				var reader = database.GetValuesFromTable("persistent");

				while (reader.Read())
					PersistentMessages.Channels.Add((ulong)reader.GetInt64(0), new object[]
					{
						(ulong)reader.GetInt64(2),
						reader.GetString(1),
						(ulong)reader.GetInt64(3),
					}); // Ajoute l'id du channel et celle du message

				reader.Close();
			}

			string token = config["token"].Value<string>();
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
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
