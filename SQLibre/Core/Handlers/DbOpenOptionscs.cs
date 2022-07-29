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
		/// ctor
		/// </summary>
		/// <param name="path"><see cref="DatabasePath"/></param>
		/// <param name="flag"><see cref="OpenFlag"/></param>
		/// <param name="vfsName"><see cref="VfsName"/></param>
		public DbOpenOptions(string path, int flag, string? vfsName)
		{
			DatabasePath = Path.GetFullPath(path).ToUpper();
			OpenFlag = flag;
			VfsName = vfsName;
			Hash = DatabasePath.GetHashCode() & OpenFlag;
		}
		public override int GetHashCode() => Hash;
		public bool Equals(DbOpenOptions other) => this.OpenFlag == other.OpenFlag && this.DatabasePath == other.DatabasePath;
		public override bool Equals(object? obj) => obj != null && obj is DbOpenOptions && Equals((DbOpenOptions)obj);
	}
}
