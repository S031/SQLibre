using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre.Core
{
	/// <summary>
	/// Structure for usage as storage key for <see cref="SQLiteConnectionPool"/>class
	/// </summary>
	internal readonly struct DbOpenOptions : IEquatable<DbOpenOptions>
	{
		internal const string MainDatabaseName = "main";
		internal const string MemoryDb = ":memory:";

		/// <summary>
		/// File or folder name (or uri) to use to create sqlite3 connection
		/// </summary>
		public string DatabasePath { get; }
		/// <summary>
		/// The SQLite OS Interface <see href="https://www.sqlite.org/vfs.html"/>
		/// </summary>
		public string? VfsName { get; }
		/// <summary>
		/// Flags used on open sqlite3 connection <see href=""="https://www.sqlite.org/c3ref/open.html"/>
		/// </summary>
		public int OpenFlag { get; }
		/// <summary>
		/// Used for <see cref="SQLiteConnectionPool"/> search key
		/// </summary>
		public int Hash { get; }
		/// <summary>
		///     Gets or sets a value indicating whether the connection will be pooled.
		/// </summary>
		/// <value>A value indicating whether the connection will be pooled.</value>
		public bool Pooling { get; }
		/// <summary>
		/// ctor
		/// </summary>
		/// <param name="path"><see cref="DatabasePath"/></param>
		/// <param name="flag"><see cref="OpenFlag"/></param>
		/// <param name="vfsName"><see cref="VfsName"/></param>
		public DbOpenOptions(string path, int flag, string? vfsName, bool pooling)
		{
			bool memory = string.IsNullOrEmpty(path) || path.Equals(MemoryDb, StringComparison.OrdinalIgnoreCase);
			DatabasePath = memory ? MemoryDb : Path.GetFullPath(path).ToUpper();
			OpenFlag = flag;
			VfsName = vfsName;
			Pooling = pooling;
			Hash = DatabasePath.GetHashCode() & OpenFlag;
		}
		public override int GetHashCode() => Hash;
		public bool Equals(DbOpenOptions other) => this.OpenFlag == other.OpenFlag && this.DatabasePath == other.DatabasePath;
		public override bool Equals(object? obj) => obj != null && obj is DbOpenOptions && Equals((DbOpenOptions)obj);
	}
}
