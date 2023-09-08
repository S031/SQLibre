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
using System.Data;
using System.Resources;

namespace SQLibre
{
	internal static class RefCounter
	{
		private static int _counter;
		public static int Add()=>Interlocked.Increment(ref _counter);
		public static int Remove()=>Interlocked.Decrement(ref _counter);
		public static int Count => _counter;
	}

	internal enum CommandState
	{
		None = 0,
		Ready = 1,
		Fetch = 2,
	}

	public  sealed partial class SQLiteCommand : IDbCommand
	{
		private SQLiteConnection? _connection;
		private List<IntPtr> _statements = new(1);
		private string? _commandText;

		internal CommandState State { get; private set; } = CommandState.None;

		internal SQLiteCommand()
		{
			RefCounter.Add();
		}

		internal SQLiteCommand(SQLiteConnection connection, string sql) : this(connection, (ReadOnlySpan<byte>)(Utf8z)sql)
		{
			_commandText = sql;
		}

		internal SQLiteCommand(SQLiteConnection connection, ReadOnlySpan<byte> sql, int commandTimeout = 0) : this()
		{
			_connection = connection;
			CommandTimeout = commandTimeout;
			PrepareStatements(sql);
		}

		public int CommandTimeout { get; set; }

		public SQLiteConnection? Connection
		{
			get => _connection;
			set
			{
				_connection = value;
				if (!string.IsNullOrEmpty(_commandText))
					Prepare();
			}		
		}

		internal List<IntPtr> Statements => _statements;

		public SQLiteReader ExecuteReader()
		{
			CheckRequiredState(nameof(ExecuteReader));
			var r = new SQLiteReader(this);
			r.NextResult();
			//State = CommandState.Fetch;
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
			return r.RecordsAffected;
		}

		public T? ExecuteScalar<T>()
		{
			CheckRequiredState(nameof(ExecuteScalar));
			using (var r = ExecuteReader())
			{
				if (r.Read())
					return (T?)r.GetValue(0);
				return default;
			}
		}

		private void PrepareStatements(ReadOnlySpan<byte> sql)
		{
			_connection.CheckRequiredState(nameof(PrepareStatements));

			Stopwatch timer = new();
			DisposeStatements();

			var byteCount = sql.Length - 1;
			int rc;
			IntPtr stmt;
			var start = 0;
			int commandTimeout = CommandTimeout;

			do
			{
				timer.Start();

				ReadOnlySpan<byte> tail;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
				while (IsBusy(rc = sqlite3_prepare_v2(_connection.Handle, sql.Slice(start), out stmt, out tail)))
				{
					if (commandTimeout != 0
						&& timer.ElapsedMilliseconds >= commandTimeout * 1000L)
					{
						break;
					}

					Thread.Sleep(150);
				}
#pragma warning restore CS8602 // Dereference of a possibly null reference.

				timer.Stop();
				start = sql.Length - tail.Length;

				CheckOK(_connection.Handle, rc);

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
			State = CommandState.Ready;
		}

		public void Dispose()
		{
			DisposeStatements();
			_connection?.RemoveCommand(this);
			GC.SuppressFinalize(this);
			RefCounter.Remove();
		}

		private void DisposeStatements()
		{
			if (State == CommandState.Ready)
			{
				//dispose also reader if need
				foreach (var p in _statements)
				{
					var rc = sqlite3_finalize(p);
					CheckOK(rc);
				}
				_statements.Clear();
			}
			State = CommandState.None;
		}

		void IDbCommand.Cancel()
		{
		}

		IDbDataParameter IDbCommand.CreateParameter() => throw new NotSupportedException();

		int IDbCommand.ExecuteNonQuery() => Execute();

		IDataReader IDbCommand.ExecuteReader() => ExecuteReader();

		IDataReader IDbCommand.ExecuteReader(CommandBehavior behavior) => ExecuteReader();

		object? IDbCommand.ExecuteScalar()
		{
			using (var r = ExecuteReader())
			{
				if (r.Read())
					return r.GetValue(0);
				return default;
			}
		}

		public void Prepare()
		{
			if (State == CommandState.None)
				PrepareStatements((Utf8z)_commandText);
		}


		unsafe string? IDbCommand.CommandText
		{
#pragma warning disable CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
			get => _commandText;
#pragma warning restore CS8768 // Nullability of reference types in return type doesn't match implemented member (possibly because of nullability attributes).
			set
			{
				_commandText = value;
				if (_connection != null)
					PrepareStatements((Utf8z)_commandText);
			}
		}
		
		CommandType IDbCommand.CommandType { 
			get => CommandType.Text;
			set
			{
				if (value != CommandType.Text)
					throw new ArgumentException($"Invalid Command Type '{value}'");
			}
		}
		
		IDbConnection? IDbCommand.Connection
		{
			get => Connection;
			set => Connection = value as SQLiteConnection ?? throw new InvalidOperationException($"Required argument of type {nameof(SQLiteConnection)}");
		}

		IDataParameterCollection IDbCommand.Parameters => throw new NotSupportedException();

		IDbTransaction? IDbCommand.Transaction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
		
		UpdateRowSource IDbCommand.UpdatedRowSource { get; set; }
		
		public static int RefCount => RefCounter.Count;
		
		internal void CheckRequiredState(string sourceMethod, CommandState requiredState = CommandState.Ready)
		{
			_connection.CheckRequiredState(sourceMethod);
			if (State != requiredState)
				throw new InvalidOperationException($"Method {sourceMethod} required {requiredState} command state");
		}
	}
}
