using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace Hickz
{
	public class Useful : ModuleBase<SocketCommandContext>
	{
		[Command("persistent")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task PersistentMessage([Remainder] string text = "")
		{
			if (text == "")
			{
				var errorEmbed = new EmbedBuilder
				{
					Color = Color.Red,
					Title = "Erreur",
					Description = $"Usage de la commande : \n`persistent` [message...]",
					Timestamp = DateTime.Now,
					Footer = new EmbedFooterBuilder()
					{
						IconUrl = Functions.GetAvatarUrl(Context.User, 32),
						Text = Context.User.Username + "#" + Context.User.Discriminator
					}
				};
				await Context.Channel.SendMessageAsync("", false, errorEmbed.Build());
				return;
			}

			var embed = new EmbedBuilder
			{
				Color = Color.Green,
				Title = "Message persistant :",
				Description = text,
				Footer = new EmbedFooterBuilder()
				{
					IconUrl = Functions.GetAvatarUrl(Context.User, 32),
					Text = Context.User.Username + "#" + Context.User.Discriminator
				}
			};

			Database database = Functions.InitializeDatabase(@"URI=file:db\persistent.db");

			if (!database.IsTableExisting("persistent"))
				database.CreateTable("persistent", "channel_id BIGINT PRIMARY KEY, embed_content VARCHAR(255), last_msg_id BIGINT, author_id BIGINT");

			ulong messageId = Context.Channel.SendMessageAsync("", false, embed.Build()).Result.Id;
			PersistentMessages.Channels.Add(Context.Channel.Id, new object[] { messageId, text, Context.User.Id });
			database.Insert($"INSERT INTO persistent (channel_id, embed_content, last_msg_id, author_id) VALUES (@1, @2, @3, @4)", Context.Channel.Id, text, messageId, Context.User.Id);
		}

		[Command("rempersistent")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task RemovePersistentMessage()
		{
			Database database = Functions.InitializeDatabase(@"URI=file:db\persistent.db");

			bool tableExisting = false;
			if (database.IsTableExisting("persistent"))
				tableExisting = true;

			ulong channelId = Context.Channel.Id;
			var reader = database.GetValuesFromTable("persistent", parameters: $"WHERE channel_id = {channelId}");

			ulong lastMessageId = 0;
			string description = "";
			while (reader.Read() && lastMessageId == 0)
			{
				if ((ulong)reader.GetInt64(0) == channelId)
				{
					lastMessageId = (ulong)reader.GetInt64(2);
					description = reader.GetString(1);
				}
			}
			reader.Close();

			if (tableExisting && lastMessageId != 0)
			{
				var channelMessages = await Context.Channel.GetMessagesAsync(100, CacheMode.AllowDownload).FlattenAsync();

				foreach (var message in channelMessages)
					if (message.Id == lastMessageId)
						await message.DeleteAsync();

				var embed = new EmbedBuilder
				{
					Color = Color.Orange,
					Title = "Message persistant :",
					Description = "Suppression du message persistant :\n> " + description,
					Footer = new EmbedFooterBuilder()
					{
						IconUrl = Functions.GetAvatarUrl(Context.User, 32),
						Text = Context.User.Username + "#" + Context.User.Discriminator
					}
				};

				PersistentMessages.Channels.Remove(Context.Channel.Id);
				database.Delete("persistent", $"WHERE last_msg_id = {lastMessageId} OR channel_id = {channelId}");

				await Context.Channel.SendMessageAsync("", false, embed.Build());
				await Context.Message.DeleteAsync();
			}
			else
			{
				var embed = new EmbedBuilder
				{
					Color = Color.Orange,
					Title = "Message persistant :",
					Description = "Aucun message persistant n'a été trouvé dans ce channel.",
					Footer = new EmbedFooterBuilder()
					{
						IconUrl = Functions.GetAvatarUrl(Context.User, 32),
						Text = Context.User.Username + "#" + Context.User.Discriminator
					}
				};
				await Context.Channel.SendMessageAsync("", false, embed.Build());
			}
		}
	}
}
