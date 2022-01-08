using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hickz
{
	internal class PersistentMessages
	{
		// Object structure :
		// 0 -> Message Id
		// 1 -> Message Content
		// 2 -> Author Id
		public static Dictionary<ulong, object[]> Channels = new Dictionary<ulong, object[]>();
	}
}
