using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre
{
	public sealed class SQLiteTransaction : IDbTransaction
	{
		private readonly SQLiteConnection _connection;
		private readonly IsolationLevel _isolationLevel;

		private bool _completed;

		internal SQLiteTransaction(SQLiteConnection connection, IsolationLevel isolationLevel = IsolationLevel.Serializable)
		{
			_connection = connection;
			if ((isolationLevel != IsolationLevel.ReadUncommitted
				|| ((_connection.ConnectionOptions.OpenFlags != (_connection.ConnectionOptions.OpenFlags & SQLiteOpenFlags.SQLITE_OPEN_SHAREDCACHE))))
				&& isolationLevel != IsolationLevel.Serializable)
				isolationLevel = IsolationLevel.Serializable;

			_isolationLevel = isolationLevel;

			if (_isolationLevel == IsolationLevel.ReadUncommitted)
				_connection.Execute("PRAGMA read_uncommitted = 1;"u8);
			_connection.Execute("begin transaction;"u8);
		}

		public IDbConnection? Connection => throw new NotImplementedException();

		public SQLiteConnection? SQLiteConnection => _connection;

		public IsolationLevel IsolationLevel => _isolationLevel;

		public void Commit()
		{
			if (_completed)
				throw new InvalidOperationException($"This {nameof(SQLiteTransaction)} has completed");
			_connection.Execute("commit;"u8);
			Complete();
		}

		public void Dispose()
		{
			if (!_completed)
				Rollback();
		}

		public void Rollback()
		{
			if (_completed)
				throw new InvalidOperationException($"This {nameof(SQLiteTransaction)} has completed");
			_connection.Execute("rollback;"u8);
			Complete();
		}
		
		private void Complete()
		{
			if (IsolationLevel == IsolationLevel.ReadUncommitted)
				_connection!.Execute("PRAGMA read_uncommitted = 0;"u8);

			_connection!.Transaction = null;
			_completed = true;
		}
	}
}
