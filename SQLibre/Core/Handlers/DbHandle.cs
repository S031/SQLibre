using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;

namespace SQLibre.Core
{
	public readonly ref struct DbHandle
	{
		public static DbHandle Empty => IntPtr.Zero;

		DbHandle(IntPtr handle) => Handle = handle;
		public IntPtr Handle { get; }
		public bool IsEmpty() => Handle == IntPtr.Zero;
		public unsafe string? FileName(string dbName = DbOpenOptions.MainDatabaseName) =>  (Utf8z)Raw.NativeMethods.sqlite3_db_filename(Handle, (Utf8z)dbName);
		public unsafe static DbHandle Open(string fileName, int flag, string? vfsName = null)
		{
			IntPtr p;
			int rc =  sqlite3_open_v2((Utf8z)fileName, out p, flag, (Utf8z)vfsName);
			SQLiteException.CheckOK(rc);
			return new(p);
		}
		public unsafe void Close()
		{
			int rc = sqlite3_close_v2(this);
			SQLiteException.CheckOK(rc);
		}

		public static implicit operator IntPtr(DbHandle db) => db.Handle;
		public static implicit operator DbHandle(IntPtr p) => new(p);
	}
}
