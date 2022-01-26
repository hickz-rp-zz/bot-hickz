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
		[Command("test")]
		[RequireOwner()]
		public async Task Test()
		{
			var builder = new ComponentBuilder()
				.WithButton("Test", "test", ButtonStyle.Primary);


			await ReplyAsync("teeeeeeeeeeeeeest", components: builder.Build());
		}
	}
}
