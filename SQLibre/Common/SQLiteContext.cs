using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLibre.Core;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;
using static SQLibre.SQLiteException;

namespace SQLibre
{
	public sealed class SQLiteContext : IDisposable
	{
		private readonly SQLiteConnection _connection;
		private readonly List<WeakReference<SQLiteCommand>> _commands = new();

		internal SQLiteContext(SQLiteConnection connection)
		{
			_connection = connection;
		}

		public SQLiteConnection Connection => _connection;

		public void Dispose()
		{
			ClearCommandsCollection();
			GC.SuppressFinalize(this);
		}

		public DbHandle Handle => _connection.Handle;

		public void BeginTransaction() => _connection.BeginTransaction();

		public void Commit() => _connection.Commit();

		public void Rollback() => _connection.Rollback();

		public int Execute(string commandText)
			=> SQLiteConnection.ExecuteInternal(_connection.Handle, (Utf8z)commandText);

		public int Execute(ReadOnlySpan<byte> commandText)
			=> SQLiteConnection.ExecuteInternal(_connection.Handle, commandText);

		public int Execute(string commandText, params object[] parameters)
		{
			using (SQLiteCommand cmd = CreateCommand(commandText))
			{
				int i = 0;
				string pName = string.Empty;

				foreach (var p in parameters)
				{
					if (i % 2 == 0)
						pName = (string)(p ?? throw new ArgumentNullException(nameof(parameters)));
					else
						cmd.Bind(pName, p);
					i++;
				}
				return cmd.Execute();
			}
		}

		public T? ExecuteScalar<T>(string commandText, params object[] parameters)
		{
			using (SQLiteCommand cmd = CreateCommand(commandText))
			{
				int i = 0;
				string pName = string.Empty;

				foreach (var p in parameters)
				{
					if (i % 2 == 0)
						pName = (string)(p ?? throw new ArgumentNullException(nameof(parameters)));
					else
						cmd.Bind(pName, p);
					i++;
				}

				using (var r = cmd.ExecuteReader())
				{
					if (r.Read())
						return (T?)r.GetValue(0);
					return default;
				}
			}
		}

		public SQLiteReader ExecuteReader(string commandText, params object[] parameters)
		{
			SQLiteCommand cmd = CreateCommand(commandText);
			int i = 0;
			string pName = string.Empty;

			foreach (var p in parameters)
			{
				if (i % 2 == 0)
					pName = (string)(p ?? throw new ArgumentNullException(nameof(parameters)));
				else
					cmd.Bind(pName, p);
				i++;
			}

			return cmd.ExecuteReader();
		}

		public SQLiteCommand CreateCommand(string statement) => CreateCommand((ReadOnlySpan<byte>)(Utf8z)statement);

		public SQLiteCommand CreateCommand(ReadOnlySpan<byte> statement)
		{
			var cmd = new SQLiteCommand(this, statement);
			AddCommand(cmd);
			return cmd;
		}

		private void ClearCommandsCollection()
		{

			for (var i = _commands.Count - 1; i >= 0; i--)
			{
				var reference = _commands[i];
				if (reference.TryGetTarget(out var command))
					command.Dispose();
				else
					_commands.RemoveAt(i);
			}

		}

		internal void AddCommand(SQLiteCommand command)
			=> _commands.Add(new WeakReference<SQLiteCommand>(command));

		internal void RemoveCommand(SQLiteCommand command)
		{
			for (var i = _commands.Count - 1; i >= 0; i--)
			{
				if (_commands[i].TryGetTarget(out var item)
					&& item == command)
				{
					_commands.RemoveAt(i);
				}
			}
		}
		public long LastInsertRowId()
			=> sqlite3_last_insert_rowid(Handle);
	}
}
