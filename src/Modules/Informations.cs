using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Globalization;

namespace Hickz
{
    public class Informations : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        public async Task Ping() => await ReplyAsync($"Latence : {Context.Client.Latency} ms");

		[Command("update")]
		[RequireOwner()]
		public async Task UpdateBotInformations(string arg, string arg2 = "")
		{
			if (arg == "status") await Context.Client.SetStatusAsync((UserStatus)int.Parse(arg2));
		}
	}
}
