using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using SQLibre.Core;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;
using static SQLibre.SQLiteException;
using System.Text.Json;

namespace SQLibre
{
	public sealed class SQLiteReader : IDisposable
	{
		private IntPtr _stmt;
		private SQLiteColumnCollection? _collumns;
		private object?[]? _values;
		private readonly SQLiteContext _context;
		private SQLIteCommand _command;
		private bool _storeDateTimeAsTicks;
		private string _dateTimeSqliteDefaultFormat;
		private System.Globalization.DateTimeStyles _dateTimeStyle;

		internal SQLiteReader(SQLiteContext context, SQLIteCommand command)
		{
			_context = context;
			_storeDateTimeAsTicks = context.Connection.StoreDateTimeAsTicks;
			_dateTimeSqliteDefaultFormat = context.Connection.DateTimeSqliteDefaultFormat;
			_dateTimeStyle = context.Connection.DateTimeStyle;

			_command = command;
			_stmt = command.Handle;
		}

		private static InvalidOperationException OperationImpossible(string name) => new InvalidOperationException($"Operation {name} is not possible for current reader state");

		public object? this[int index]
			=> _collumns == null ? throw OperationImpossible("this[int index]") : Values?[index];

		public object? this[string index]
			=> _collumns == null ? throw OperationImpossible("this[string index]") : Values?[_collumns.ColumnIndex(index)];

		public bool Read()
		{
			var result = sqlite3_step(_stmt);
			if (result == Raw.SQLITE_ROW)
			{
				if (_collumns == null)
				{
					int count = sqlite3_column_count(_stmt);
					_collumns = new(count);
					_values = null;
					for (int i = 0; i < count; i++)
					{
						_collumns[i] = ReadCol(_stmt, i);
					}
				}
				return true;
			}
			_collumns?.Clear();
			_collumns = null;
			_values = null;
			return false;
		}

		public object?[] Values
		{
			get
			{
				if (_values == null)
				{
					int count = (_collumns ?? throw OperationImpossible(nameof(Values))).Count;
					_values = new object[count];
					for (int i = 0; i < count; i++)
					{
						_values[i] = ReadColValue(_stmt, i, _collumns[i].ColumnType);
					}
				}
				return _values;
			}
		}

		private static unsafe SQLiteColumn ReadCol(IntPtr stmt, int index)
		{
			string? name = (Utf8z)sqlite3_column_name(stmt, index);
			if (name == null)
				throw new ArgumentOutOfRangeException(nameof(index));
			var type = (SQLiteColumnType)sqlite3_column_type(stmt, index);
			return new(name, type);
		}

		private static unsafe object? ReadColValue(IntPtr stmt, int index, SQLiteColumnType type)
			=> type switch
			{
				SQLiteColumnType.Integer => sqlite3_column_int64(stmt, index),
				SQLiteColumnType.Float => sqlite3_column_double(stmt, index),
				SQLiteColumnType.Text => Raw.sqlite3_column_text(stmt, index).ToString(),
				SQLiteColumnType.Blob => Raw.sqlite3_column_blob(stmt, index).ToArray(),
				SQLiteColumnType.Null => null,
				_ => throw new InvalidCastException()
			};

		//public bool NextResult()
		//{
		//	_stmt = sqlite3_next_stmt(_connection.Handle, _stmt);
		//	if (!_stmt.IsInvalid)
		//	{
		//		var result = (Result)sqlite3_step(_stmt);
		//		return result == Result.Done;
		//	}
		//	// get first
		//	_stmt = sqlite3_next_stmt(_connection.Handle, Sqlite3Statement.From(IntPtr.Zero, _connection.Handle));
		//	return false;
		//}

		public void Dispose()
		{
			var rc = sqlite3_reset(_stmt);
			CheckOK(rc);
			if (_collumns != null)
			{
				_collumns?.Clear();
				_collumns = null;
			}
			GC.SuppressFinalize(this);
		}

		#region get_values
		public object? GetValue(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetValue));
			return ReadColValue(_stmt, index, _collumns[index].ColumnType);
		}
		public bool GetBoolean(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetBoolean));
			return sqlite3_column_int(_stmt, index) != 0;
		}
		public byte GetByte(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetByte));
			return Convert.ToByte(sqlite3_column_int(_stmt, index));
		}
		public sbyte GetSByte(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetSByte));
			return Convert.ToSByte(sqlite3_column_int(_stmt, index));
		}

		public char GetChar(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetChar));
			return Convert.ToChar(sqlite3_column_int(_stmt, index));
		}
		public short GetInt16(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetInt16));
			return Convert.ToInt16(sqlite3_column_int(_stmt, index));
		}

		public ushort GetUInt16(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetUInt16));
			return Convert.ToUInt16(sqlite3_column_int(_stmt, index));
		}

		public int GetInt32(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetInt32));
			return sqlite3_column_int(_stmt, index);
		}

		public uint GetUInt32(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetUInt32));
			return Convert.ToUInt32(sqlite3_column_int64(_stmt, index));
		}
		public long GetInt64(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetInt64));
			return sqlite3_column_int64(_stmt, index);
		}

		public ulong GetUInt64(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetUInt64));
			return Convert.ToUInt64(sqlite3_column_int64(_stmt, index));
		}

		public float GetSingle(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetSingle));
			return Convert.ToSingle(sqlite3_column_double(_stmt, index));
		}

		public double GetDouble(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetDouble));
			return sqlite3_column_double(_stmt, index);
		}

		public decimal GetDecimal(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetDecimal));
			return Convert.ToDecimal(sqlite3_column_double(_stmt, index));
		}

		public string? GetString(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetString));
			return Raw.sqlite3_column_text(_stmt, index);
		}
		
		public Utf8z GetUtf8String(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetUtf8String));
			return Raw.sqlite3_column_text(_stmt, index);
		}

		public byte[]? GetByteArray(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetByteArray));
			return Raw.sqlite3_column_blob(_stmt, index).ToArray();
		}

		public ReadOnlySpan<byte> GetBytes(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetBytes));
			return Raw.sqlite3_column_blob(_stmt, index);
		}

		public DateTime GetDateTime(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(GetString));
			if (_storeDateTimeAsTicks)
				return new DateTime(sqlite3_column_int64(_stmt, index));

			string? text = Raw.sqlite3_column_text(_stmt, index);

			return GetDateTime(text, _dateTimeSqliteDefaultFormat, _dateTimeStyle);
		}

		private static DateTime GetDateTime(string? value,
			string dateTimeSqliteDefaultFormat,
			System.Globalization.DateTimeStyles dateTimeStyle)
		{
			DateTime resultDate;
			if (!DateTime.TryParseExact(value, dateTimeSqliteDefaultFormat, System.Globalization.CultureInfo.InvariantCulture, dateTimeStyle, out resultDate))
				if (!DateTime.TryParse(value, out resultDate))
					throw new InvalidCastException(value);

			return resultDate;
		}

		public bool IsNull(int index)
		{
			if (_collumns == null)
				throw OperationImpossible(nameof(IsNull));
			return _collumns[index].ColumnType == SQLiteColumnType.Null;
		}

		#endregion get_values
	}
}
