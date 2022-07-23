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
	internal static class RefCounter
	{
		private static int _counter;
		public static int Add()=>Interlocked.Increment(ref _counter);
		public static int Remove()=>Interlocked.Decrement(ref _counter);
		public static int Count => _counter;
	}

	public  sealed partial class SQLiteCommand : IDisposable
	{
		private SQLiteContext _context;
		private List<IntPtr> _statements = new(1);

		internal SQLiteCommand(SQLiteContext context, ReadOnlySpan<byte> statement)
		{
			_context = context;
			PrepareStatements(statement, 0);
			RefCounter.Add();
		}

		public int CommandTimeout { get; set; }

		public SQLiteContext Context => _context;

		internal List<IntPtr> Statements => _statements;

		public SQLiteReader ExecuteReader()
		{
			var r = new SQLiteReader(this);
			return r;
		}

		public Task<int> ExecuteAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Execute());
		}

		public unsafe int Execute()
		{
			using var r = ExecuteReader();
			r.Read();
			return r.RecordsAffected;
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

		private void PrepareStatements(ReadOnlySpan<byte> sql, int commandTimeout)
		{
			Stopwatch timer = new();
			DisposeStatements();

			var byteCount = sql.Length - 1;
			int rc;
			IntPtr stmt;
			var start = 0;
			
			do
			{
				timer.Start();

				ReadOnlySpan<byte> tail;
				while (IsBusy(rc = sqlite3_prepare_v2(_context.Handle, sql.Slice(start), out stmt, out tail)))
				{
					if (commandTimeout != 0
						&& timer.ElapsedMilliseconds >= commandTimeout * 1000L)
					{
						break;
					}

					Thread.Sleep(150);
				}

				timer.Stop();
				start = sql.Length - tail.Length;

				CheckOK(_context.Handle, rc);

				// Statement was empty, white space, or a comment
				if (stmt == IntPtr.Zero)
				{
					if (start < byteCount)
						continue;
					break;
				}

				_statements.Add(stmt);
			}
			while (start < byteCount);
		}

		public void Dispose()
		{
			DisposeStatements();
			_context.RemoveCommand(this);
			GC.SuppressFinalize(this);
			RefCounter.Remove();
		}

		private void DisposeStatements()
		{
			//dispose also reader if need
			foreach (var p in _statements)
			{
				var rc = sqlite3_finalize(p);
				CheckOK(rc);
			}
			_statements.Clear();
		}

		public static int RefCount => RefCounter.Count;
	}
}
