using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hickz
{
	internal class PersistentMessages
	{
		public struct StructPersistentMessages
		{
			public EmbedBuilder embed;
			public ulong lastMessage;
		}

		public static Dictionary<ulong, StructPersistentMessages> persistentMessages { get; set; }
	}
}
