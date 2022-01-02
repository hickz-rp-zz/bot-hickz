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

		private static Dictionary<ulong, StructPersistentMessages> _persistentMessages = Functions.ReadFromBinaryFile<Dictionary<ulong, StructPersistentMessages>>("data/persistent-messages");
		public static Dictionary<ulong, StructPersistentMessages> persistentMessages
		{
			get
			{
				foreach (KeyValuePair<ulong, StructPersistentMessages> kvp in _persistentMessages)
				{
					Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
				}
				return _persistentMessages;
			}
			set
			{
				_persistentMessages = value;
				Functions.WriteToBinaryFile("data/persistent-messages", _persistentMessages, true);
			}
		}
	}
}
