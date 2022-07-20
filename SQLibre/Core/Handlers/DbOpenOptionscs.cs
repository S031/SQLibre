using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre.Core
{
	internal readonly struct DbOpenOptions : IEquatable<DbOpenOptions>
	{
		public string DatabasePath { get; }
		public string? VfsName { get; }
		public int OpenFlag { get; }
		public int Hash { get; }
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
