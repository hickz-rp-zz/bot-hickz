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
		public async Task PersistentMessage(string rgb = "0,255,0", [Remainder] string text = "")
		{
			string[] _rgb = rgb.Split(',');

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
				Color = new Color(Convert.ToByte(_rgb[0]), Convert.ToByte(_rgb[1]), Convert.ToByte(_rgb[2])),
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
			database.Close();
		}

		[Command("rempersistent")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task RemovePersistentMessage()
		{
			ulong channelId = Context.Channel.Id;

			if (PersistentMessages.Channels.ContainsKey(channelId))
			{
				var channelMessages = await Context.Channel.GetMessagesAsync(100, CacheMode.AllowDownload).FlattenAsync();

				foreach (var message in channelMessages)
					if (message.Id == (ulong)PersistentMessages.Channels[channelId][0])
						await message.DeleteAsync();

				var confirmationEmbed = new EmbedBuilder
				{
					Color = Color.Orange,
					Title = "Message persistant :",
					Description = "Suppression du message persistant :\n> " + PersistentMessages.Channels[channelId][1].ToString(),
					Footer = new EmbedFooterBuilder()
					{
						IconUrl = Functions.GetAvatarUrl(Context.User, 32),
						Text = Context.User.Username + "#" + Context.User.Discriminator
					}
				};

				Database database = Functions.InitializeDatabase(@"URI=file:db\persistent.db");
				database.Delete("persistent", $"WHERE last_msg_id = {PersistentMessages.Channels[channelId][0]} OR channel_id = {channelId}");
				database.Close();

				PersistentMessages.Channels.Remove(Context.Channel.Id);

				await Context.Channel.SendMessageAsync("", false, confirmationEmbed.Build());
				await Context.Message.DeleteAsync();
				return;
			}

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
