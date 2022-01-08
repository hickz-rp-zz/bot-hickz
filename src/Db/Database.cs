using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;

namespace Hickz
{
	public class Database
	{
		SQLiteConnection con = null;

		public Database(string connectionString)
		{
			con = new SQLiteConnection(connectionString);
			con.Open();
		}

		public void Insert(params object[] requestInformations)
		{
			using var cmd = new SQLiteCommand(con);
			cmd.CommandText = requestInformations[0].ToString();

			if (requestInformations.Length > 1) // Si on a des paramètres en plus (donc des valeurs de table)
			{
				for (int i = 1; i < requestInformations.Length; i++)
					cmd.Parameters.AddWithValue($"@{i}", requestInformations[i]);

				cmd.Prepare();
			}

			cmd.ExecuteNonQuery();
		}

		public void CreateTable(string name, string parameters)
		{
			using var cmd = new SQLiteCommand(con);

			cmd.CommandText = $@"CREATE TABLE {name}({parameters})";
			Console.WriteLine("created");
			cmd.ExecuteNonQuery();
		}

		public bool IsTableExisting(string name)
		{
			using var cmd = new SQLiteCommand(con);
			cmd.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{name}'";
			return cmd.ExecuteReader().HasRows;
		}

		public SQLiteDataReader GetValuesFromTable(string tableName, string selection = "*", string parameters = "")
		{
			using var cmd = new SQLiteCommand(con);
			cmd.CommandText = $"SELECT {selection} FROM {tableName} {parameters}";
			
			return cmd.ExecuteReader();
		}

		public void Delete(string tableName, string whereCondition)
		{
			using var cmd = new SQLiteCommand(con);
			cmd.CommandText = $"DELETE FROM {tableName} {whereCondition}";

			cmd.ExecuteNonQuery();
		}

		public void Update(string tableName, string setInstructions)
		{
			using var cmd = new SQLiteCommand(con);
			cmd.CommandText = $"UPDATE {tableName} {setInstructions}";

			cmd.ExecuteNonQuery();
		}

		~Database() => con.Dispose();
	}
}
