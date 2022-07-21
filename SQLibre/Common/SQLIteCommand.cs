using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLibre.Core;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;
using static SQLibre.SQLiteException;
using System.Text.Json;

namespace SQLibre
{
	internal static class RefCounter
	{
		private static int _counter;
		public static int Add()=>Interlocked.Increment(ref _counter);
		public static int Remove()=>Interlocked.Decrement(ref _counter);
		public static int Count => _counter;
	}

	public sealed class SQLiteCommand : IDisposable
	{
		private SQLiteContext _context;
		internal IntPtr _stmt;

		internal IntPtr Handle => _stmt;

		internal SQLiteCommand(SQLiteContext context, ReadOnlySpan<byte> statement)
		{
			_context = context;
			var rc = sqlite3_prepare_v2(context.Handle, (Utf8z)statement, out IntPtr stmt, out var tail);
			SQLiteException.CheckOK(rc);
			_stmt = stmt;
			RefCounter.Add();
		}

		#region binding
		public SQLiteCommand Bind(int paramIndex, int paramValue)
		{
			_ = sqlite3_bind_int(_stmt, paramIndex, paramValue);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, bool paramValue)
		{
			_ = sqlite3_bind_int(_stmt, paramIndex, paramValue ? 1 : 0);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, long paramValue)
		{
			_ = sqlite3_bind_int64(_stmt, paramIndex, paramValue);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, double paramValue)
		{
			_ = sqlite3_bind_double(_stmt, paramIndex, paramValue);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex)
		{
			_ = sqlite3_bind_null(_stmt, paramIndex);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, byte[] paramValue)
		{
			_ = sqlite3_bind_blob(_stmt, paramIndex, paramValue);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<int> paramValue)
		{
			_ = Bind(paramIndex, '[' + string.Join(',', paramValue) + ']');
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<long> paramValue)
		{
			_ = Bind(paramIndex, '[' + string.Join(',', paramValue) + ']');
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<double> paramValue)
		{
			_ = Bind(paramIndex, '[' + string.Join(',', paramValue.Select(d => d.ToString(CultureInfo.InvariantCulture))) + ']');
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<string> paramValue)
		{
			_ = Bind(paramIndex, !paramValue.Any() ? "[]" : "[\"" + string.Join("\",\"", paramValue) + "\"]");
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, string paramValue)
		{
			_ = sqlite3_bind_text(_stmt, paramIndex, paramValue);
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, Guid paramValue)
		{
			_ = sqlite3_bind_text(_stmt, paramIndex, paramValue.ToString());
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, DateTime paramValue)
		{
			_ = _context.Connection.StoreDateTimeAsTicks ? sqlite3_bind_int64(_stmt, paramIndex, paramValue.Ticks)
					: sqlite3_bind_text(_stmt, paramIndex, paramValue.ToString(_context.Connection.DateTimeSqliteDefaultFormat));
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, TimeSpan paramValue)
		{
			_ = _context.Connection.StoreDateTimeAsTicks ? sqlite3_bind_int64(_stmt, paramIndex, paramValue.Ticks)
					: sqlite3_bind_text(_stmt, paramIndex, paramValue.ToString());
			return this;
		}
		public SQLiteCommand Bind(string paramName, int paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, bool paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, long paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, double paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex);
		}
		public SQLiteCommand Bind(string paramName, byte[] paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<int> paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<long> paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<double> paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<string> paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, string paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, Guid paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, DateTime paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, TimeSpan paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(string paramName, object? paramValue)
		{
			int paramIndex = sqlite3_bind_parameter_index(_stmt, paramName);
			if (paramIndex <= 0)
				throw new ArgumentOutOfRangeException(nameof(paramName));
			return Bind(paramIndex, paramValue);
		}
		public SQLiteCommand Bind(int paramIndex, object? paramValue)
		{
			if (paramValue == null)
				return Bind(paramIndex);
			else
			{
				var type = paramValue.GetType();
				var t = Type.GetTypeCode(type);
				switch (t)
				{
					case TypeCode.Boolean: return Bind(paramIndex, (bool)paramValue);
					case TypeCode.Byte: return Bind(paramIndex, (byte)paramValue);
					case TypeCode.SByte: return Bind(paramIndex, (sbyte)paramValue);
					case TypeCode.Int16: return Bind(paramIndex, (short)paramValue);
					case TypeCode.UInt16: return Bind(paramIndex, (ushort)paramValue);
					case TypeCode.Char: return Bind(paramIndex, (char)paramValue);
					case TypeCode.Int32: return Bind(paramIndex, (int)paramValue);
					case TypeCode.UInt32: return Bind(paramIndex, (uint)paramValue);
					case TypeCode.Int64: return Bind(paramIndex, (long)paramValue);
					case TypeCode.UInt64: return Bind(paramIndex, (ulong)paramValue);
					case TypeCode.Single: return Bind(paramIndex, (float)paramValue);
					case TypeCode.Double: return Bind(paramIndex, (double)paramValue);
					case TypeCode.Decimal: return Bind(paramIndex, Convert.ToDouble(paramValue));
					case TypeCode.String: return Bind(paramIndex, (string)paramValue);
					case TypeCode.DBNull: return Bind(paramIndex);
					case TypeCode.Empty: return Bind(paramIndex);
					case TypeCode.DateTime: return Bind(paramIndex, (DateTime)paramValue);
					default:
						if (type == typeof(TimeSpan))
							return Bind(paramIndex, (TimeSpan)paramValue);
						else if (type == typeof(Guid))
							return Bind(paramIndex, (Guid)paramValue);
						else if (type == typeof(byte[]))
							return Bind(paramIndex, (byte[])paramValue);
						else if (type.IsEnum)
							return Bind(paramIndex, Convert.ToInt32(paramValue));
						else
							throw new ArgumentException($"Parameter of type {type} not supported");
				}
			}
		}
		#endregion binding

		public unsafe string? Sql => (Utf8z)sqlite3_sql(_stmt);

		public SQLiteReader ExecuteReader()
			=> new SQLiteReader(_context, this);

		public Task<int> ExecuteAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Execute());
		}

		public unsafe int Execute()
		{
			try
			{
				var r = sqlite3_step(_stmt);
				SQLiteException.CheckOK(_context.Handle, r);

				int count = sqlite3_changes(_context.Handle);
				return count;
			}
			catch
			{
				throw;
			}
			finally
			{
				_ = sqlite3_reset(_stmt);
			}
		}

		public T? ExecuteScalar<T>()
		{
			using (var r = ExecuteReader())
			{
				if (r.Read())
					return (T?)r.GetValue(0);
				return default;
			}
		}

		public void Dispose()
		{
			var rc = sqlite3_finalize(_stmt);
			CheckOK(rc);
			_context.RemoveCommand(this);
			GC.SuppressFinalize(this);
			RefCounter.Remove();
		}
		public static int RefCount => RefCounter.Count;
	}
}
