using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLibre.Core;
using raw = SQLibre.Core.Raw;

namespace SQLibre
{
	/// <summary>
	/// SQLite open connection flags
	/// </summary>
	[Flags]
	public enum SQLiteOpenFlags
	{
		SQLITE_OPEN_READONLY = raw.SQLITE_OPEN_READONLY  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_READWRITE = raw.SQLITE_OPEN_READWRITE  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_CREATE = raw.SQLITE_OPEN_CREATE  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_DELETEONCLOSE = raw.SQLITE_OPEN_DELETEONCLOSE  /* VFS only */
		, SQLITE_OPEN_EXCLUSIVE = raw.SQLITE_OPEN_EXCLUSIVE  /* VFS only */
		, SQLITE_OPEN_AUTOPROXY = raw.SQLITE_OPEN_AUTOPROXY /* VFS only */
		, SQLITE_OPEN_URI = raw.SQLITE_OPEN_URI  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_MEMORY = raw.SQLITE_OPEN_MEMORY /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_MAIN_DB = raw.SQLITE_OPEN_MAIN_DB  /* VFS only */
		, SQLITE_OPEN_TEMP_DB = raw.SQLITE_OPEN_TEMP_DB  /* VFS only */
		, SQLITE_OPEN_TRANSIENT_DB = raw.SQLITE_OPEN_TRANSIENT_DB  /* VFS only */
		, SQLITE_OPEN_MAIN_JOURNAL = raw.SQLITE_OPEN_MAIN_JOURNAL  /* VFS only */
		, SQLITE_OPEN_TEMP_JOURNAL = raw.SQLITE_OPEN_TEMP_JOURNAL  /* VFS only */
		, SQLITE_OPEN_SUBJOURNAL = raw.SQLITE_OPEN_SUBJOURNAL /* VFS only */
		, SQLITE_OPEN_MASTER_JOURNAL = raw.SQLITE_OPEN_MASTER_JOURNAL /* VFS only */
		, SQLITE_OPEN_NOMUTEX = raw.SQLITE_OPEN_NOMUTEX  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_FULLMUTEX = raw.SQLITE_OPEN_FULLMUTEX  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_SHAREDCACHE = raw.SQLITE_OPEN_SHAREDCACHE /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_PRIVATECACHE = raw.SQLITE_OPEN_PRIVATECACHE  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_WAL = raw.SQLITE_OPEN_WAL  /* VFS only */
		, SQLITE_OPEN_NOFOLLOW = 0x01000000  /* Ok for sqlite3_open_v2() */
		, SQLITE_OPEN_EXRESCODE = 0x02000000  /* Extended result codes */
	}
	public enum SQLiteEncoding
	{
		Utf8 = raw.SQLITE_UTF8,
		Utf16le = raw.SQLITE_UTF16LE,
		Utf16be = raw.SQLITE_UTF16BE,
		Utf16 = raw.SQLITE_UTF16,  /* Use native byte order */
		SQLITE_ANY = raw.SQLITE_ANY,  /* sqlite3_create_function only */
		SQLITE_UTF16_ALIGNED = raw.SQLITE_UTF16_ALIGNED  /* sqlite3_create_function only */
	}

	public enum SQLiteJournalMode : int
	{
		DELETE,
		TRUNCATE,
		PERSIST,
		MEMORY,
		WAL,
		OFF
	}
	/// <summary>
	/// <see cref="SQLiteConnection"/> configuration options
	/// </summary>
	public sealed class SQLiteConnectionOptions
	{
		/// <summary>
		/// Default <see cref="SQLiteOpenFlags"/> constant for open sqlite3 database
		/// </summary>
		public const SQLiteOpenFlags DefaultOpenFlags = SQLiteOpenFlags.SQLITE_OPEN_CREATE
			| SQLiteOpenFlags.SQLITE_OPEN_READWRITE
			| SQLiteOpenFlags.SQLITE_OPEN_SHAREDCACHE
			| SQLiteOpenFlags.SQLITE_OPEN_FULLMUTEX;

		/// <summary>
		/// Default datetiem format for them string representation
		/// </summary>
		const string DateTimeSqliteDefaultFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";

