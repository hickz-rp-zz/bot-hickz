using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Hickz
{
    public static class Functions
    {
        public static async Task SetBotStatusAsync(DiscordSocketClient client)
        {
            await client.SetStatusAsync(UserStatus.Online);
            await client.SetGameAsync($"| mp → support", type: ActivityType.Listening);
		}

        public static JObject GetConfig()
        {
			using StreamReader configJson = new StreamReader(Directory.GetCurrentDirectory() + @"/config.json");
				return (JObject)JsonConvert.DeserializeObject(configJson.ReadToEnd());
        }

        public static string GetAvatarUrl(SocketUser user, ushort size = 1024)
        {
            return user.GetAvatarUrl(size: size) ?? user.GetDefaultAvatarUrl(); 
        }

		/// <summary>
		/// Writes the given object instance to a binary file.
		/// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
		/// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
		/// </summary>
		/// <typeparam name="T">The type of object being written to the XML file.</typeparam>
		/// <param name="filePath">The file path to write the object instance to.</param>
		/// <param name="objectToWrite">The object instance to write to the XML file.</param>
		/// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
		public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
		{
			using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
			{
				var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				binaryFormatter.Serialize(stream, objectToWrite);
			}
		}

		/// <summary>
		/// Reads an object instance from a binary file.
		/// </summary>
		/// <typeparam name="T">The type of object to read from the XML.</typeparam>
		/// <param name="filePath">The file path to read the object instance from.</param>
		/// <returns>Returns a new instance of the object read from the binary file.</returns>
		public static T ReadFromBinaryFile<T>(string filePath)
		{
			using (Stream stream = File.Open(filePath, FileMode.Open))
			{
				if (stream.Length != 0)
				{
					var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
					return (T)binaryFormatter.Deserialize(stream);
				}
				else
				{
					return default;
				}
			}
		}

		public static void ColoredMessage(ConsoleColor bgColor = ConsoleColor.Black, ConsoleColor fgColor = ConsoleColor.White, string msg = "")
		{
			Console.BackgroundColor = bgColor;
			Console.ForegroundColor = fgColor;
			Console.WriteLine(msg);
			Console.ResetColor();
		}
	}
}
