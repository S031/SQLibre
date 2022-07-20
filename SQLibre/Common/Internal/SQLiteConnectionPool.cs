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
		public static DbHandle GetConnection(DbOpenOptions o, int millisecondsTimeout = Timeout.Infinite)
		{
			Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(millisecondsTimeout));
			try
			{
				int index = _options.IndexOf(o);
				DbHandle db;
				if (index == -1)
				{
					db = DbHandle.Open(o.DatabasePath, o.OpenFlag, o.VfsName);
					_pool.Add(db);
					_options.Add(o);
				}
				else
					db = _pool[index];
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

		public static void Remove(DbHandle handle, int millisecondsTimeout = Timeout.Infinite)
		{
			Monitor.TryEnter(_lock, TimeSpan.FromMilliseconds(millisecondsTimeout));
			try
			{
				RemoveInternal(handle);
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
					RemoveInternal(_pool[0]);
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

		private static void RemoveInternal(DbHandle handle)
		{
			int index = _pool.IndexOf(handle);
			if (index == -1)
				throw new ArgumentOutOfRangeException(nameof(handle));
			var db = (DbHandle)_pool[index];
			db.Close();
			_pool.RemoveAt(index);
			_options.RemoveAt(index);
		}
	}
}
