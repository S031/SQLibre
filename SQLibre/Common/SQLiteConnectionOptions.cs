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

	public sealed class SQLiteConnectionOptions
	{
		public const SQLiteOpenFlags DefaultOpenFlags = SQLiteOpenFlags.SQLITE_OPEN_CREATE
			| SQLiteOpenFlags.SQLITE_OPEN_READWRITE
			| SQLiteOpenFlags.SQLITE_OPEN_SHAREDCACHE
			| SQLiteOpenFlags.SQLITE_OPEN_FULLMUTEX;

		const string DateTimeSqliteDefaultFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff";
		private DbOpenOptions _openOptions;
		internal DbOpenOptions OpenOptions => _openOptions;
		public string DatabasePath => _openOptions.DatabasePath;
		public string? VfsName => _openOptions.VfsName;
		public SQLiteOpenFlags OpenFlags { get; init; }
		public bool StoreDateTimeAsTicks { get; init; }
		public bool StoreTimeSpanAsTicks { get; init; }
		public string DateTimeStringFormat { get; init; }
		public DateTimeStyles DateTimeStyle { get; init; }
		public object? Key { get; init; }

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
			var pairs = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
			StoreDateTimeAsTicks = false;
			StoreTimeSpanAsTicks = false;
			DateTimeStringFormat = DateTimeSqliteDefaultFormat;
			Key = null;
			OpenFlags = DefaultOpenFlags;
			
			string path = string.Empty;
			string? vfsName = null;

			foreach (var pair in pairs)
			{
				int p = pair.IndexOf('=');
				if (p == -1)
					throw new ArgumentException("Connection string has incorrect format");
				var key = pair.Substring(0, p);
				var value = pair.Substring(p + 1);
				switch (key.ToUpper())
				{
					case "DATABASEPATH":
						path = value; ;
						break;
					case "STOREDATETIMEASTICKS":
						if (bool.TryParse(value, out bool storeDateTimeAsTicks))
							StoreDateTimeAsTicks = storeDateTimeAsTicks;
						else
							throw new ArgumentException("Invalid value for StoreDateTimeAsTicks parameter");
						break;
					case "STORETIMESPANASTICKS":
						if (bool.TryParse(value, out bool storeTimeSpanAsTicks))
							StoreTimeSpanAsTicks = storeTimeSpanAsTicks;
						else
							throw new ArgumentException("Invalid value for StoreTimeSpanAsTicks parameter");
						break;
					case "DATETIMESTRINGFORMAT":
						DateTimeStringFormat = value;
						break;
					case "KEY":
						key = value;
						break;
					case "VFSNAME":
						vfsName = value;
						break;
				}
			}
			DateTimeStyle = "o".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) 
				|| "r".Equals(DateTimeStringFormat, StringComparison.OrdinalIgnoreCase) 
				? DateTimeStyles.RoundtripKind 
				: DateTimeStyles.None;
			_openOptions = new(path, (int)OpenFlags, vfsName);
		}

		public override int GetHashCode()
			=> DatabasePath.GetHashCode() & (int)OpenFlags;
	}
}
