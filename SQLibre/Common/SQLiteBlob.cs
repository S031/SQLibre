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
using System.Resources;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace SQLibre
{
	public class SQLiteBlob : Stream
	{
		private IntPtr _blob = IntPtr.Zero;
		private readonly SQLiteConnection _connection;
		private long _position;

		private static InvalidOperationException OperationImpossible(string name) => 
			new InvalidOperationException($"Operation {name} is not possible for current connection state");
		/// <summary>
		///     Initializes a new instance of the <see cref="SqliteBlob" /> class.
		/// </summary>
		/// <param name="connection">An open connection to the database.</param>
		/// <param name="tableName">The name of table containing the blob.</param>
		/// <param name="columnName">The name of the column containing the blob.</param>
		/// <param name="rowid">The rowid of the row containing the blob.</param>
		/// <param name="readOnly">A value indicating whether the blob is read-only.</param>
		/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/blob-io">BLOB I/O</seealso>
		public SQLiteBlob(
			SQLiteConnection connection,
			string tableName,
			string columnName,
			long rowid,
			bool readOnly = false)
			: this(connection, SQLiteConnection.MainDatabaseName, tableName, columnName, rowid, readOnly)
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="SqliteBlob" /> class.
		/// </summary>
		/// <param name="connection">An open connection to the database.</param>
		/// <param name="databaseName">The name of the attached database containing the blob.</param>
		/// <param name="tableName">The name of table containing the blob.</param>
		/// <param name="columnName">The name of the column containing the blob.</param>
		/// <param name="rowid">The rowid of the row containing the blob.</param>
		/// <param name="readOnly">A value indicating whether the blob is read-only.</param>
		/// <seealso href="https://docs.microsoft.com/dotnet/standard/data/sqlite/blob-io">BLOB I/O</seealso>
		public unsafe SQLiteBlob(
			SQLiteConnection connection,
			Utf8z databaseName,
			Utf8z tableName,
			Utf8z columnName,
			long rowid,
			bool readOnly = false)
		{
			if (connection == null || connection.State != ConnectionState.Open)
				throw new InvalidOperationException($"Operation {nameof(SQLiteBlob)} is not possible for current connection state");

			if (tableName.Length == 0)
				throw new ArgumentNullException(nameof(tableName));

			if (columnName.Length == 0)
				throw new ArgumentNullException(nameof(columnName));

			_connection = connection;
			CanWrite = !readOnly;
			var rc = sqlite3_blob_open(
				_connection.Handle,
				databaseName,
				tableName,
				columnName,
				rowid,
				readOnly ? 0 : 1,
				out _blob);
			CheckOK(_connection.Handle, rc);
			Length = sqlite3_blob_bytes(_blob);
		}
		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite { get; } 

		public override long Length { get; }

		public override long Position
		{
			get => _position;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, message: null);
				_position = value;
			}
		}

		public override void Flush()
		{
		}

		/// <summary>
		///     Reads a sequence of bytes from the current stream and advances the position
		///     within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">
		///     An array of bytes. When this method returns, the buffer contains the specified byte array
		///     with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.
		/// </param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the buffer.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				// NB: Message is provided by the framework
				throw new ArgumentOutOfRangeException(nameof(offset), offset, message: null);
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count, message: null);
			}

			if (offset + count > buffer.Length)
			{
				throw new ArgumentException($"Invalid {nameof(offset)} And {nameof(count)}");
			}

			return Read(buffer.AsSpan(offset, count));
		}

		/// <summary>
		///     Reads a sequence of bytes from the current stream and advances the position within the stream by the
		///     number of bytes read.
		/// </summary>
		/// <param name="buffer">
		///     A region of memory. When this method returns, the contents of this region are replaced by the bytes read
		///     from the current source.
		/// </param>
		/// <returns>
		///     The total number of bytes read into the buffer. This can be less than the number of bytes allocated in
		///     the buffer if that many bytes are not currently available, or zero (0) if the end of the stream has been
		///     reached.
		/// </returns>
		public unsafe override int Read(Span<byte> buffer)
		{
			if (_blob == IntPtr.Zero)
			{
				throw new ObjectDisposedException(objectName: nameof(SQLiteBlob));
			}

			var position = _position;
			if (position > Length)
			{
				position = Length;
			}

			var count = buffer.Length;
			if (position + count > Length)
			{
				count = (int)(Length - position);
			}

			fixed (byte* b = buffer)
			{
				var rc = sqlite3_blob_read(_blob, b, count, (int)position);
				CheckOK(_connection.Handle, rc);
			}
			_position += count;
			return count;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			long position;
			switch (origin)
			{
				case SeekOrigin.Begin:
					position = offset;
					break;
				case SeekOrigin.Current:
					position = _position + offset;
					break;
				case SeekOrigin.End:
					position = Length + offset;
					break;
				default:
					throw new NotSupportedException(nameof(origin));
			}

			if (position < 0)
				throw new IOException("SeekBeforeBegin");

			return _position = position;
		}

		public override void SetLength(long value)
			=> throw new NotSupportedException();

		/// <summary>
		///     Writes a sequence of bytes to the current stream and advances the current position
		///     within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				// NB: Message is provided by the framework
				throw new ArgumentOutOfRangeException(nameof(offset), offset, message: null);
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count, message: null);
			}

			if (offset + count > buffer.Length)
			{
				throw new ArgumentException($"Invalid {nameof(offset)} And {nameof(count)}");
			}

			if (_blob == IntPtr.Zero)
			{
				throw new ObjectDisposedException(objectName: nameof(SQLiteBlob));
			}

			Write(buffer.AsSpan(offset, count));
		}

		/// <summary>
		///     Writes a sequence of bytes to the current stream and advances the current position within this stream by
		///     the number of bytes written.
		/// </summary>
		/// <param name="buffer">
		///     A region of memory. This method copies the contents of this region to the current stream.
		/// </param>
		public unsafe override void Write(ReadOnlySpan<byte> buffer)
		{
			if (!CanWrite)
			{
				throw new NotSupportedException(nameof(Write));
			}

			var position = _position;
			if (position > Length)
			{
				position = Length;
			}

			var count = buffer.Length;
			if (position + count > Length)
			{
				throw new NotSupportedException("Method Resize not supported");
			}

			fixed (byte* b = buffer)
			{
				var rc = sqlite3_blob_write(_blob, b, count, (int)position);
				CheckOK(_connection.Handle, rc);
			}
			_position += count;
		}
		
		/// <summary>
		///     Releases any resources used by the blob and closes it.
		/// </summary>
		/// <param name="disposing">
		///     true to release managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
		/// </param>
		protected override void Dispose(bool disposing)
		{
			if (!disposing)
				base.Dispose(false);

			if (_blob != IntPtr.Zero)
			{
				var rc = sqlite3_blob_close(_blob);
				CheckOK(_connection.Handle, rc);
				_blob = IntPtr.Zero;
			}
		}

	}
}
