using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLibre
{
	internal ref struct KeyValuePairReader
	{
		private ReadOnlySpan<char> _buffer;
		private ReadOnlySpan<char> _sep1;
		private ReadOnlySpan<char> _sep2;
		int _offset;

		public KeyValuePairReader(string text, string itemSeparator=";", string keyValueSeparator = "=")
		{
			_buffer = text.AsSpan();
			_sep1 = itemSeparator.AsSpan();
			_sep2 = keyValueSeparator.AsSpan();
			_offset = 0;
		}

		public bool Read(out KeyValuePair<string, string> pair)
		{
			pair = new();
			int len = _buffer.Length;
			if (_offset >= len)
				return false;

			int pos = _buffer[_offset..].IndexOf(_sep1);
			if (pos == -1)
				pos = len - _offset;

			var token = _buffer.Slice(_offset, pos);
			if (token.Length > 0)
			{
				pos = token.IndexOf(_sep2);
				if (pos == -1)
					throw new InvalidOperationException($"Wrong string format");
				pair = new(token[0..(pos)].ToString(), token[(pos+_sep2.Length)..].ToString());
			}
			_offset += token.Length + _sep1.Length;
			return true;
		}

		public static ReadOnlySpan<char> GetToken(ReadOnlySpan<char> source, ReadOnlySpan<char> separator, int offset)
		{
			int start = offset;
			int len = separator.Length;
			int totalLen = source.Length;

			if (start >= totalLen)
				return ReadOnlySpan<char>.Empty;

			int pos = source[start..].IndexOf(separator);
			if (pos < 0) pos = totalLen;

			int finish = source[start..].IndexOf(separator);
			if (finish > 0)
			{
				return source.Slice(start, finish);
			}
			else
			{
				return source[start..];
			}
		}
	}
}
