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
	public partial class CommandHandlingService
    {
		private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
		private Dictionary<ulong, SocketCommandContext> usersWaiting = new Dictionary<ulong, SocketCommandContext>();
		private JObject config = Functions.GetConfig();

		public CommandHandlingService(IServiceProvider services)
        {
			_commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Event handlers
            _client.Ready += ClientReadyAsync;
            _client.MessageReceived += HandleCommandAsync;
			_client.ReactionAdded += ReactionAdded;
			_client.ButtonExecuted += ButtonExecuted;
		}

		private async Task ButtonExecuted(SocketMessageComponent messageComponent)
		{
			if (messageComponent.Data.CustomId == "test")
			{
				await messageComponent.RespondAsync($"{messageComponent.User.Mention} a cliqué !");
			}
		}

		private async Task ReactionAdded(Cacheable<IUserMessage, ulong> user, Cacheable<IMessageChannel, ulong> message, SocketReaction react)
		{
			foreach (var (key, value) in usersWaiting)
			{
				if (react.UserId == key)
				{
					if (react.Emote.Name == "hickz")
					{
						Stopwatch watcher = new Stopwatch();

						var socketGuild = _client.GetGuild(JsonConvert.DeserializeObject<ulong>(config["hickzDiscordServerId"].ToString()));
						var author = _client.GetUser(key);
						var channelPermisssions = new ChannelPermissions(false, false, false, false, false);

						OverwritePermissions perms = new OverwritePermissions(
							viewChannel: PermValue.Allow,
							sendMessages: PermValue.Allow,
							attachFiles: PermValue.Allow,
							readMessageHistory: PermValue.Allow);

						var channel = await socketGuild.CreateTextChannelAsync("aide-" + author.Username, prop => {
							prop.CategoryId = JsonConvert.DeserializeObject<ulong>(config["hickzSupportCategoryId"].ToString());
							prop.Topic = "Ticket d'assistance pour " + author.Mention;
							prop.PermissionOverwrites = new List<Overwrite>
							{
								new Overwrite(socketGuild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
								new Overwrite(JsonConvert.DeserializeObject<ulong>(config["hickzSupportRoleId"].ToString()), PermissionTarget.Role, perms),
								new Overwrite(author.Id, PermissionTarget.User, perms)
							};
						});

						var embed = new EmbedBuilder
						{
							Color = Color.DarkTeal,
							Title = "📩 • Ticket de support",
							Description = $"Message envoyé à la demande d'ouverture :\n{value.Message.Content}",
							Timestamp = DateTime.Now,
							Footer = new EmbedFooterBuilder()
							{
								IconUrl = Functions.GetAvatarUrl(author, 32),
								Text = author.Username + "#" + author.Discriminator
							}
						};

						var supportMessage = await channel.SendMessageAsync(text: socketGuild.GetRole(JsonConvert.DeserializeObject<ulong>(config["hickzSupportRoleId"].ToString())).Mention, embed: embed.Build());
						supportMessage.PinAsync().Wait();
						await value.Message.ReplyAsync($"Ticket créé, rendez-vous dans : {channel.Mention} 👋");
						usersWaiting.Remove(key);
					}
					else
					{
						usersWaiting.Remove(key);
						await value.Channel.SendMessageAsync($"Ouverture du ticket annulée.");
					}
					break;
				}
			}
		}

		private async Task HandleCommandAsync(SocketMessage rawMessage)
        {
			if (rawMessage.Author.IsBot || !(rawMessage is SocketUserMessage message))
				return;

			var context = new SocketCommandContext(_client, message);

			int argPos = 0;

			string[] prefixes = JsonConvert.DeserializeObject<string[]>(config["prefixes"].ToString());

			// Check if message has any of the prefixes or mentiones the bot.
			if (prefixes.Any(x => message.HasStringPrefix(x, ref argPos)) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
			{
				// Execute the command.
				var result = await _commands.ExecuteAsync(context, argPos, _services);
				if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
			}
			else
			{
				if (rawMessage.Channel is IPrivateChannel) // Création de ticket de support si mp DM
				{
					if (usersWaiting.ContainsKey(rawMessage.Author.Id))
					{
						var embedWaiting = new EmbedBuilder
						{
							Color = Color.Red,
							Title = "📩 • Ticket de support",
							Description = $"Vous avez un ticket en attente de confirmation pour sa création.",
							Timestamp = DateTime.Now,
						};

						await context.Message.ReplyAsync(embed: embedWaiting.Build());
						return;
					}

					var socketGuild = _client.GetGuild(JsonConvert.DeserializeObject<ulong>(config["hickzDiscordServerId"].ToString()));
					var socketCategoryChannel = socketGuild.GetCategoryChannel(JsonConvert.DeserializeObject<ulong>(config["hickzSupportCategoryId"].ToString()));

					ulong? currentTicketChannel = null;
					foreach (var categoryChannel in socketCategoryChannel.Channels)
					{
						var properties = categoryChannel as ITextChannel;
						if (properties.Topic == "Ticket d'assistance pour " + rawMessage.Author.Mention)
						{
							currentTicketChannel = properties.Id;
							break;
						}
					}

					if (currentTicketChannel == null)
					{
						var confirmation = new EmbedBuilder
						{
							Color = Color.DarkTeal,
							Title = "📩 • Ticket de support",
							Description = $"Cochez la réaction pour valider la création du ticket avec en raison d'ouverture :\n{rawMessage.Content}",
							Timestamp = DateTime.Now,
							Footer = new EmbedFooterBuilder()
							{
								IconUrl = Functions.GetAvatarUrl(rawMessage.Author, 32),
								Text = "Pour annuler l'ouverture, cochez la croix.Pour annuler l'ouverture, cochez la croix."
							}
						};

						var confirmationMsg = await rawMessage.Channel.SendMessageAsync(embed: confirmation.Build());
						IEmote emote = _client.GetGuild(JsonConvert.DeserializeObject<ulong>(config["hickzDiscordServerId"].ToString())).Emotes.First(e => e.Name == "hickz");
						await confirmationMsg.AddReactionsAsync(new[] { emote, new Emoji("❌") });
						usersWaiting.Add(rawMessage.Author.Id, context);
					}
					else
					{
						await context.Message.ReplyAsync($"Vous avez déjà un ticket de créé, rendez-vous dans : <#{currentTicketChannel}> 👋");
					}
				}
			}
		}

		private async Task ClientReadyAsync()
		{
			int lastMemberCount = 0;
			string lastResponse = "...";
			await Functions.SetBotStatusAsync(_client);

			var socketGuild = _client.GetGuild(JsonConvert.DeserializeObject<ulong>(config["hickzDiscordServerId"].ToString()));
			var memberCountChannel = socketGuild.GetChannel(JsonConvert.DeserializeObject<ulong>(config["hickzTotalUsersChannel"].ToString()));
			var serverStatusChannel = socketGuild.GetChannel(JsonConvert.DeserializeObject<ulong>(config["hickzServerStatusChannel"].ToString()));
			while (true)
			{
				string response;
				try
				{
					response = new WebClient().DownloadString("http://localhost:30120/players.json");
				}
				catch (Exception)
				{
					response = null;
				}

				if (lastMemberCount != socketGuild.MemberCount)
				{
					await memberCountChannel.ModifyAsync(prop => prop.Name = $"👥 Membres : {socketGuild.MemberCount}");
					lastMemberCount = socketGuild.MemberCount;
				}

				if (lastResponse != response)
				{
					await serverStatusChannel.ModifyAsync(prop => prop.Name = $"🎮・{(response != null ? $"🟢 | {JsonConvert.DeserializeObject<PlayersInfo[]>(response).Length}/128" : "🔴")}");
					lastResponse = response;
				}

				await Task.Delay(30000);
			}
		}

        public async Task InitializeAsync()
            => await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

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
