using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Discord;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Hickz
{
	public partial class ChannelManagerService
	{
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
		private JObject config = Functions.GetConfig();

		public ChannelManagerService(IServiceProvider services)
        {
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Event handlers
            _client.Ready += ClientReadyAsync;
		}

        private async Task ClientReadyAsync()
		{
			while (true)
			{
				Console.WriteLine("UPDATED");
				var socketGuild = _client.GetGuild(JsonConvert.DeserializeObject<ulong>(config["hickzDiscordServerId"].ToString()));
				var memberCountChannel = socketGuild.GetChannel(JsonConvert.DeserializeObject<ulong>(config["hickzTotalUsersChannel"].ToString()));
				var serverStatusChannel = socketGuild.GetChannel(JsonConvert.DeserializeObject<ulong>(config["hickzServerStatusChannel"].ToString()));
				var serverConnectedUsersChannel = socketGuild.GetChannel(JsonConvert.DeserializeObject<ulong>(config["hickzServerConnectedUsersChannel"].ToString()));

				string response;
				try
				{
					response = new WebClient().DownloadString("http://localhost:30120/players.json");
				}
				catch (Exception)
				{
					response = null;
				}

				if (response != null)
				{
					PlayersInfo[] playersInfos = JsonConvert.DeserializeObject<PlayersInfo[]>(response);
					await serverConnectedUsersChannel.ModifyAsync(prop => prop.Name = $"👥 Connecté(s) : {playersInfos.Length}/128");
				}
				await memberCountChannel.ModifyAsync(prop => prop.Name = $"👥 Membres : {socketGuild.MemberCount}");
				await serverStatusChannel.ModifyAsync(prop => prop.Name = $"Status : {(response != null ? "🟢" : "🔴")}");
				await Task.Delay(600000 * 30); // 30 sec
			}
		}

		private class PlayersInfo
		{
			private string endpoint { get; set; }
			private int id { get; set; }
			private string[] identifiers { get; set; }
			private string name { get; set; }
			private int ping { get; set; }
		}
	}
}
