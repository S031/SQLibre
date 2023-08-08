using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

using SQLibre.Core;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;
using static SQLibre.SQLiteException;
using System.Runtime.Loader;
using System.Windows.Input;

namespace SQLibre
{
	public enum ConnectionState : int
	{
		Closed = 0,
		Open = 1,
		Connecting = Open << 1,
		Executing = Connecting << 1,
		Fetching = Executing << 1,
		Broken = Fetching << 1
	}
	/// <summary>
	/// ToDo:
	/// 1.	Command.Reader property
	/// 2.	Test Dispose() for all common objects in any variant using them
	/// 3.	Reader.NextResult call sqlite3.enable_sqlite3_next_stmt(true)
	/// 4.	Import common featuries from SQLitePCL.Ugly
	/// </summary>
	public sealed class SQLiteConnection : IDisposable
	{
		internal const string MainDatabaseName = Core.DbOpenOptions.MainDatabaseName;
		private static readonly object _lock = new object();

		private IntPtr _handle;
		private int _inTransaction;

		private readonly SQLiteConnectionOptions _connectionOptions;
		private readonly List<WeakReference<SQLiteCommand>> _commands = new();

		public string DateTimeSqliteDefaultFormat => _connectionOptions.DateTimeStringFormat;
		public bool StoreDateTimeAsTicks => _connectionOptions.StoreDateTimeAsTicks;
		public bool StoreTimeSpanAsTicks => _connectionOptions.StoreTimeSpanAsTicks;
		public System.Globalization.DateTimeStyles DateTimeStyle => _connectionOptions.DateTimeStyle;
		public int HashCode => _connectionOptions.GetHashCode();
		public string DatabasePath => _connectionOptions.DatabasePath;
		public DbHandle Handle => _handle;
		public ConnectionState State { get; internal set; } = ConnectionState.Closed;
		public bool UsingAutoCommit { get => _connectionOptions.UsingAutoCommit; set => _connectionOptions.UsingAutoCommit = value; }


		public static void Config(int configOptions, int value)
			=> CheckOK(sqlite3_config(configOptions, value));

		public static void Config(int configOptions)
			=> CheckOK(sqlite3_config(configOptions));

		public SQLiteConnection(string connectionString) : this(new SQLiteConnectionOptions(connectionString))
		{
		}

		public SQLiteConnection(SQLiteConnectionOptions connectionOptions)
		{
			_connectionOptions = connectionOptions;
			_handle = SQLiteConnectionPool.GetConnection(_connectionOptions.OpenOptions, 100000,
				db => ExecuteInternal(db, (Utf8z)$"PRAGMA journal_mode={connectionOptions.JournalMode}"));
			State = ConnectionState.Open;
		}

		public void EnableWriteAhead() => Execute("PRAGMA journal_mode=WAL");

		public void BeginTransaction()
		{
			if (_inTransaction == 1)
				throw new InvalidOperationException("transaction already active");
			Interlocked.Increment(ref _inTransaction);
			Execute("begin transaction;");
		}

		public void Commit()
		{
			if (_inTransaction == 0)
				throw new InvalidOperationException("transaction does not exist");
			Execute("commit transaction;");
			Interlocked.Decrement(ref _inTransaction);
		}

		public void Rollback()
		{
			if (_inTransaction == 0)
				throw new InvalidOperationException("transaction does not exist");
			Execute("rollback transaction;");
		}

		public static void DropDb(SQLiteConnectionOptions options)
		{
			if (File.Exists(options.DatabasePath))
			{
				SQLiteConnectionPool.Remove(new SQLiteConnection(options).Handle, true);
				File.Delete(options.DatabasePath);
			}
		}

		public static unsafe void CreateDb(SQLiteConnectionOptions options, SQLiteEncoding encoding = SQLiteEncoding.Utf8, SQLiteJournalMode journalMode = SQLiteJournalMode.WAL)
		{
			if (File.Exists(options.DatabasePath))
				return;

			IntPtr db = IntPtr.Zero;
			try
			{
				var rc = NativeMethods.sqlite3_open_v2((Utf8z)options.DatabasePath, out db, (int)options.OpenFlags, (Utf8z)options.VfsName);
				SQLiteException.CheckOK(db, rc);
				ExecuteInternal(db, (Utf8z)$"PRAGMA journal_mode={journalMode};");
				ExecuteInternal(db, (Utf8z)$"PRAGMA encoding={encoding};");
			}
			catch
			{
				throw;
			}
			finally
			{
				if (db != IntPtr.Zero)
				{
					var rc = sqlite3_close_v2(db);
					SQLiteException.CheckOK(db, rc);
				}
			}
		}

		public int Execute(string commandText)
			=> ExecuteInternal(_handle, (Utf8z)commandText);

		public int Execute(ReadOnlySpan<byte> commandText)
			=> ExecuteInternal(_handle, commandText);

		internal static unsafe int ExecuteInternal(DbHandle db, ReadOnlySpan<byte> statement)
		{
			var rc = sqlite3_exec(db, (Utf8z)statement, IntPtr.Zero, IntPtr.Zero, out var p_errMsg);
			if (p_errMsg != IntPtr.Zero)
			{
				var error = new Utf8z(p_errMsg).ToString();
				sqlite3_free(p_errMsg);
				throw new SQLiteException(rc, error);
			}
			return rc;
		}

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

		public long LastInsertRowId()
			=> sqlite3_last_insert_rowid(Handle);

		public void Close() => Dispose();

		public void Dispose()
		{
			ClearCommandsCollection();
			State = ConnectionState.Closed;
			SQLiteConnectionPool.Remove(Handle, false);
			GC.SuppressFinalize(this);
		}
		private void ClearCommandsCollection()
		{
			Monitor.Enter(_lock);
			try
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
			catch { throw; }
			finally { Monitor.Exit(_lock); }
		}

		internal void AddCommand(SQLiteCommand command)
		{
			Monitor.Enter(_lock);
			try
			{
				_commands.Add(new WeakReference<SQLiteCommand>(command));
			}
			catch { throw; }
			finally { Monitor.Exit(_lock); }
		}

		internal void RemoveCommand(SQLiteCommand command)
		{
			Monitor.Enter(_lock);
			try
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
			catch { throw; }
			finally { Monitor.Exit(_lock); }
		}

	}
}