		private DbOpenOptions _openOptions;
		/// <summary>
		/// Structure for usage as storage key for <see cref="SQLiteConnectionPool"/>class
		/// </summary>
		internal DbOpenOptions OpenOptions => _openOptions;
		/// <summary>
		/// Database file path.
		/// If assigned dirrectory path, then all .db files in folder will be attached as schemas to connected database
		/// if exists file dbo.db, that will be used as main database, other all will be attached
		/// else 'memory' used as main database, other all will be attached 
		/// </summary>
		public string DatabasePath => _openOptions.DatabasePath;
		/// <summary>
		/// The SQLite OS Interface <see href="https://www.sqlite.org/vfs.html"/>
		/// </summary>
		public string? VfsName => _openOptions.VfsName;
		/// <summary>
		/// Flags used on open sqlite3 connection <see href=""="https://www.sqlite.org/c3ref/open.html"/>
		/// </summary>
		public SQLiteOpenFlags OpenFlags { get; init; }
		/// <summary>
		/// If specified <see cref="DateTime"/> values will be stored as int ticks count
		/// </summary>
		public bool StoreDateTimeAsTicks { get; init; }
		/// <summary>
		/// If specified <see cref="TimeSpan"/> values will be stored as int ticks count
		/// </summary>
		public bool StoreTimeSpanAsTicks { get; init; }
		/// <summary>
		/// <see cref="DateTime" store format for usage string for store datatime data />
		/// </summary>
		public string DateTimeStringFormat { get; init; }
		/// <summary>
		/// Region depended datetime style
		/// </summary>
		public DateTimeStyles DateTimeStyle { get; init; }
		/// <summary>
		/// Password foe open encrypted data files
		/// </summary>
		public object? Key { get; init; }
		/// <summary>
		/// if true in <see cref="SQLiteConnection.Using()"/> method use automatic transaction 
		/// </summary>
		public bool UsingAutoCommit { get; set; }
		/// <summary>
		/// Command Timeout for prapere && runing command
		/// </summary>
		public int CommandTimeout { get; set; }
		/// <summary>
		/// Create new <see cref="SQLiteConnectionOptions"/> object
		/// </summary>
		/// <param name="databasePath"><see cref="DatabasePath"/></param>
		/// <param name="storeDateTimeAsTicks"><see cref="StoreDateTimeAsTicks"/></param>
		public SQLiteConnectionOptions(string databasePath, bool storeDateTimeAsTicks = false)
			: this(databasePath, DefaultOpenFlags, storeDateTimeAsTicks)
		{
		}

		public SQLiteConnectionOptions(string databasePath, bool storeDateTimeAsTicks, object? key = null, string? vfsName = null)
			: this(databasePath, DefaultOpenFlags, storeDateTimeAsTicks, key, vfsName)
		{
		}

		public SQLiteConnectionOptions(
			string databasePath,
			SQLiteOpenFlags openFlags,
			bool storeDateTimeAsTicks,
			object? key = null,
			string? vfsName = null,
			string dateTimeStringFormat = DateTimeSqliteDefaultFormat,
			bool storeTimeSpanAsTicks = true)
		{
			if (key != null && !(key is byte[] || key is string))
				throw new ArgumentException("Encryption keys must be strings or byte arrays", nameof(key));
			StoreDateTimeAsTicks = storeDateTimeAsTicks;
			StoreTimeSpanAsTicks = storeTimeSpanAsTicks;
			DateTimeStringFormat = dateTimeStringFormat;
			DateTimeStyle = "o".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) || "r".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) ? DateTimeStyles.RoundtripKind : DateTimeStyles.None; ;
			Key = key;
			_openOptions = new(databasePath, (int)openFlags, vfsName);
		}

		public SQLiteConnectionOptions(string connectionString)
		{
			var pairs = new KeyValuePairReader(connectionString, ";", "=", c => c != ' ', c => c != ' ');
			StoreDateTimeAsTicks = false;
			StoreTimeSpanAsTicks = false;
			DateTimeStringFormat = DateTimeSqliteDefaultFormat;
			Key = null;
			OpenFlags = DefaultOpenFlags;
			
			string path = string.Empty;
			string? vfsName = null;

			/// in <see cref="KeyValuePairReader.Read" method apce was removed from keys />
			for (;pairs.Read(out var pair);)
			{
				switch (pair.Key.ToUpper())
				{
					case "DATABASEPATH":
					case "DATASOURCE":
						path = pair.Value;
						break;
					case "STOREDATETIMEASTICKS":
						if (bool.TryParse(pair.Value, out bool storeDateTimeAsTicks))
							StoreDateTimeAsTicks = storeDateTimeAsTicks;
						else
							throw new ArgumentException("Invalid value for StoreDateTimeAsTicks parameter");
						break;
					case "DATETIMEFORMAT":
						StoreDateTimeAsTicks = "Ticks".Equals(pair.Value, StringComparison.OrdinalIgnoreCase);
						break;
					case "STORETIMESPANASTICKS":
						if (bool.TryParse(pair.Value, out bool storeTimeSpanAsTicks))
							StoreTimeSpanAsTicks = storeTimeSpanAsTicks;
						else
							throw new ArgumentException("Invalid value for StoreTimeSpanAsTicks parameter");
						break;
					case "DATETIMESTRINGFORMAT":
						DateTimeStringFormat = pair.Value;
						break;
					case "KEY":
					case "PASSWORD":
						Key = pair.Value;
						break;
					case "VFSNAME":
						vfsName = pair.Value;
						break;
					case "COMMANDTIMEOUT":
						CommandTimeout = int.TryParse(pair.Value, out int i) ? i : 0; ;
						break;
					case "READONLY":
						OpenFlags = (OpenFlags & ~SQLiteOpenFlags.SQLITE_OPEN_READWRITE) | SQLiteOpenFlags.SQLITE_OPEN_READONLY;
						break;
					case "JOURNALMODE":
					case "ENCODING":
					case "UTF16ENCODING":
					case "CACHESIZE":
					case "PAGESIZE":
						//add this
						break;
				}
			}
			DateTimeStyle = "o".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) 
				|| "r".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) 
				? DateTimeStyles.RoundtripKind 
				: DateTimeStyles.None;
			_openOptions = new(path, (int)OpenFlags, vfsName);
		}
		/// <summary>
		/// Using DatabasePath + Open flags hash code for pooling unique key
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
			=> DatabasePath.GetHashCode() & (int)OpenFlags;
	}
}
