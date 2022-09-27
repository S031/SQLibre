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
		internal const string MainDatabaseName = "main";

		private IntPtr _handle;
		private int _inTransaction;

		private readonly SQLiteConnectionOptions _connectionOptions;
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

		public int Execute(string commandText)
			=> ExecuteInternal(_handle, (Utf8z)commandText);

		public int Execute(ReadOnlySpan<byte> commandText)
			=> ExecuteInternal(_handle, commandText);

		public void Using(Action<SQLiteContext> usingDelegate)
		{
			using (SQLiteContext ctx = new SQLiteContext(this))
			{
				if (!UsingAutoCommit)
					usingDelegate(ctx);
				else
				{
					try
					{
						BeginTransaction();
						usingDelegate(ctx);
						Commit();
					}
					catch
					{
						Rollback();
						throw;
					}
				}
			}
		}

		public T Using<T>(Func<SQLiteContext, T> usingDelegate)
		{
			using (SQLiteContext ctx = new SQLiteContext(this))
			{
				if (!UsingAutoCommit)
					return usingDelegate(ctx);
				else
				{
					try
					{
						BeginTransaction();
						var result = usingDelegate(ctx);
						Commit();
						return result;
					}
					catch
					{
						Rollback(); ;
						throw;
					}
				}
			}
		}

		public void Close() => Dispose();

		public void Dispose()
		{
			State = ConnectionState.Closed;
			GC.SuppressFinalize(this);
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

		public static void DropDb(SQLiteConnectionOptions options)
		{
			// add poll.Remove(connection) before 
			if (File.Exists(options.DatabasePath))
			{
				SQLiteConnectionPool.Remove(new SQLiteConnection(options).Handle);
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
	}
}
