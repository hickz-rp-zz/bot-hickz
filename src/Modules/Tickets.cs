using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Hickz
{
	public class Tickets : ModuleBase<SocketCommandContext>
	{
		[Command("ticket-del")]
		public async Task TicketDelete([Remainder] string reason)
		{
			ITextChannel channelProperties = Context.Channel as ITextChannel;
			SocketUser userHelped = Context.Guild.GetUser(Convert.ToUInt64(Regex.Match(channelProperties.Topic, "[0-9]{17,18}").Value));

			var embed = new EmbedBuilder
			{
				Color = Color.DarkRed,
				Title = "📩 • Ticket de support : Fermeture de votre ticket.",
				Description = $"Le staff a supprimé votre ticket avec comme raison : \n*{reason}*\n\n|| Ne répondez pas à ce message ||",
				Timestamp = DateTime.Now,
				Footer = new EmbedFooterBuilder()
				{
					IconUrl = Functions.GetAvatarUrl(Context.User, 32),
					Text = $"Fermé par {Context.User.Username}#{Context.User.Discriminator}"
				}
			};

			await userHelped.SendMessageAsync(embed: embed.Build());
			await channelProperties.DeleteAsync();
		}
	}
}
