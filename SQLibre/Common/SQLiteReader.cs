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
using System.Diagnostics;
using System.Data.Common;
using System.Reflection.Metadata;

namespace SQLibre
{
	enum StepInfo
	{
		None = 0,
		HasRows = 1,
		HasNoRows = 2
	}
	/// <summary>
	/// ADO.NET DataReader like Read only && forward only cursor for result data rows iteration
	/// </summary>
	public sealed class SQLiteReader : IDisposable
	{
		private int _recordsAffected = -1;
		private List<IntPtr> _statements;
		private SQLiteColumnCollection? _collumns;
		private object?[]? _values;
		private readonly SQLiteContext _context;
		private SQLiteCommand _command;
		private bool _storeDateTimeAsTicks;
		private string _dateTimeSqliteDefaultFormat;
		private System.Globalization.DateTimeStyles _dateTimeStyle;
		private int _current;
		private StepInfo _step;
		private IntPtr _stmt;

		internal SQLiteReader(SQLiteCommand command)
		{
			_context = command.Context;
			_statements = command.Statements;
			_storeDateTimeAsTicks = _context.Connection.StoreDateTimeAsTicks;
			_dateTimeSqliteDefaultFormat = _context.Connection.DateTimeSqliteDefaultFormat;
			_dateTimeStyle = _context.Connection.DateTimeStyle;
			_command = command;
			_current = -1;
			//_stmt = _statements[_current];
			_step = StepInfo.None;
		}

		private static InvalidOperationException OperationImpossible(string name) => new InvalidOperationException($"Operation {name} is not possible for current reader state");
		public int RecordsAffected => _recordsAffected;

		public unsafe string? Sql => ((Utf8z)sqlite3_sql(_stmt)).ToString();

		public object? this[int index]
			=> _collumns == null ? throw OperationImpossible("this[int index]") : Values?[index];

		public object? this[string index]
			=> _collumns == null ? throw OperationImpossible("this[string index]") : Values?[_collumns.ColumnIndex(index)];

		public int FieldCount
			=> _collumns == null ? throw OperationImpossible("FieldCount") : _collumns.Count;

		public bool Read()
		{
			if (_step != StepInfo.None)
			{
				var stepRersult = _step; 
				_step = StepInfo.None;
				return stepRersult == StepInfo.HasRows;
			}

			var result = sqlite3_step(_stmt);
			CheckOK(_context.Handle, result);
			if (result != Raw.SQLITE_DONE)
			{
				if (_collumns == null)
					ReadColumns();
				_values = null;
				return true;
			}
			_collumns?.Clear();
			_collumns = null;
			_values = null;
			return false;
		}
		
		private void ReadColumns()
		{
			int count = sqlite3_column_count(_stmt);
			_collumns = new(count);
			_values = null;
			for (int i = 0; i < count; i++)
			{
				_collumns[i] = ReadCol(_stmt, i);
			}
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

		public bool NextResult()
		{
			int rc;
			_step = StepInfo.None;
			Stopwatch _timer = new();
			for (int i = _current + 1; i < _statements.Count; i++)
			{
				var stmt = _statements[i];
				try
				{
					_timer.Start();

					while (IsBusy(rc = sqlite3_step(stmt)))
					{
						if (_command.CommandTimeout != 0
							&& _timer.ElapsedMilliseconds >= _command.CommandTimeout * 1000L)
						{
							break;
						}

						_ = sqlite3_reset(stmt);

						// TODO: Consider having an async path that uses Task.Delay()
						Thread.Sleep(150);
					}

					_timer.Stop();

					CheckOK(_context.Handle, rc);

					// It's a SELECT statement
					if (sqlite3_column_count(stmt) != 0)
					{
						_current = i;
						_stmt = stmt;
						_step = rc != SQLITE_DONE ? StepInfo.HasRows : StepInfo.HasNoRows;
						ReadColumns();
						return true;
					}

					while (rc != SQLITE_DONE)
					{
						rc = sqlite3_step(stmt);
						CheckOK(_context.Handle, rc);
					}

					_ = sqlite3_reset(stmt);

					var changes = sqlite3_changes(_context.Handle);
					if (_recordsAffected == -1)
						_recordsAffected = changes;
					else
						_recordsAffected += changes;
				}
				catch
				{
					_ = sqlite3_reset(stmt);
					Dispose();

					throw;
				}
			}
			return false;
		}

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

		public unsafe Stream GetStream(int ordinal)
		{
			if (ordinal < 0 || ordinal >= FieldCount)
				throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, message: null);
			else if (_collumns == null)
				throw OperationImpossible(nameof(GetValue));
			
			int rowIdIndex = _collumns?.ColumnIndex("ROWID") ?? -1;
			if (rowIdIndex == -1)
				throw new InvalidOperationException($"{nameof(GetStream)} method required a RowId column in {nameof(SQLiteReader)}");

			var handle = _context.Connection.Handle;
			var blobDatabaseName = (Utf8z)sqlite3_column_database_name(_stmt, ordinal);
			var blobTableName = (Utf8z)sqlite3_column_table_name(_stmt, ordinal);
			var blobColumnName = (Utf8z)sqlite3_column_origin_name(_stmt, ordinal);
			long rowid = GetInt64(rowIdIndex);

			return new SQLiteBlob(_context.Connection, blobDatabaseName, blobTableName, blobColumnName, rowid, readOnly: true);
		}

		public TextReader GetTextReader(int ordinal)
			=> IsNull(ordinal)
				? new StringReader(string.Empty)
				: new StreamReader(GetStream(ordinal), Encoding.UTF8);

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
