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

namespace Hickz
{
	public partial class CommandHandlingService
    {
		private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
		private Dictionary<ulong, SocketCommandContext> usersWaiting = new Dictionary<ulong, SocketCommandContext>();
		private Database database;

		public CommandHandlingService(IServiceProvider services)
        {
			_commands = services.GetRequiredService<CommandService>();
            _client = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Event handlers
            _client.Ready += ClientReadyAsync;
            _client.MessageReceived += HandleCommandAsync;
			_client.ReactionAdded += ReactionAdded;
		}

		private async Task ReactionAdded(Cacheable<IUserMessage, ulong> user, Cacheable<IMessageChannel, ulong> message, SocketReaction react)
		{
			foreach (var (key, value) in usersWaiting)
			{
				if (react.UserId == key)
				{
					JObject config = Functions.GetConfig();
					Console.WriteLine(react.Emote.Name);
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

			if (!(rawMessage.Channel is IPrivateChannel))
			{
				if (PersistentMessages.Channels.Count > 0)
				{
					JObject cfg = Functions.GetConfig();
					var channelInfo = rawMessage.Channel as SocketGuildChannel;
					if (channelInfo.Guild.Id == JsonConvert.DeserializeObject<ulong>(cfg["hickzDiscordServerId"].ToString()))
					{
						if (PersistentMessages.Channels.ContainsKey(channelInfo.Id))
						{
							ulong messageId = (ulong)PersistentMessages.Channels[channelInfo.Id][0];
							string description = PersistentMessages.Channels[channelInfo.Id][1].ToString();
							SocketUser baseAuthor = _client.GetUser((ulong)PersistentMessages.Channels[channelInfo.Id][2]);

							var embed = new EmbedBuilder
							{
								Color = Color.Green,
								Title = "Message persistant :",
								Description = description,
								Footer = new EmbedFooterBuilder()
								{
									IconUrl = Functions.GetAvatarUrl(baseAuthor, 32),
									Text = baseAuthor.Username + "#" + baseAuthor.Discriminator
								}
							};
							
							ulong sendedMessageId = rawMessage.Channel.SendMessageAsync(embed: embed.Build()).Result.Id;
							PersistentMessages.Channels[channelInfo.Id] = new object[] { sendedMessageId, description, baseAuthor.Id };
							if (database == null)
								database = Functions.InitializeDatabase(@"URI=file:db\persistent.db");

							database.Update("persistent", $"SET last_msg_id = {sendedMessageId} WHERE channel_id = {channelInfo.Id}");
							database.Close();
							database = null;

							var channelMessages = await rawMessage.Channel.GetMessagesAsync(10, CacheMode.AllowDownload).FlattenAsync();
							foreach (var msg in channelMessages)
								if (msg.Embeds.Count > 0)
									foreach (var _embed in msg.Embeds)
										if (_embed.Title == "Message persistant :")
											await msg.DeleteAsync();
						}
					}
				}
			}
			
			var context = new SocketCommandContext(_client, message);

			int argPos = 0;

			JObject config = Functions.GetConfig();
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
            => await Functions.SetBotStatusAsync(_client);

        public async Task InitializeAsync()
            => await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }
}
