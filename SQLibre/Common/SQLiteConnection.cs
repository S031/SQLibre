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
using System.Data;

namespace SQLibre
{
	/// <summary>
	/// ToDo:
	/// 1.	Command.Reader property
	/// 2.	Test Dispose() for all common objects in any variant using them
	/// 3.	Reader.NextResult call sqlite3.enable_sqlite3_next_stmt(true)
	/// 4.	Import common featuries from SQLitePCL.Ugly
	/// </summary>
	public sealed class SQLiteConnection : IDbConnection
	{
		internal const string main_database_name = Core.DbOpenOptions.MainDatabaseName;
		internal const string memorydb_connection_string = $"DataSource={DbOpenOptions.MemoryDb}";

		private static readonly object _lock = new object();

		private IntPtr _handle;

		private SQLiteConnectionOptions _connectionOptions;
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
		internal SQLiteConnectionOptions ConnectionOptions => _connectionOptions;
		internal SQLiteTransaction? Transaction { get; set; }

		private string _connsectionString;
		public string ConnectionString
		{
			get => _connsectionString;
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
			set
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
			{
				if (State != ConnectionState.Closed)
					throw new InvalidOperationException("Connection already opened");
				_connsectionString = value ?? DbOpenOptions.MemoryDb;
				_connectionOptions = new SQLiteConnectionOptions(_connsectionString);
			}
		}

		public int ConnectionTimeout => _connectionOptions.CommandTimeout;

		public string Database => Path.GetFileNameWithoutExtension(_connectionOptions.DatabasePath);

		public static void Config(int configOptions, int value)
			=> CheckOK(sqlite3_config(configOptions, value));

		public static void Config(int configOptions)
			=> CheckOK(sqlite3_config(configOptions));

		public SQLiteConnection()
		{
		}

		public SQLiteConnection(string connectionString) : this(new SQLiteConnectionOptions(connectionString))
		{
			_connsectionString = connectionString;
		}

		public SQLiteConnection(SQLiteConnectionOptions connectionOptions)
		{
			if (string.IsNullOrEmpty(_connsectionString))
				_connsectionString = connectionOptions.DatabasePath;

			_connectionOptions = connectionOptions;
			Open();
		}

		public void EnableWriteAhead() => Execute("PRAGMA journal_mode=WAL"u8);

		public IDbTransaction BeginTransaction()
			=>BeginTransaction(IsolationLevel.Serializable);

		public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			CheckOpenState(nameof(BeginTransaction));
			if (Transaction != null)
				throw new InvalidOperationException("transaction already active");
			Transaction = new SQLiteTransaction(this, isolationLevel);
			return Transaction;
		}

		public void Commit()
		{
			CheckOpenState(nameof(Commit));
			if (Transaction == null)
				throw new InvalidOperationException("transaction does not exist");
			Transaction.Commit();
		}

		public void Rollback()
		{
			CheckOpenState(nameof(Rollback));
			if (Transaction == null)
				throw new InvalidOperationException("transaction does not exist");
			Transaction.Commit();
		}

		public static void DropDb(SQLiteConnectionOptions options)
		{
			if (File.Exists(options.DatabasePath))
			{
				using (var cn = new SQLiteConnection(options))
					SQLiteConnectionPool.Remove(cn.Handle, true);
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
		{
			CheckOpenState(nameof(Execute));
			return ExecuteInternal(_handle, (Utf8z)commandText);
		}

		public int Execute(ReadOnlySpan<byte> commandText)
		{
			CheckOpenState(nameof(Execute));
			return ExecuteInternal(_handle, commandText);
		}

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
			CheckOpenState(nameof(Execute));
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
			CheckOpenState(nameof(ExecuteScalar));

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
			CheckOpenState(nameof(ExecuteReader));

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
			Transaction?.Dispose();
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

		public void ChangeDatabase(string databaseName) => throw new NotSupportedException();

		public void Open()
		{
			if (State == ConnectionState.Open)
				return;

			if (State != ConnectionState.Closed)
				throw new InvalidOperationException("Connection already opened");

			_handle = SQLiteConnectionPool.GetConnection(_connectionOptions.OpenOptions, 100000,
				db => ExecuteInternal(db, (Utf8z)$"PRAGMA journal_mode={_connectionOptions.JournalMode}"));
			State = ConnectionState.Open;
		}

		private void CheckOpenState(string source)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException($"Method {source} required opened connection");
		}

		IDbCommand IDbConnection.CreateCommand()
		{
			throw new NotImplementedException();
		}
	}
}
