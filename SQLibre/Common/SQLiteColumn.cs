using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;

namespace SQLibre
{
	public enum SQLiteColumnType : int
	{
		Integer = SQLITE_INTEGER,
		Float = SQLITE_FLOAT,
		Text = SQLITE_TEXT,
		Blob = SQLITE_BLOB,
		Null = SQLITE_NULL
	}
	/// <summary>
	/// Class for store table column info
	/// </summary>
	public readonly struct SQLiteColumn
	{
		/// <summary>
		/// Column name
		/// </summary>
		public string Name { get; }
		/// <summary>
		/// Column type <see cref="SQLiteColumnType"/>
		/// </summary>
		public SQLiteColumnType ColumnType { get; }
		/// <summary>
		/// Hash code from upper column name
		/// </summary>
		public int HashCode { get; }
		/// <summary>
		/// Create new <see cref="SQLiteColumn"/>
		/// </summary>
		/// <param name="name"><see cref="Name"/></param>
		/// <param name="columnType"><see cref="ColumnType"/></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SQLiteColumn(string name, SQLiteColumnType columnType)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ColumnType = columnType;
			HashCode = name.ToUpper().GetHashCode();
		}

		public override int GetHashCode() => HashCode;

		public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is SQLiteColumn col ? col.GetHashCode() == GetHashCode() : false;

		public static bool operator ==(SQLiteColumn left, SQLiteColumn right) => left.Equals(right);

		public static bool operator !=(SQLiteColumn left, SQLiteColumn right) => !(left == right);
	}
}
