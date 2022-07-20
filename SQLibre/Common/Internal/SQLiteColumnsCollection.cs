using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace SQLibre
{
	internal sealed class SQLiteColumnCollection : IEnumerable<SQLiteColumn>
	{
		private SQLiteColumn[] _cols;

		internal SQLiteColumnCollection(int colCount)
		{
			//_cols = ArrayPool<SQLiteColumn>.Shared.Rent(colCount); //new SQLiteColumn[colCount];
			_cols = new SQLiteColumn[colCount];
		}

		public int Count => _cols.Length;

		public SQLiteColumn this[int index]
		{
			get => _cols[index];
			internal set => _cols[index] = value;
		}

		public SQLiteColumn this[string index]
		{
			get => _cols[ColumnIndex(index)];
			internal set => _cols[ColumnIndex(index)] = value;
		}

		public int ColumnIndex(string colName)
		{
			int index = 0;
			int hash = colName.ToUpper().GetHashCode();
			foreach (var c in _cols)
			{
				if (c.HashCode == hash)
					return index;
				index++;
			}
			return -1;
		}

		public IEnumerator<SQLiteColumn> GetEnumerator()
			=> (IEnumerator<SQLiteColumn>)_cols.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
			=> _cols.GetEnumerator();

		internal void Clear()
		{
			//ArrayPool<SQLiteColumn>.Shared.Return(_cols, true);
			_cols = Array.Empty<SQLiteColumn>();
		}
	}
}