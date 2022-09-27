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
using System.Diagnostics;

namespace SQLibre
{
	public partial class SQLiteCommand
	{
		#region binding
		#region internal
		/// <summary>
		/// using for bind empty blob long size and then using blob_write method for load data
		/// </summary>
		/// <param name="stmt">sqlite3 statement</param>
		/// <param name="paramIndex">parameter index</param>
		/// <param name="blobSize">size of new blob</param>
		/// <returns></returns>
		private static int BindZeroBlob(IntPtr stmt, int paramIndex, int blobSize) => sqlite3_bind_zeroblob(stmt, paramIndex, blobSize);
		private static int BindValue(IntPtr stmt, int paramIndex, int paramValue) => sqlite3_bind_int(stmt, paramIndex, paramValue);
		private static int BindValue(IntPtr stmt, int paramIndex, bool paramValue) => sqlite3_bind_int(stmt, paramIndex, paramValue ? 1 : 0);
		private static int BindValue(IntPtr stmt, int paramIndex, long paramValue) => sqlite3_bind_int64(stmt, paramIndex, paramValue);
		private static int BindValue(IntPtr stmt, int paramIndex, double paramValue) => sqlite3_bind_double(stmt, paramIndex, paramValue);
		private static int BindValue(IntPtr stmt, int paramIndex) => sqlite3_bind_null(stmt, paramIndex);
		private static int BindValue(IntPtr stmt, int paramIndex, byte[] paramValue) => sqlite3_bind_blob(stmt, paramIndex, paramValue);
		private static int BindValue(IntPtr stmt, int paramIndex, IEnumerable<int> paramValue) => BindValue(stmt, paramIndex, '[' + string.Join(',', paramValue) + ']');
		private static int BindValue(IntPtr stmt, int paramIndex, IEnumerable<long> paramValue) => BindValue(stmt, paramIndex, '[' + string.Join(',', paramValue) + ']');
		private static int BindValue(IntPtr stmt, int paramIndex, IEnumerable<double> paramValue) => BindValue(stmt, paramIndex, '[' + string.Join(',', paramValue.Select(d => d.ToString(CultureInfo.InvariantCulture))) + ']');
		private static int BindValue(IntPtr stmt, int paramIndex, IEnumerable<string> paramValue) => BindValue(stmt, paramIndex, !paramValue.Any() ? "[]" : "[\"" + string.Join("\",\"", paramValue) + "\"]");
		private static int BindValue(IntPtr stmt, int paramIndex, string paramValue) => sqlite3_bind_text(stmt, paramIndex, paramValue);
		private static int BindValue(IntPtr stmt, int paramIndex, Guid paramValue) => sqlite3_bind_text(stmt, paramIndex, paramValue.ToString());
		private int BindValue(IntPtr stmt, int paramIndex, DateTime paramValue) => _context.Connection.StoreDateTimeAsTicks ? sqlite3_bind_int64(stmt, paramIndex, paramValue.Ticks)
					: sqlite3_bind_text(stmt, paramIndex, paramValue.ToString(_context.Connection.DateTimeSqliteDefaultFormat));
		private int BindValue(IntPtr stmt, int paramIndex, TimeSpan paramValue) =>
			_context.Connection.StoreDateTimeAsTicks
			? sqlite3_bind_int64(stmt, paramIndex, paramValue.Ticks)
			: sqlite3_bind_text(stmt, paramIndex, paramValue.ToString());
		#endregion internal
		#region ByIndex
		public SQLiteCommand BindZeroBlob(int paramIndex, int blobSize)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindZeroBlob(stmt, paramIndex, blobSize);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, int paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, bool paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, long paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, double paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, byte[] paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<int> paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<long> paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<double> paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, IEnumerable<string> paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, string paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, Guid paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, DateTime paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, TimeSpan paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		public SQLiteCommand Bind(int paramIndex, object? paramValue)
		{
			foreach (var stmt in _statements)
			{
				var rc = BindValue(stmt, paramIndex, paramValue);
				CheckOK(rc);
			}
			return this;
		}
		#endregion ByIndex
		#region ByName
		public SQLiteCommand BindZeroBlob(string paramName, int blobSize)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindZeroBlob(stmt, paramIndex, blobSize);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, int paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, bool paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, long paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, double paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, byte[] paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<int> paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<long> paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<double> paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, IEnumerable<string> paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, string paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, Guid paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, DateTime paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, TimeSpan paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					BindValue(stmt, paramIndex, paramValue);
			}
			return this;
		}
		public SQLiteCommand Bind(string paramName, object? paramValue)
		{
			foreach (var stmt in _statements)
			{
				int paramIndex = sqlite3_bind_parameter_index(stmt, paramName);
				if (paramIndex > 0)
					CheckOK(BindValue(stmt, paramIndex, paramValue));
			}
			return this;
		}
		#endregion ByName

		private int BindValue(IntPtr stmt, int paramIndex, object? paramValue)
		{
			if (paramValue == null)
				return BindValue(stmt, paramIndex);
			else
			{
				var type = paramValue.GetType();
				var t = Type.GetTypeCode(type);
				switch (t)
				{
					case TypeCode.Boolean: return BindValue(stmt, paramIndex, (bool)paramValue);
					case TypeCode.Byte: return BindValue(stmt, paramIndex, (byte)paramValue);
					case TypeCode.SByte: return BindValue(stmt, paramIndex, (sbyte)paramValue);
					case TypeCode.Int16: return BindValue(stmt, paramIndex, (short)paramValue);
					case TypeCode.UInt16: return BindValue(stmt, paramIndex, (ushort)paramValue);
					case TypeCode.Char: return BindValue(stmt, paramIndex, (char)paramValue);
					case TypeCode.Int32: return BindValue(stmt, paramIndex, (int)paramValue);
					case TypeCode.UInt32: return BindValue(stmt, paramIndex, (uint)paramValue);
					case TypeCode.Int64: return BindValue(stmt, paramIndex, (long)paramValue);
					case TypeCode.UInt64: return BindValue(stmt, paramIndex, (ulong)paramValue);
					case TypeCode.Single: return BindValue(stmt, paramIndex, (float)paramValue);
					case TypeCode.Double: return BindValue(stmt, paramIndex, (double)paramValue);
					case TypeCode.Decimal: return BindValue(stmt, paramIndex, Convert.ToDouble(paramValue));
					case TypeCode.String: return BindValue(stmt, paramIndex, (string)paramValue);
					case TypeCode.DBNull: return BindValue(stmt, paramIndex);
					case TypeCode.Empty: return BindValue(stmt, paramIndex);
					case TypeCode.DateTime: return BindValue(stmt, paramIndex, (DateTime)paramValue);
					default:
						if (type == typeof(TimeSpan))
							return BindValue(stmt, paramIndex, (TimeSpan)paramValue);
						else if (type == typeof(Guid))
							return BindValue(stmt, paramIndex, (Guid)paramValue);
						else if (type == typeof(byte[]))
							return BindValue(stmt, paramIndex, (byte[])paramValue);
						else if (type.IsEnum)
							return BindValue(stmt, paramIndex, Convert.ToInt32(paramValue));
						else
							throw new ArgumentException($"Parameter of type {type} not supported");
				}
			}
		}
		#endregion binding
	}
}
