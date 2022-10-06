using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using SQLibre.Core;
using static SQLibre.Core.Raw;
using static SQLibre.Core.Raw.NativeMethods;


namespace SQLibre
{

	/// <summary>
	/// Class for store connection pool information
	/// </summary>
	internal static class SQLiteConnectionPool
	{
		private static readonly object _lock = new object();

		private static readonly List<IntPtr> _pool = new();
		private static readonly List<DbOpenOptions> _options = new();
		private static readonly List<IntPtr> _non_pooled = new();

		static SQLiteConnectionPool() =>
			AssemblyLoadContext.Default.Unloading += ctx =>
			{
				RemoveAll();
				_ = sqlite3_shutdown();
			};

		/// <summary>
		/// Rent a connection from a pool
		/// </summary>
		/// <param name="options"><see cref="SQLiteConnectionOptions"/>SQLite connection configuration parameters</param>
		/// <param name="millisecondsTimeout">Timeoput for waiting in milliseconds</param>
		/// <returns></returns>
		public static DbHandle GetConnection(DbOpenOptions o, int millisecondsTimeout = Timeout.Infinite, Action<IntPtr>? OpenCallBack = null)
		{
			Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(millisecondsTimeout));
			try
			{
				DbHandle db;
				int index;
				if (o.Pooling && (index = _options.IndexOf(o)) > -1)
				{
					return _pool[index];
				}

				db = DbHandle.Open(o.DatabasePath, o.OpenFlag, o.VfsName);
				if (OpenCallBack != null)
					OpenCallBack(db);

				if (o.Pooling)
				{
					_pool.Add(db);
					_options.Add(o);
				}
				else
				{
					_non_pooled.Add(db);
				}
				return db;
			}
			catch 
			{
				throw;
			}
			finally
			{
				Monitor.Exit(_lock);
			}
		}
		/// <summary>
		/// Remove ptr from pool
		/// </summary>
		/// <param name="handle">sqlite3 db</param>
		/// <param name="fullRemove">if true close pooled && non pooled else non pooled only</param>
		/// <param name="millisecondsTimeout">wait timeout if db is busy</param>
		public static void Remove(DbHandle handle, bool fullRemove = false, int millisecondsTimeout = Timeout.Infinite)
		{
			Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(millisecondsTimeout));
			try
			{
				int index = _non_pooled.IndexOf(handle);
				if (index > -1)
					RemoveInternal(index, false);
				else if (fullRemove)
				{
					index = _pool.IndexOf(handle);
					if (index > -1)
						RemoveInternal(index, true);
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				Monitor.Exit(_lock);
			}
		}

		public static void RemoveAll(int millisecondsTimeout = Timeout.Infinite)
		{
			Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(millisecondsTimeout));
			try
			{
				for (; _pool.Count > 0;)
					RemoveInternal(0, true);
				for (; _non_pooled.Count > 0;)
					RemoveInternal(0, false);
			}
			catch
			{
				throw;
			}
			finally
			{
				Monitor.Exit(_lock);
			}
		}

		private static void RemoveInternal(
			int index, 
			bool pooled)
		{
			List<IntPtr> pool = pooled ? _pool : _non_pooled;
			DbHandle handle = pool[index];
			handle.Close();
			pool.RemoveAt(index);
			if (pooled)
				_options.RemoveAt(index);
		}
	}
}
